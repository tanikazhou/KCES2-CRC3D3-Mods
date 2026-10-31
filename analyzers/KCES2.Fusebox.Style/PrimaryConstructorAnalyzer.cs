using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KCES2.Fusebox.Style;

/// <summary>Reports classes with primary constructor.</summary>
/// <remarks>Records and structs keep primary constructors; Roslyn has no rule that forbids them on classes.</remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimaryConstructorAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Descriptors.PrimaryConstructorOnClass];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (declaration.ParameterList == null) return;

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.PrimaryConstructorOnClass, declaration.ParameterList.GetLocation(), declaration.Identifier.Text));
    }
}
