using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using KCES2.Fusebox;

namespace CRC3D3.KCES2LoaderFix;

/// <summary>Lets BepInEx load in KCES2 when CRC3D3 starts it (Edit buttons, <c>@ToEdit</c>).</summary>
/// <remarks>
/// <para>
/// Doorstop 4 marks CRC3D3 process with environment variable <c>DOORSTOP_INITIALIZED</c> (and other <c>DOORSTOP_*</c> variables).
/// <see cref="CRCEdit.EditBridge"/> starts <c>CR EditSystem.exe</c> (or its launcher) with <see cref="Process.Start()"/>, so KCES2 inherits
/// CRC3D3 environment. KCES2 Doorstop then sees mark, thinks it already ran and loads nothing: KCES2 runs without BepInEx and translation.
/// </para>
/// <para>
/// <see cref="Environment.SetEnvironmentVariable(string, string)"/> before <c>CallEdit</c> is not enough: <see cref="CRCEdit.EditBridge"/>
/// uses <c>UseShellExecute = true</c>, so child gets native environment and managed change does not reach it.
/// </para>
/// <para>
/// While <c>CallEdit</c> or <c>CallLauncher</c> runs, prefix on <see cref="Process.Start()"/> switches to <c>UseShellExecute = false</c> and
/// gives process explicit environment without <c>DOORSTOP_*</c> variables. It also clears them in native (kernel32) and managed environment.
/// </para>
/// </remarks>
internal static class KcesModsFix
{
    internal const string Feature = "KcesModsFix";

    private static ConfigEntry<bool> _enabled;

    [ThreadStatic]
    private static int _inBridge;

    internal static void Bind(SettingList settings) =>
        _enabled = settings.Bind("Fixes", "LoadModsInKCES2", true,
            "Start KCES2 without the Doorstop variables of CRC3D3, so that BepInEx and the KCES2 plugins load in KCES2 too when CRC3D3 starts it.");

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetEnvironmentVariableW(string name, string value);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetEnvironmentVariableW(string name, StringBuilder buffer, int size);

    private static bool NativeHas(string name)
    {
        try
        {
            return GetEnvironmentVariableW(name, new StringBuilder(2), 2) > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void FixStart(Process p)
    {
        ProcessStartInfo si = p.StartInfo;
        List<string> found = DoorstopEnv.Names(Environment.GetEnvironmentVariables());
        bool nativeMark = NativeHas("DOORSTOP_INITIALIZED");

        // Explicit environment for child: copy of CRC3D3 environment without Doorstop variables.
        si.UseShellExecute = false;
        StringDictionary env = si.EnvironmentVariables;
        List<string> remove = DoorstopEnv.RemoveFrom(env);

        // Also clear them in CRC3D3's own environment (managed and native): CRC3D3 no longer needs them. Best effort.
        foreach (string name in found)
        {
            try
            {
                Environment.SetEnvironmentVariable(name, null);
            }
            catch (Exception)
            {
                // Child environment is already clean.
            }

            try
            {
                SetEnvironmentVariableW(name, null);
            }
            catch (Exception)
            {
                // Child environment is already clean.
            }
        }

        bool overrides = env.ContainsKey("WINEDLLOVERRIDES");
        Plugin.Log.LogInfo($"KCES2 start: {Path.GetFileName(si.FileName)} without Doorstop variables "
            + $"(removed: {(remove.Count > 0 ? string.Join(", ", remove) : "none")}; native mark was "
            + $"{(nativeMark ? "set" : "not set")}; WINEDLLOVERRIDES {(overrides ? "passed on" : "not set")})");
    }

    internal static class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CRCEdit.EditBridge), "CallEdit")]
        [HarmonyPatch(typeof(CRCEdit.EditBridge), "CallLauncher")]
        private static void BeforeBridge() => _inBridge++;

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CRCEdit.EditBridge), "CallEdit")]
        [HarmonyPatch(typeof(CRCEdit.EditBridge), "CallLauncher")]
        private static Exception AfterBridge(Exception __exception)
        {
            if (_inBridge > 0) _inBridge--;
            return __exception;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Process), nameof(Process.Start), [])]
        private static void BeforeProcessStart(Process __instance)
        {
            if (_inBridge == 0 || !_enabled.Value || Plugin.Guard.IsOff(Feature)) return;

            try
            {
                FixStart(__instance);
            }
            catch (Exception e)
            {
                Plugin.Guard.Fail(Feature, e);
            }
        }
    }
}
