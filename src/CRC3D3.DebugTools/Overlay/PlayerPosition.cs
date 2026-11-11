using System.Text;
using BepInEx.Configuration;
using CRC;
using CrcMove.Controller;
using KCES2.Fusebox;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CRC3D3.DebugTools.Overlay;

/// <summary>Overlay line with player position and facing, and key that copies them in script tag form.</summary>
/// <remarks>
/// <para>
/// Player movement has no game event, so this section polls: while overlay is on and line is enabled, player transform is read
/// <c>PositionUpdatesPerSecond</c> times per second, rounded to screen precision and compared with last written values. Only a change
/// marks overlay dirty, so standing still costs one read per tick and no text rebuild. <see cref="Write"/> reads transform again, so
/// overlay never shows stale values after it turns on. Source: <see cref="PlayerMoveController.Instance"/> transform, which noclip also moves.
/// </para>
/// <para>
/// Copy key works while overlay is hidden too. It reads transform at key press, puts text on system clipboard, writes it to log and shows
/// game system message "Position copied"; at most one message per <see cref="NoticeSeconds"/>, game shows queued messages that far apart.
/// Key does nothing while text field has focus or without player controller.
/// </para>
/// </remarks>
internal static class PlayerPosition
{
    internal const string Feature = "PlayerPosition";

    /// <summary>Key hint row for copy key, shown in town while CRC3D3.KeyHints runs.</summary>
    internal static readonly KeyHint Hint = new() { Context = "Field", Order = 310, Label = "Copy position" };

    private const string Notice = "Position copied";
    private const float NoticeSeconds = 2f;

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<KeyboardShortcut> _copyKey;
    private static ConfigEntry<int> _rate;
    private static (long X, long Y, long Z, long Facing) _last;
    private static bool _written;
    private static bool _loggedShown;
    private static float _nextRead;
    private static float _noticeFreeAt;

    internal static void Bind(SettingList settings)
    {
        _enabled = settings.Bind("Overlay", "Position", true,
            "Show the player's position (X, Y, Z) and facing as the first line of the debug overlay.");
        _copyKey = settings.Bind("Overlay", "PositionCopyKey", new KeyboardShortcut(KeyCode.P, KeyCode.LeftShift),
            "Key that copies the player's position and facing to the clipboard and the log, as script tag values: "
            + "pos={\"x\":0.000,\"y\":0.000,\"z\":0.000} rot={\"x\":0,\"y\":0.0,\"z\":0}. Works while the debug overlay is hidden too.",
            hint: Hint);
        _rate = settings.BindAdvanced("Overlay", "PositionUpdatesPerSecond", 5,
            "How often the position line reads the player position while it shows.",
            new AcceptableValueRange<int>(1, 30));
        _enabled.SettingChanged += static (_, _) => DebugOverlay.MarkDirty();
        Plugin.Log.LogInfo($"Position: line {(_enabled.Value ? "on" : "off")}, copy key {_copyKey.Value}, {_rate.Value} reads per second");
    }

    /// <summary>Handles copy key; while line shows, reads player position on timer and marks overlay dirty when screen text changes.</summary>
    internal static void Tick()
    {
        if (KeyInput.IsDownEitherSide(_copyKey.Value)) Copy();
        if (!DebugOverlay.IsOn || !_enabled.Value || Plugin.Guard.IsOff(DebugOverlay.Feature)) return;

        float now = Time.unscaledTime;
        if (now < _nextRead) return;

        _nextRead = now + (1f / Mathf.Max(1, _rate.Value));
        ReadOnTimer();
    }

    /// <summary>Appends position line; appends nothing while line is off or no player controller exists.</summary>
    internal static void Write(StringBuilder sb)
    {
        _written = false;
        if (!_enabled.Value) return;

        PlayerMoveController player = PlayerMoveController.Instance;
        if (player == null) return;

        _last = Read(player);
        _written = true;
        PositionText.AppendScreenLine(sb, _last);
        sb.AppendLine();
        if (_loggedShown) return;

        _loggedShown = true;
        Plugin.Log.LogInfo("Position line shown in debug overlay");
    }

    // ReSharper disable Unity.PerformanceAnalysis - Runs PositionUpdatesPerSecond times per second (default 5), only while line shows.
    private static void ReadOnTimer()
    {
        PlayerMoveController player = PlayerMoveController.Instance;
        if (player == null)
        {
            if (_written) DebugOverlay.MarkDirty();
            return;
        }

        if (!_written || Read(player) != _last) DebugOverlay.MarkDirty();
    }

    private static (long X, long Y, long Z, long Facing) Read(PlayerMoveController player)
    {
        Transform t = player.transform;
        Vector3 p = t.position;
        return PositionText.Round(p.x, p.y, p.z, t.eulerAngles.y);
    }

    // ReSharper disable Unity.PerformanceAnalysis - Runs only on copy key press.
    private static void Copy()
    {
        PlayerMoveController player = PlayerMoveController.Instance;
        if (player == null || TextFieldFocused()) return;

        Transform t = player.transform;
        Vector3 p = t.position;
        string text = PositionText.CopyText(p.x, p.y, p.z, t.eulerAngles.y);
        string clipboard;
        try
        {
            GUIUtility.systemCopyBuffer = text;
            clipboard = GUIUtility.systemCopyBuffer == text ? "clipboard set" : "clipboard read back differs";
        }
        catch (Exception e)
        {
            clipboard = "clipboard failed: " + e.Message;
        }

        Plugin.Log.LogInfo($"Position copied: {text} ({clipboard})");
        ShowNotice();
    }

    private static void ShowNotice()
    {
        float now = Time.unscaledTime;
        GameManager game = GameMain.GameManager;
        if (now < _noticeFreeAt || game == null || game.gameUI == null || game.gameUI.systemMessagesManager == null) return;

        _noticeFreeAt = now + NoticeSeconds;
        try
        {
            game.CallSystemMessage(Notice);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning("Position copied, system message failed: " + e.Message);
        }
    }

    private static bool TextFieldFocused()
    {
        EventSystem events = EventSystem.current;
        GameObject selected = events != null ? events.currentSelectedGameObject : null;
        if (selected == null) return false;

        TMP_InputField tmp = selected.GetComponent<TMP_InputField>();
        if (tmp != null && tmp.isFocused) return true;

        InputField legacy = selected.GetComponent<InputField>();
        return legacy != null && legacy.isFocused;
    }
}
