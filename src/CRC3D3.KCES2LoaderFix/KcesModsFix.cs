using System.Diagnostics;
using BepInEx.Configuration;
using KCES2.Fusebox;

namespace CRC3D3.KCES2LoaderFix;

/// <summary>Lets BepInEx load in KCES2 when CRC3D3 starts it (Edit buttons, <c>@ToEdit</c>).</summary>
/// <remarks>
/// <para>
/// Doorstop 4 sets <c>DOORSTOP_INITIALIZED</c>, <c>DOORSTOP_DISABLE</c> and other <c>DOORSTOP_*</c> variables in CRC3D3 process.
/// <see cref="CRCEdit.EditBridge"/> starts <c>CR EditSystem.exe</c> (or its launcher) with <see cref="Process.Start()"/>, so KCES2 inherits
/// CRC3D3 environment. KCES2 Doorstop sees either variable and loads nothing: KCES2 runs without BepInEx and translation.
/// </para>
/// <para>
/// Fix removes Doorstop variables from CRC3D3 environment once, at plugin start. BepInEx reads them before plugins load, and no later
/// code in CRC3D3 reads them. Every process that CRC3D3 starts afterwards inherits environment without them.
/// </para>
/// </remarks>
internal static class KcesModsFix
{
    internal const string Feature = "KcesModsFix";

    internal static void Run(SettingList settings)
    {
        ConfigEntry<bool> enabled = settings.Bind("Fixes", "LoadModsInKCES2", true,
            "Start KCES2 without the Doorstop variables of CRC3D3, so that BepInEx and the KCES2 plugins load in KCES2 too when CRC3D3 starts it. "
            + "Takes effect at next CRC3D3 start.");
        if (!enabled.Value)
        {
            Plugin.Log.LogInfo("KCES2 start fix off: Doorstop variables stay in CRC3D3 environment");
            return;
        }

        List<string> names = DoorstopEnv.Names(Environment.GetEnvironmentVariables());
        foreach (string name in names) Environment.SetEnvironmentVariable(name, null);

        Plugin.Log.LogInfo("Doorstop variables removed from CRC3D3 environment, so KCES2 started by CRC3D3 loads BepInEx: "
            + (names.Count > 0 ? string.Join(", ", names) : "none"));
    }
}
