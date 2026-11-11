using BepInEx;
using BepInEx.Logging;
using CRC3D3.DebugTools.Overlay;
using HarmonyLib;
using KCES2.Fusebox;

namespace CRC3D3.DebugTools;

/// <summary>Debug tools for CRC3D3: on-screen debug overlay.</summary>
/// <remarks>Plugin <c>Update</c> runs feature ticks; it needs <c>HideManagerGameObject = true</c> in <c>BepInEx.cfg</c>.</remarks>
[BepInPlugin(Guid, "CRC3D3 Debug Tools", Version)]
[BepInProcess("CRC 3D 3.exe")]
public class Plugin : BaseUnityPlugin
{
    /// <summary>Plugin GUID, also Harmony instance ID.</summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Guid = "crc3d3.debugtools";

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
        ManagerObjectCheck.Run(Log);
        _harmony = new Harmony(Guid);

        Guard.Start(DebugOverlay.Feature, () =>
        {
            DebugOverlay.Bind(settings);
            _harmony.PatchAll(typeof(GameUiHooks));
            Guard.AddTick(DebugOverlay.Feature, DebugOverlay.Tick);
        }, [DebugOverlay.Hint]);

        Guard.Start(EventInfo.Feature, () =>
        {
            EventInfo.Bind(settings);
            _harmony.PatchAll(typeof(EventInfo.Hooks));
        });

        Log.LogInfo($"Hooks: {_harmony.GetPatchedMethods().Count()} game methods patched");
    }

    private void Update() => Guard.TickAll();
}
