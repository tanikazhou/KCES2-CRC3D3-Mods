using BepInEx;
using BepInEx.Logging;
using CRC3D3.KeyHints.Game;
using HarmonyLib;
using KCES2.Fusebox;

namespace CRC3D3.KeyHints;

/// <summary>Key hints for CRC3D3: rows for keys that other plugins declare with key hint tags.</summary>
/// <remarks>Plugin <c>Update</c> runs feature ticks; it needs <c>HideManagerGameObject = true</c> in <c>BepInEx.cfg</c>.</remarks>
[BepInPlugin(Guid, "CRC3D3 Key Hints", Version)]
[BepInProcess("CRC 3D 3.exe")]
public class Plugin : BaseUnityPlugin
{
    /// <summary>Plugin GUID, also Harmony instance ID.</summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Guid = "crc3d3.keyhints";

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
        ManagerObjectCheck.Run(Log);
        _harmony = new Harmony(Guid);

        Guard.Start(ContextHooks.Feature, () => _harmony.PatchAll(typeof(ContextHooks)));

        Log.LogInfo($"Hooks: {_harmony.GetPatchedMethods().Count()} game methods patched");
    }
}
