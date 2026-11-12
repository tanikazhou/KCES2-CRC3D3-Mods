using BepInEx;
using BepInEx.Logging;
using KCES2.Fusebox;

namespace CRC3D3.KCES2LoaderFix;

/// <summary>Lets BepInEx load in KCES2 when CRC3D3 starts it.</summary>
[BepInPlugin(Guid, "CRC3D3 KCES2 Loader Fix", Version)]
[BepInProcess("CRC 3D 3.exe")]
[BepInProcess("COM3D2x64.exe")]
public class Plugin : BaseUnityPlugin
{
    /// <summary>Plugin GUID.</summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Guid = "crc3d3.kces2loaderfix";

    /// <summary>Plugin version.</summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Version = "0.1.0";

    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;
        var settings = new SettingList(Config);

        new FeatureGuard(Log).Start(KcesModsFix.Feature, () => KcesModsFix.Run(settings));
    }
}
