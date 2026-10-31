using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KCES2.Fusebox;

/// <summary>Key tests that treat left and right modifier keys alike.</summary>
internal static class KeyInput
{
    /// <summary>Determines whether shortcut was pressed in this frame.</summary>
    /// <returns><see langword="false"/> always for unset key (<see cref="KeyCode.None"/>).</returns>
    /// <remarks>
    /// Left and right modifier keys count as same key, as in XUnity.AutoTranslator
    /// (BepInEx <see cref="KeyboardShortcut.IsDown"/> checks only written side).
    /// Other held keys do not block shortcut: BepInEx <see cref="KeyboardShortcut.IsDown"/> fails
    /// while any other key is held, e.g. W while walking.
    /// </remarks>
    public static bool IsDownEitherSide(KeyboardShortcut k)
    {
        if (k.MainKey == KeyCode.None || !UnityInput.Current.GetKeyDown(k.MainKey)) return false;
        foreach (KeyCode m in k.Modifiers)
        {
            if (!UnityInput.Current.GetKey(m) && !UnityInput.Current.GetKey(OtherSide(m))) return false;
        }

        return true;
    }

    private static KeyCode OtherSide(KeyCode k)
    {
        return k switch
        {
            KeyCode.LeftAlt => KeyCode.RightAlt,
            KeyCode.RightAlt => KeyCode.LeftAlt,
            KeyCode.LeftControl => KeyCode.RightControl,
            KeyCode.RightControl => KeyCode.LeftControl,
            KeyCode.LeftShift => KeyCode.RightShift,
            KeyCode.RightShift => KeyCode.LeftShift,
            _ => k,
        };
    }
}
