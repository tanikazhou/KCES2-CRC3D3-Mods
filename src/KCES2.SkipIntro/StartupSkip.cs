using System.Collections;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using KCES2.Fusebox;

namespace KCES2.SkipIntro;

/// <summary>Skips logo and caution screen at start.</summary>
/// <remarks>
/// <para>
/// <c>SceneFirst.OnSetupCompleted</c> starts <c>OnSetUpCheck</c>: waits until character manager is idle, then fades in logo.
/// Click cross-fades to caution screen, second click calls <see cref="SceneFirst.NextScene"/>.
/// Prefix waits same way, hides load icon and calls <see cref="SceneFirst.NextScene"/> at once.
/// </para>
/// <para>
/// KCES2 takes same path itself when CRC3D3 starts it (bridge mode).
/// <see cref="SceneFirst.NextScene"/> picks next scene in each program: CRC3D3 title, KCES2 gender select or KCES2 edit scene.
/// </para>
/// <para>On error, feature turns off and <c>OnSetupCompleted</c> runs again, so logo and caution screen appear as without plugin.</para>
/// </remarks>
internal static class StartupSkip
{
    internal const string Feature = "StartupSkip";

    private static ConfigEntry<bool> _skip;

    internal static void Bind(SettingList settings) =>
        _skip = settings.Bind("Startup", "SkipStartupScreens", true, "Skip the logo and the caution screen at start.");

    private static IEnumerator WaitThenNext(SceneFirst scene, MethodBase setupCompleted)
    {
        while (true)
        {
            bool busy;
            try
            {
                busy = GameMain.CharacterManager.isBusy;
            }
            catch (Exception e)
            {
                Fallback(scene, setupCompleted, e);
                yield break;
            }

            if (!busy) break;
            yield return null;
        }

        // Load icon is cosmetic: go on if it fails.
        try
        {
            MonoSingleton<SystemLoadIcon>.Instance.forcedVisible = false;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning("Startup skip: load icon not hidden: " + e.Message);
        }

        try
        {
            scene.NextScene();
            Plugin.Log.LogInfo("Startup screens skipped");
        }
        catch (Exception e)
        {
            Fallback(scene, setupCompleted, e);
        }
    }

    /// <summary>Turns feature off and runs start with logo and caution screen.</summary>
    /// <remarks>Call goes through patched method: prefix sees feature off and lets code run.</remarks>
    private static void Fallback(SceneFirst scene, MethodBase setupCompleted, Exception e)
    {
        Plugin.Guard.Fail(Feature, e);
        // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison - One-time call (not on hot path).
        if (scene == null) return;

        try
        {
            setupCompleted.Invoke(scene, null);
            Plugin.Log.LogInfo("Startup skip: startup screens shown instead");
        }
        catch (Exception e2)
        {
            Plugin.Log.LogError("Startup skip: startup screens failed too: " + e2);
        }
    }

    internal static class Hooks
    {
        /// <summary>Starts skip coroutine in place of start.</summary>
        /// <returns><see langword="false"/> to skip default startup code; otherwise, <see langword="true"/>.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneFirst), "OnSetupCompleted")]
        private static bool BeforeSetupCompleted(SceneFirst __instance, MethodBase __originalMethod)
        {
            if (Plugin.Guard.IsOff(Feature) || !_skip.Value) return true;

            try
            {
                __instance.StartCoroutine(WaitThenNext(__instance, __originalMethod));
                return false;
            }
            catch (Exception e)
            {
                Plugin.Guard.Fail(Feature, e);
                return true;
            }
        }
    }
}
