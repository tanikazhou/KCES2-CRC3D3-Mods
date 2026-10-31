using Microsoft.CodeAnalysis;

namespace KCES2.Fusebox.Style;

/// <summary>Diagnostics of repo style analyzers.</summary>
/// <remarks>Rules: CODE_STYLE.md at repo root of KCES2-CRC3D3-Mods.</remarks>
internal static class Descriptors
{
    /// <summary>Max characters per C# line.</summary>
    public const int MaxLineLength = 160;

    /// <summary>C# line over 160 characters.</summary>
    public static readonly DiagnosticDescriptor LineTooLong = new(
        "KFB0001",
        "Line too long",
        "Line has {0} characters, max {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Wrap at commas or operators. Exempt: string literal or URL that covers column 160.");

    /// <summary>Blank line directly after opening brace or before closing brace.</summary>
    public static readonly DiagnosticDescriptor BlankLineAtBrace = new(
        "KFB0002",
        "Blank line next to brace",
        "Remove blank line {0} brace",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>Project file is not well-formed XML.</summary>
    public static readonly DiagnosticDescriptor ProjectFileInvalid = new(
        "KFB0003",
        "Project file not well-formed",
        "{0}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>Project file layout: indent, tabs, line ends, trailing spaces, final newline.</summary>
    public static readonly DiagnosticDescriptor ProjectFileLayout = new(
        "KFB0004",
        "Project file layout",
        "{0}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "2-space indent per element level, no tabs, LF, no trailing spaces, final newline.");

    /// <summary>C# file with CR line ends.</summary>
    public static readonly DiagnosticDescriptor SourceLineEnds = new(
        "KFB0005",
        "C# line ends",
        "{0}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "LF line ends.");

    /// <summary>Embedded statement on its own line without braces.</summary>
    public static readonly DiagnosticDescriptor BracesOnOwnLine = new(
        "KFB0006",
        "Braces required",
        "Statement on its own line needs braces; or put one-line guard on same line",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>Class with primary constructor.</summary>
    public static readonly DiagnosticDescriptor PrimaryConstructorOnClass = new(
        "KFB0008",
        "Explicit constructor required",
        "Class '{0}' has primary constructor; declare explicit constructor",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Primary constructors only on records and structs.");

    private const string Category = "Style";
}
