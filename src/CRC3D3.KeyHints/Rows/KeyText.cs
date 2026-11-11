using BepInEx.Configuration;
using UnityEngine;

namespace CRC3D3.KeyHints.Rows;

/// <summary>Short key names for hint rows.</summary>
internal static class KeyText
{
    /// <summary>Builds short key text: "Ctrl+F8", "N", "Space".</summary>
    /// <returns>Empty text for unset key.</returns>
    /// <remarks>Left and right modifier keys get same name.</remarks>
    public static string Of(KeyboardShortcut key)
    {
        if (key.MainKey == KeyCode.None)
        {
            return "";
        }

        string text = "";
        foreach (KeyCode m in key.Modifiers)
        {
            text += Name(m) + "+";
        }

        return text + Name(key.MainKey);
    }

    private static string Name(KeyCode k) => k switch
    {
        KeyCode.LeftControl or KeyCode.RightControl => "Ctrl",
        KeyCode.LeftShift or KeyCode.RightShift => "Shift",
        KeyCode.LeftAlt or KeyCode.RightAlt => "Alt",
        KeyCode.Escape => "Esc",
        KeyCode.Return => "Enter",
        _ => Short(k.ToString()),
    };

    private static string Short(string name) =>
        name.StartsWith("Alpha", StringComparison.Ordinal) && name.Length == 6 ? name.Substring(5)
        : name.StartsWith("Keypad", StringComparison.Ordinal) ? "Num" + name.Substring(6)
        : name;
}
