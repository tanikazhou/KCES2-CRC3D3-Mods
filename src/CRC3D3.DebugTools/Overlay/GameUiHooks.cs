using CRC;
using CRC.UI;
using HarmonyLib;

namespace CRC3D3.DebugTools.Overlay;

/// <summary>Hooks on game UI life cycle for overlay: follow character panel references, game font, follow panel shown.</summary>
/// <remarks>
/// <c>GameUIAdapter.LoadComponent</c> runs at start and after each UI rebuild (<c>ReCreateGameUI</c>); <c>GameUIAdapter.OnDestroy</c> ends that UI.
/// Follow character panel shows or hides with main menu window: <c>MainMenuManager.ShowHeroineParameter</c> (menu opens),
/// <c>OtherIUOpen</c> (sub-screen hides window), <c>ReturnToBulkUI</c>, <c>ReturnToBulkUI_Status</c> (window back), <c>Close</c>.
/// </remarks>
internal static class GameUiHooks
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameUIAdapter), nameof(GameUIAdapter.LoadComponent))]
    private static void AfterLoadComponent(GameUIAdapter __instance)
    {
        if (Plugin.Guard.IsOff(DebugOverlay.Feature)) return;

        try
        {
            FollowPanel.Capture(__instance);
            DebugOverlay.UseGameFont(__instance);
        }
        catch (Exception e)
        {
            Plugin.Guard.Fail(DebugOverlay.Feature, e);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameUIAdapter), "OnDestroy")]
    private static void BeforeDestroy(GameUIAdapter __instance) => FollowPanel.Forget(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.ShowHeroineParameter))]
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OtherIUOpen))]
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.ReturnToBulkUI))]
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.ReturnToBulkUI_Status))]
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Close), typeof(bool), typeof(Action))]
    private static void AfterMenuWindowChange() => DebugOverlay.TrackFollowPanel();
}
