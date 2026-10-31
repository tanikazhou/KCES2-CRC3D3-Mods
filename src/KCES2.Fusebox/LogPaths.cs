#nullable enable
using System.Text;

// ReSharper disable once CheckNamespace
namespace KCES2.Fusebox;

// ReSharper disable RedundantSuppressNullableWarningExpression
// net48 string.IsNullOrEmpty lacks NotNullWhen, so "!" is needed there; only net10 test context sees it as redundant.

/// <summary>Absolute path removal from paths and exception texts for log.</summary>
/// <remarks>
/// <para>Absolute path, e.g. program folder, can hold account name.</para>
/// <para>Pure code without BepInEx types; folder is parameter. Compares ignore case (Windows and Wine paths).
/// Path counts as inside folder only when separator follows folder name (CRC and CRC2 are different folders).</para>
/// </remarks>
internal static class LogPaths
{
    /// <summary>Returns path relative to <paramref name="root"/>.</summary>
    /// <param name="path">Path to log.</param>
    /// <param name="root">Program folder.</param>
    /// <remarks>Outside folder, or without folder: file name only.</remarks>
    public static string? Rel(string? path, string? root)
    {
        if (string.IsNullOrEmpty(path)) return path;

        string r = Trim(root);
        if (r.Length > 0 && path!.StartsWith(r, StringComparison.OrdinalIgnoreCase))
        {
            if (path.Length == r.Length) return "";
            if (IsSeparator(path[r.Length])) return path.Substring(r.Length).TrimStart('\\', '/');
        }

        return Path.GetFileName(path);
    }

    /// <summary>Replaces <paramref name="root"/> in <paramref name="text"/> with <paramref name="placeholder"/>.</summary>
    /// <param name="text">Exception text or similar.</param>
    /// <param name="root">Program folder.</param>
    /// <param name="placeholder">Text that stands for folder.</param>
    /// <remarks>Both <c>\</c> and <c>/</c> count as separators.</remarks>
    public static string? Scrub(string? text, string? root, string placeholder = ".")
    {
        string r = Trim(root);
        if (string.IsNullOrEmpty(text) || r.Length == 0) return text;

        text = Replace(text, r, placeholder);
        return Replace(text, r.Replace('\\', '/'), placeholder);
    }

    /// <summary>Replaces every <paramref name="old"/> in <paramref name="text"/> with <paramref name="with"/>, ignoring case.</summary>
    /// <remarks>.NET Framework <see cref="string.Replace(string, string)"/> has no comparison option.</remarks>
    public static string? Replace(string? text, string? old, string with)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(old)) return text;
        int i = text!.IndexOf(old, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return text;

        var sb = new StringBuilder(text.Length);
        int from = 0;
        while (i >= 0)
        {
            sb.Append(text, from, i - from).Append(with);
            from = i + old!.Length;
            i = text.IndexOf(old, from, StringComparison.OrdinalIgnoreCase);
        }

        return sb.Append(text, from, text.Length - from).ToString();
    }

    private static string Trim(string? root) => string.IsNullOrEmpty(root) ? "" : root!.TrimEnd('\\', '/');

    private static bool IsSeparator(char c) => c == '\\' || c == '/';
}
