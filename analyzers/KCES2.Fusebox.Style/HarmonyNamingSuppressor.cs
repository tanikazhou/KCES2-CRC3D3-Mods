using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KCES2.Fusebox.Style;

/// <summary>Suppresses IDE1006 on parameters of Harmony patch methods.</summary>
/// <remarks>
/// <para>
/// Harmony binds patch parameters by name: __instance, __result, ___field, and parameter names of patched method.
/// Renaming one breaks patch at run time, not at build time.
/// </para>
/// <para>
/// Patch method = method with Harmony attribute, Prefix/Postfix/... in class with Harmony attribute, method with parameter that starts
/// with <c>__</c>, or method named in <c>nameof</c> in file with <c>using HarmonyLib</c> (manual <c>Harmony.Patch</c> registration).
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HarmonyNamingSuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor _rule = new(
        "KFBP0001",
        "IDE1006",
        "Harmony binds patch method parameters by name.");

    private static readonly ImmutableHashSet<string> _patchMethodNames = ImmutableHashSet.Create(
        "Prefix", "Postfix", "Transpiler", "Finalizer", "Prepare", "Cleanup", "TargetMethod", "TargetMethods");

    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [_rule];

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        HashSet<IMethodSymbol> namedMethods = null;
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            SyntaxTree tree = diagnostic.Location.SourceTree;

            SyntaxNode node = tree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
            if (node?.FirstAncestorOrSelf<ParameterSyntax>() == null) continue;

            MethodDeclarationSyntax method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null) continue;

            SemanticModel model = context.GetSemanticModel(tree);
            if (model.GetDeclaredSymbol(method, context.CancellationToken) is not { } symbol) continue;

            if (IsPatchMethod(symbol))
            {
                context.ReportSuppression(Suppression.Create(_rule, diagnostic));
                continue;
            }

            namedMethods ??= NamedInHarmonyFiles(context);
            if (namedMethods.Contains(symbol.OriginalDefinition)) context.ReportSuppression(Suppression.Create(_rule, diagnostic));
        }
    }

    private static bool IsPatchMethod(IMethodSymbol method)
    {
        if (HasHarmonyAttribute(method)) return true;
        if (_patchMethodNames.Contains(method.Name) && HasHarmonyAttribute(method.ContainingType)) return true;

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (parameter.Name.StartsWith("__", StringComparison.Ordinal)) return true;
        }

        return false;
    }

    /// <summary>Collects methods named in <c>nameof</c> in files that import HarmonyLib.</summary>
    private static HashSet<IMethodSymbol> NamedInHarmonyFiles(SuppressionAnalysisContext context)
    {
        var methods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            SyntaxNode root = tree.GetRoot(context.CancellationToken);
            if (root.DescendantNodes().OfType<UsingDirectiveSyntax>().All(static u => u.Name?.ToString() != "HarmonyLib")) continue;

            SemanticModel model = null;
            foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (call.Expression is not IdentifierNameSyntax { Identifier.ValueText: "nameof" } || call.ArgumentList.Arguments.Count != 1) continue;

                model ??= context.GetSemanticModel(tree);
                SymbolInfo info = model.GetSymbolInfo(call.ArgumentList.Arguments[0].Expression, context.CancellationToken);
                foreach (ISymbol candidate in info.Symbol != null ? [info.Symbol] : info.CandidateSymbols)
                {
                    if (candidate is IMethodSymbol named) methods.Add(named.OriginalDefinition);
                }
            }
        }

        return methods;
    }

    private static bool HasHarmonyAttribute(ISymbol symbol)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == "HarmonyLib") return true;
        }

        return false;
    }
}
