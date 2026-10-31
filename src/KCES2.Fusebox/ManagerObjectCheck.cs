#nullable enable
using BepInEx.Bootstrap;
using BepInEx.Logging;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KCES2.Fusebox;

/// <summary>
/// Check of BepInEx setting HideManagerGameObject.
/// </summary>
/// <remarks>
/// <para>
/// Logs a warning if your BepInEx.cfg doesn't have <c>HideManagerGameObject = true</c>.
/// For people who wonder why their plugin isn't working in KCES2 or CRC3D3 and don't think to check <see cref="Chainloader.ManagerObject"/>.
/// </para>
/// <para>Plugins run as components of BepInEx manager object. KCES2 and CRC3D3 destroy the visible manager object in the first frame,
/// which stops every plugin without error. <c>HideManagerGameObject = true</c> in <c>BepInEx/config/BepInEx.cfg</c> hides the object and keeps it alive.</para>
/// <para>The setting itself is internal to BepInEx, so check reads hide flags of <see cref="Chainloader.ManagerObject"/>
/// (<see cref="HideFlags.HideAndDontSave"/> when setting is on).</para>
/// <para> This isn't because of game-specific code, it's just Unity cleaning house before the first scene load.
/// See also BepInEx#222. Objects that have <c>DontUnloadUnusedAsset</c> stay.</para>
/// </remarks>
internal static class ManagerObjectCheck
{
    /// <summary>Logs warning when BepInEx manager object is not hidden.</summary>
    /// <param name="log">Plugin log.</param>
    /// <returns><see langword="true"/> if manager object is hidden; otherwise, <see langword="false"/>.</returns>
    public static bool Run(ManualLogSource log)
    {
        GameObject manager = Chainloader.ManagerObject;
        if (manager != null && manager.hideFlags == HideFlags.HideAndDontSave) return true;

        log.LogWarning("HideManagerGameObject is off in BepInEx/config/BepInEx.cfg. KCES2 and CRC3D3 destroy the BepInEx manager object, which stops this plugin. Set it to true.");
        return false;
    }
}
