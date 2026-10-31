using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KCES2.Fusebox.Style;

/// <summary>Requires braces when embedded statement starts on its own line.</summary>
/// <remarks>IDE0011 when_multiline flags only bodies that span several lines, not "if (x)" + "return;" on next line.</remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmbeddedStatementAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Descriptors.BracesOnOwnLine];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.IfStatement,
            SyntaxKind.ElseClause,
            SyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement,
            SyntaxKind.ForEachVariableStatement,
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement,
            SyntaxKind.UsingStatement,
            SyntaxKind.LockStatement,
            SyntaxKind.FixedStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        StatementSyntax body = context.Node switch
        {
            IfStatementSyntax s => s.Statement,
            ElseClauseSyntax s => s.Statement,
            ForStatementSyntax s => s.Statement,
            CommonForEachStatementSyntax s => s.Statement,
            WhileStatementSyntax s => s.Statement,
            DoStatementSyntax s => s.Statement,
            UsingStatementSyntax s => s.Statement,
            LockStatementSyntax s => s.Statement,
            FixedStatementSyntax s => s.Statement,
            _ => null,
        };
        if (body == null || body is BlockSyntax || (body is IfStatementSyntax && context.Node is ElseClauseSyntax)) return;

        // Nested using/fixed chain shares braces of innermost statement.
        if (body is UsingStatementSyntax && context.Node is UsingStatementSyntax) return;
        if (body is FixedStatementSyntax && context.Node is FixedStatementSyntax) return;

        SyntaxToken before = body.GetFirstToken().GetPreviousToken();
        int headLine = before.GetLocation().GetLineSpan().EndLinePosition.Line;
        int bodyLine = body.GetLocation().GetLineSpan().StartLinePosition.Line;
        if (bodyLine > headLine) context.ReportDiagnostic(Diagnostic.Create(Descriptors.BracesOnOwnLine, body.GetLocation()));
    }
}
