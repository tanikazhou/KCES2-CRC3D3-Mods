using CRC;
using CRC3D3.KeyHints.View;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace CRC3D3.KeyHints.Game;

/// <summary>Takes CRC3D3 hint font when game UI loads.</summary>
/// <remarks>
/// First choice: font and material of window hint text (<see cref="TextMeshProUGUI"/> under object "Esc"), as CRC3D3 draws "Esc Close".
/// Else CRC3D3 Japanese font asset from game UI fonts. Until then, TMP default font. No timed search.
/// </remarks>
internal static class FontHooks
{
    /// <summary>Feature name in plugin guard.</summary>
    public const string Feature = "Font";

    private const string JapaneseFont = "ja_BIZUDGothic-Regular SDF";

    private static bool _hintStyle;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameUIAdapter), nameof(GameUIAdapter.LoadComponent))]
    private static void AfterLoadComponent(GameUIAdapter __instance)
    {
        if (_hintStyle || Plugin.Guard.IsOff(Feature)) return;

        try
        {
            Take(__instance);
        }
        catch (Exception e)
        {
            Plugin.Guard.Fail(Feature, e);
        }
    }

    private static void Take(GameUIAdapter ui)
    {
        TextMeshProUGUI[] texts = ui.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI t in texts)
        {
            if (t.font == null || t.transform.parent == null || t.transform.parent.name != "Esc") continue;

            _hintStyle = true;
            HintStack.UseFont(t.font, t.fontSharedMaterial);
            Transform window = t.transform.parent.parent;
            Plugin.Log.LogInfo($"Key hints font: {t.font.name} (game hint style, from {(window != null ? window.name : "")}/Esc/{t.name})");
            Plugin.MarkRowsDirty();
            return;
        }

        foreach (TextMeshProUGUI t in texts)
        {
            TMP_FontAsset font = t.font;
            TMP_FontAsset found = font == null ? null
                : font.name == JapaneseFont ? font
                : font.fallbackFontAssetTable?.Find(static f => f != null && f.name == JapaneseFont);
            if (found == null) continue;

            HintStack.UseFont(found, null);
            Plugin.Log.LogInfo($"Key hints font: {found.name} (no game hint text in this UI, {texts.Length} texts checked)");
            Plugin.MarkRowsDirty();
            return;
        }

        Plugin.Log.LogWarning($"Key hints font: no game font in this UI ({texts.Length} texts checked), TMP default kept");
    }
}
