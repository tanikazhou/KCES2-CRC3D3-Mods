using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace KCES2.Fusebox.Style;

/// <summary>Checks C# line length, blank lines at braces and CR line ends.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SourceLayoutAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Descriptors.LineTooLong, Descriptors.BlankLineAtBrace, Descriptors.SourceLineEnds];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(Analyze);
    }

    private static void Analyze(SyntaxTreeAnalysisContext context)
    {
        SyntaxTree tree = context.Tree;
        SourceText text = tree.GetText(context.CancellationToken);
        SyntaxNode root = tree.GetRoot(context.CancellationToken);

        CheckLineEnds(context, tree, text);
        foreach (TextLine line in text.Lines)
        {
            int length = line.End - line.Start;
            if (length > Descriptors.MaxLineLength && !IsExempt(root, text, line))
            {
                var span = TextSpan.FromBounds(line.Start + Descriptors.MaxLineLength, line.End);
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LineTooLong, Location.Create(tree, span), length, Descriptors.MaxLineLength));
            }
        }

        foreach (SyntaxToken token in root.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.OpenBraceToken)) CheckAfterOpen(context, text, token);
            else if (token.IsKind(SyntaxKind.CloseBraceToken)) CheckBeforeClose(context, text, token);
        }
    }

    private static void CheckLineEnds(SyntaxTreeAnalysisContext context, SyntaxTree tree, SourceText text)
    {
        foreach (TextLine line in text.Lines)
        {
            if (line.EndIncludingLineBreak > line.End && text[line.End] == '\r')
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.SourceLineEnds, Location.Create(tree, new TextSpan(line.End, 0)), "CR line end; use LF"));
                return;
            }
        }
    }

    // Overflow inside string literal or URL cannot break cleanly.
    private static bool IsExempt(SyntaxNode root, SourceText text, TextLine line)
    {
        int position = line.Start + Descriptors.MaxLineLength;
        SyntaxToken token = root.FindToken(position, findInsideTrivia: true);
        if (token.Span.Contains(position) && IsStringToken(token)) return true;

        string content = text.ToString(line.Span);
        return content.Contains("://");
    }

    private static bool IsStringToken(SyntaxToken token)
    {
        return (SyntaxKind)token.RawKind switch
        {
            SyntaxKind.StringLiteralToken or SyntaxKind.InterpolatedStringTextToken or SyntaxKind.Utf8StringLiteralToken or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken => true,
            _ => token.Parent != null && token.Parent.FirstAncestorOrSelf<SyntaxNode>(static n => n.IsKind(SyntaxKind.InterpolatedStringExpression)) != null,
        };
    }

    private static void CheckAfterOpen(SyntaxTreeAnalysisContext context, SourceText text, SyntaxToken brace)
    {
        int lineNumber = text.Lines.GetLineFromPosition(brace.SpanStart).LineNumber;
        SyntaxToken next = brace.GetNextToken(includeZeroWidth: false, includeSkipped: true);
        if (next.IsKind(SyntaxKind.None)) return;

        int nextLine = text.Lines.GetLineFromPosition(next.SpanStart).LineNumber;
        if (nextLine <= lineNumber + 1 || !IsBlank(text, lineNumber + 1)) return;

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineAtBrace, Location.Create(context.Tree, text.Lines[lineNumber + 1].Span), "after opening"));
    }

    private static void CheckBeforeClose(SyntaxTreeAnalysisContext context, SourceText text, SyntaxToken brace)
    {
        int lineNumber = text.Lines.GetLineFromPosition(brace.SpanStart).LineNumber;
        SyntaxToken previous = brace.GetPreviousToken(includeZeroWidth: false, includeSkipped: true);
        if (previous.IsKind(SyntaxKind.None) || lineNumber == 0) return;

        int previousLine = text.Lines.GetLineFromPosition(previous.Span.End).LineNumber;
        if (previousLine >= lineNumber - 1 || !IsBlank(text, lineNumber - 1)) return;

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineAtBrace, Location.Create(context.Tree, text.Lines[lineNumber - 1].Span), "before closing"));
    }

    private static bool IsBlank(SourceText text, int lineNumber) =>
        string.IsNullOrWhiteSpace(text.ToString(text.Lines[lineNumber].Span));
}
