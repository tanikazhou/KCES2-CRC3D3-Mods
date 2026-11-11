using System.Text;
using BepInEx.Configuration;
using CRC;
using CRC.Database;
using CrcMove.Event;
using CrcMove.UI;
using HarmonyLib;
using KCES2.Fusebox;
using UnityEngine;

namespace CRC3D3.DebugTools.Overlay;

/// <summary>Overlay lines for field event points in player range: event row, script, conditions, game's last availability check.</summary>
/// <remarks>
/// <para>
/// Event driven, no polling: hooks on <see cref="MoveEventPoint"/> trigger enter, exit and disable keep list of points in range;
/// hooks on prompt changes (<c>SimpleExplanationPop.Open</c>, <c>EventStartImage.RemoveCanStartEvent</c>, <c>ClearCanStartEvents</c>)
/// and on row changes (<see cref="MoveEventPoint.Setup"/>) drop cached text and mark overlay dirty. Text is built only then; overlay
/// refresh for other sections (player position) appends cached text.
/// </para>
/// <para>
/// Availability is result of game's own <c>CanShowEventNow</c> calls (trigger enter, start key, <c>RefreshFieldEvents</c>), with game time of
/// that call. Overlay never calls it: it runs TJS conditions and swaps character slot 0. Static row text is cached per row. Point whose
/// prompt shows (<c>EventStartImage.usedTrans</c>) is marked with <c>&gt;</c>; prompt of other kind (shop, elevator) shows its type.
/// </para>
/// <para>
/// Script source: <c>GameResource.NativeFileManager.fileSystem</c> answers through plugin callbacks (script files added by plugins), its
/// <see cref="FileSystemHook.baseSystem"/> only from game archives. Answers are cached until no point is in range.
/// </para>
/// </remarks>
internal static class EventInfo
{
    internal const string Feature = "EventInfo";

    private static readonly AccessTools.FieldRef<Transform> _promptPoint =
        AccessTools.StaticFieldRefAccess<Transform>(AccessTools.Field(typeof(EventStartImage), "usedTrans"));

    private static readonly AccessTools.FieldRef<MoveEventPoint, bool> _inRangeFlag = AccessTools.FieldRefAccess<MoveEventPoint, bool>("_isPlayerInRange");

    private static readonly List<MoveEventPoint> _inRange = [];
    private static readonly Dictionary<MoveEventPoint, (bool Available, TimeSpan? At)> _checks = [];
    private static readonly Dictionary<Field.FieldEvent, (string Type, string Script, string Lines)> _rows = [];
    private static readonly Dictionary<string, string> _sources = [];
    private static readonly StringBuilder _build = new(1024);

    private static ConfigEntry<bool> _enabled;
    private static string _text;

    internal static void Bind(SettingList settings)
    {
        _enabled = settings.Bind("Overlay", "EventInfo", true,
            "Show the field event points in range in the debug overlay: event id, start type, the game's last availability check, "
            + "script file and label (and whether the file exists), prompt, time and character conditions, linked character.");
        _enabled.SettingChanged += static (_, _) => Changed();
    }

    /// <summary>Appends event lines; appends nothing when no point is in range and no prompt shows.</summary>
    /// <remarks>Text is built again only after own hook or setting change (<see cref="Changed"/>), not on refresh for other sections.</remarks>
    internal static void Write(StringBuilder sb)
    {
        if (_text == null)
        {
            _build.Clear();
            Build(_build);
            _text = _build.ToString();
        }

        sb.Append(_text);
    }

    /// <summary>Drops cached text, so next refresh builds event lines again (overlay turned on: script source can be known now).</summary>
    internal static void DropCache() => _text = null;

    private static void Build(StringBuilder sb)
    {
        if (!_enabled.Value) return;

        Transform prompt = _promptPoint();
        bool hasPrompt = prompt != null;
        if (_inRange.Count == 0 && !hasPrompt) return;

        sb.AppendLine().AppendLine("Events in range");
        bool promptListed = false;
        foreach (MoveEventPoint e in _inRange)
        {
            bool shown = hasPrompt && e.transform == prompt;
            promptListed |= shown;
            WritePoint(sb, e, shown);
        }

        if (hasPrompt && !promptListed)
        {
            MonoBehaviour owner = prompt.GetComponent<MonoBehaviour>();
            sb.Append("> prompt of ").Append(owner != null ? owner.GetType().Name : "object").Append(' ').Append(prompt.name).AppendLine();
        }
    }

    private static void WritePoint(StringBuilder sb, MoveEventPoint e, bool shown)
    {
        sb.Append(shown ? "> " : "  ").Append(e.name);
        Field.FieldEvent f = e.fieldEvent;
        if (f == null)
        {
            sb.AppendLine("  (no event row)");
            return;
        }

        (string type, string script, string lines) = Row(f);
        sb.Append("  ").Append(type);
        if (_checks.TryGetValue(e, out (bool Available, TimeSpan? At) check))
        {
            sb.Append(check.Available ? "  available" : "  not available");
            if (check.At is { } at) sb.Append(" at ").Append(Clock(at));
        }
        else
        {
            sb.Append("  no game check yet");
        }

        sb.AppendLine().Append(script).Append("  (").Append(Source(f.loadScript.fileName)).Append(')').AppendLine().Append(lines);
        if (e.maid != null) sb.Append("    character ").Append(e.maid.baseStatus?.GetFullName() ?? e.maid.name).AppendLine();
    }

    /// <summary>Gets cached text of event row: start type, script line without its source, lines for prompt, time and character conditions.</summary>
    /// <remarks>Script source is not cached here: it can change (file system ready later, file added by plugin), see <see cref="Source"/>.</remarks>
    private static (string Type, string Script, string Lines) Row(Field.FieldEvent f)
    {
        if (_rows.TryGetValue(f, out (string Type, string Script, string Lines) row)) return row;

        string script = $"    script {f.loadScript.fileName} {f.loadScript.labelName}";
        var sb = new StringBuilder();
        sb.Append("    prompt \"").Append(f.interactPrompt.text).Append('"').AppendLine();
        sb.Append("    days ").Append(Join(f.availableDaySections)).Append("  times ").Append(Join(f.availableTimeSections))
            .Append("  ranges ").Append(Ranges(f.availableTimeRanges)).Append("  limit ").Append(f.executionLimitType).AppendLine();
        if (f.requiresHeroineFollow || f.requiresWorkLocationId != 0)
        {
            sb.Append("    needs ").Append(f.requiresHeroineFollow ? "follower " : string.Empty)
                .Append(f.requiresWorkLocationId != 0 ? "work spot " + f.requiresWorkLocationId : string.Empty).AppendLine();
        }

        if (!string.IsNullOrEmpty(f.availableEvalScript)) sb.Append("    if ").Append(f.availableEvalScript).AppendLine();
        if (!string.IsNullOrEmpty(f.availableCharaEvalScript)) sb.Append("    if character ").Append(f.availableCharaEvalScript).AppendLine();
        row = (f.eventType.ToString(), script, sb.ToString());
        _rows[f] = row;
        return row;
    }

    /// <summary>Gets where script file comes from.</summary>
    private static string Source(string file)
    {
        if (string.IsNullOrEmpty(file)) return "no file";
        if (_sources.TryGetValue(file, out string known)) return known;

        FileSystemHook fs = GameResource.NativeFileManager?.fileSystem;
        if (fs == null) return "file system not ready";

        string source = fs.baseSystem != null && fs.baseSystem.IsExistentFile(file) ? "game archive"
            : fs.IsExistentFile(file) ? "added by plugin"
            : "MISSING";

        _sources[file] = source;
        return source;
    }

    private static string Join<T>(HashSet<T> set) => set == null || set.Count == 0 ? "any" : string.Join(",", set);

    private static string Ranges(List<GameTime.Range> ranges)
    {
        if (ranges == null || ranges.Count == 0) return "any";

        var sb = new StringBuilder();
        foreach (GameTime.Range r in ranges)
        {
            if (sb.Length > 0) sb.Append(',');
            sb.Append(Clock(r.Start)).Append('-').Append(Clock(r.End));
        }

        return sb.ToString();
    }

    /// <summary>Formats game time as hours and minutes; hours go past 24 (day end is 26:00).</summary>
    private static string Clock(TimeSpan t) => $"{(int)t.TotalHours:00}:{t.Minutes:00}";

    /// <summary>Drops cached text and marks overlay dirty.</summary>
    private static void Changed()
    {
        _text = null;
        DebugOverlay.MarkDirty();
    }

    /// <summary>Adds point to or removes it from in-range list after trigger enter or exit, from its range flag.</summary>
    private static void Track(MoveEventPoint e)
    {
        bool inside = _inRangeFlag(e);
        int index = IndexOf(e);
        if (inside == (index >= 0)) return;

        if (inside)
        {
            _inRange.Add(e);
        }
        else
        {
            _inRange.RemoveAt(index);
            if (_inRange.Count == 0) _sources.Clear();
        }

        Changed();
    }

    private static void Forget(MoveEventPoint e)
    {
        _checks.Remove(e);
        int index = IndexOf(e);
        if (index < 0) return;

        _inRange.RemoveAt(index);
        if (_inRange.Count == 0) _sources.Clear();
        Changed();
    }

    private static int IndexOf(MoveEventPoint e)
    {
        for (int i = 0; i < _inRange.Count; i++)
        {
            if (ReferenceEquals(_inRange[i], e)) return i;
        }

        return -1;
    }

    private static void Record(MoveEventPoint e, bool available)
    {
        GameManager game = GameMain.GameManager;
        GameClockManager clock = game != null ? game.gameClockManager : null;
        _checks[e] = (available, clock != null ? clock.Time : null);
        if (IndexOf(e) >= 0) Changed();
    }

    internal static class Hooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MoveEventPoint), "OnTriggerEnter")]
        [HarmonyPatch(typeof(MoveEventPoint), "OnTriggerExit")]
        private static void AfterTrigger(MoveEventPoint __instance)
        {
            if (Plugin.Guard.IsOff(Feature)) return;

            try
            {
                Track(__instance);
            }
            catch (Exception e)
            {
                Plugin.Guard.Fail(Feature, e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MoveEventPoint), "OnDisable")]
        private static void AfterDisable(MoveEventPoint __instance)
        {
            if (Plugin.Guard.IsOff(Feature)) return;

            try
            {
                Forget(__instance);
            }
            catch (Exception e)
            {
                Plugin.Guard.Fail(Feature, e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MoveEventPoint), "CanShowEventNow")]
        private static void AfterCanShow(MoveEventPoint __instance, bool __result)
        {
            if (Plugin.Guard.IsOff(Feature)) return;

            try
            {
                Record(__instance, __result);
            }
            catch (Exception e)
            {
                Plugin.Guard.Fail(Feature, e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MoveEventPoint), nameof(MoveEventPoint.Setup))]
        [HarmonyPatch(typeof(SimpleExplanationPop), nameof(SimpleExplanationPop.Open), typeof(Transform), typeof(Vector3), typeof(string))]
        [HarmonyPatch(typeof(EventStartImage), nameof(EventStartImage.RemoveCanStartEvent))]
        [HarmonyPatch(typeof(EventStartImage), nameof(EventStartImage.ClearCanStartEvents))]
        private static void AfterPromptOrRowChange() => Changed();
    }
}
