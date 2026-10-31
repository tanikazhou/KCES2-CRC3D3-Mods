using System;
using System.Collections.Immutable;
using System.IO;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace KCES2.Fusebox.Style;

/// <summary>Checks .csproj, .projitems, .props, .targets and .slnx files passed as AdditionalFiles.</summary>
/// <remarks>KCES2.Fusebox.props adds project file, repo build files, .projitems and .slnx files as AdditionalFiles.</remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProjectFileAnalyzer : DiagnosticAnalyzer
{
    private const int IndentSize = 2;

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Descriptors.ProjectFileInvalid, Descriptors.ProjectFileLayout];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterAdditionalFileAction(Analyze);
    }

    private static void Analyze(AdditionalFileAnalysisContext context)
    {
        string path = context.AdditionalFile.Path;
        if (!IsProjectFile(path)) return;

        SourceText text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text == null) return;

        CheckText(context, path, text);
        CheckIndent(context, path, text);
    }

    private static bool IsProjectFile(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".projitems", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".props", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".targets", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }

    private static void CheckText(AdditionalFileAnalysisContext context, string path, SourceText text)
    {
        if (text.Length > 0 && text[text.Length - 1] != '\n') Report(context, path, text, text.Lines.Count - 1, "No final newline.");

        bool crReported = false;
        foreach (TextLine line in text.Lines)
        {
            string content = text.ToString(line.Span);
            if (!crReported && line.EndIncludingLineBreak > line.End && text[line.End] == '\r')
            {
                Report(context, path, text, line.LineNumber, "CR line end; use LF.");
                crReported = true;
            }

            if (content.Length > 0 && char.IsWhiteSpace(content[content.Length - 1])) Report(context, path, text, line.LineNumber, "Trailing whitespace.");
            if (content.TrimStart(' ').StartsWith("\t", StringComparison.Ordinal)) Report(context, path, text, line.LineNumber, "Tab in indent; use spaces.");
        }
    }

    // Lines that start with element, end tag, comment or declaration: indent = 2 x depth. Continuation lines are free.
    private static void CheckIndent(AdditionalFileAnalysisContext context, string path, SourceText text)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        try
        {
            using var reader = XmlReader.Create(new StringReader(text.ToString()), settings);
            var info = (IXmlLineInfo)reader;
            while (reader.Read())
            {
                // Line position points at node name; distance back to "<" differs per node type.
                int back = reader.NodeType switch
                {
                    XmlNodeType.Element => 1,
                    XmlNodeType.EndElement => 2,
                    XmlNodeType.Comment => 4,
                    XmlNodeType.XmlDeclaration or XmlNodeType.ProcessingInstruction => 2,
                    _ => -1,
                };
                if (back < 0) continue;

                int lineNumber = info.LineNumber - 1;
                int column = info.LinePosition - 1 - back;
                string content = text.ToString(text.Lines[lineNumber].Span);
                int indent = content.Length - content.TrimStart(' ').Length;
                if (indent != column) continue;

                int expected = reader.Depth * IndentSize;
                if (indent != expected) Report(context, path, text, lineNumber, $"Indent {indent}, expected {expected} (2 spaces per level).");
            }
        }
        catch (XmlException e)
        {
            int lineNumber = Math.Max(0, Math.Min(text.Lines.Count - 1, e.LineNumber - 1));
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.ProjectFileInvalid, LineLocation(path, text, lineNumber), e.Message));
        }
    }

    private static void Report(AdditionalFileAnalysisContext context, string path, SourceText text, int lineNumber, string message) =>
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.ProjectFileLayout, LineLocation(path, text, lineNumber), message));

    private static Location LineLocation(string path, SourceText text, int lineNumber)
    {
        TextLine line = text.Lines[lineNumber];
        return Location.Create(path, line.Span, text.Lines.GetLinePositionSpan(line.Span));
    }
}
