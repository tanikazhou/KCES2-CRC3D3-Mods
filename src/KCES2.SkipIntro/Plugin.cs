using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KCES2.Fusebox;

namespace KCES2.SkipIntro;

/// <summary>Skips logo and caution screen in KCES2 and CRC3D3.</summary>
/// <remarks>One DLL for both programs, built against KCES2 references: it uses only types and members that both Assembly-CSharp builds have.</remarks>
[BepInPlugin(Guid, "KCES2 Skip Intro", Version)]
[BepInProcess("CR EditSystem.exe")]
[BepInProcess("CRC 3D 3.exe")]
public class Plugin : BaseUnityPlugin
{
    /// <summary>Plugin GUID, also Harmony instance ID.</summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Guid = "kces2.skipintro";

    /// <summary>Plugin version.</summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Version = "0.1.0";

    internal static ManualLogSource Log;

    internal static FeatureGuard Guard;

    /// <remarks>Never dispose: HarmonyX <c>Dispose</c> calls <see cref="Harmony.UnpatchSelf"/>.</remarks>
    private static Harmony _harmony;

    private void Awake()
    {
        Log = Logger;
        Guard = new FeatureGuard(Log);
        var settings = new SettingList(Config);
        _harmony = new Harmony(Guid);

        Guard.Start(StartupSkip.Feature, () =>
        {
            StartupSkip.Bind(settings);
            _harmony.PatchAll(typeof(StartupSkip.Hooks));
        });
    }
}
