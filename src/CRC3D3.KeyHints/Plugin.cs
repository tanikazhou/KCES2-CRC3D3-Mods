using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using CRC3D3.KeyHints.Discovery;
using CRC3D3.KeyHints.Game;
using CRC3D3.KeyHints.Rows;
using CRC3D3.KeyHints.View;
using HarmonyLib;
using KCES2.Fusebox;
using UnityEngine;

namespace CRC3D3.KeyHints;

/// <summary>Key hints for CRC3D3: rows for keys that other plugins declare with key hint tags.</summary>
/// <remarks>
/// <para>At start, plugin reads <see cref="KeyHint"/> tags on settings of all loaded plugins (by class name, no reference).
/// Rows rebuild only after a tag change, a setting change, a context change or a font change; no per-frame call into other plugins.</para>
/// <para>Plugin <c>Update</c> runs feature ticks; it needs <c>HideManagerGameObject = true</c> in <c>BepInEx.cfg</c>.</para>
/// </remarks>
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

    private const string Feature = "KeyHints";
    private const int LogLimit = 50;

    private static readonly List<FoundHint> _hints = [];
    private static readonly List<HintState> _states = [];
    private static readonly List<RowText> _rows = [];

    /// <remarks>Settings that carry a tag: only their changes rebuild rows (other settings of same plugin do not).</remarks>
    private static readonly HashSet<ConfigEntryBase> _tagged = [];

    /// <remarks>Never dispose: HarmonyX <c>Dispose</c> calls <see cref="Harmony.UnpatchSelf"/>.</remarks>
    private static Harmony _harmony;

    /// <remarks>Set from any thread (tag change signal), read in <see cref="Tick"/>.</remarks>
    private static volatile bool _dirty = true;

    private static ConfigEntry<bool> _show;
    private static ConfigEntry<KeyboardShortcut> _toggleKey;
    private static ConfigEntry<float> _scale;
    private static int _shownCount = -1;
    private static int _logged;

    /// <summary>Requests row rebuild at next frame.</summary>
    /// <remarks>Safe from any thread.</remarks>
    internal static void MarkRowsDirty() => _dirty = true;

    private static void Tick()
    {
        if (KeyInput.IsDownEitherSide(_toggleKey.Value))
        {
            _show.Value = !_show.Value;
            Log.LogInfo($"Key hints {(_show.Value ? "shown" : "hidden")}");
        }

        if (!_dirty) return;

        _dirty = false;
        Rebuild();
    }

    private static void Rebuild()
    {
        if (!_show.Value)
        {
            HintStack.Hide();
            return;
        }

        _states.Clear();
        foreach (FoundHint hint in _hints)
        {
            _states.Add(hint.Read());
        }

        RowPlanner.Plan(_hints, _states, ContextHooks.Contexts, _toggleKey.Value, _rows);
        HintStack.Show(_rows, _scale.Value);

        if (_rows.Count != _shownCount && _logged < LogLimit)
        {
            _shownCount = _rows.Count;
            _logged++;
            Log.LogInfo($"Key hints shown: {_rows.Count} rows, font {HintStack.FontName}");
        }
    }

    /// <summary>Reads key hint tags of all loaded plugins.</summary>
    private static void Discover()
    {
        Action onChanged = MarkRowsDirty;
        int plugins = 0;
        foreach (PluginInfo info in Chainloader.PluginInfos.Values)
        {
            BaseUnityPlugin plugin = info.Instance;
            if (plugin == null) continue;

            int before = _hints.Count;
            TagReader.Read(info.Metadata.GUID, plugin.Config, onChanged, Log, _hints);
            if (_hints.Count == before) continue;

            for (int i = before; i < _hints.Count; i++)
            {
                _tagged.Add(_hints[i].Entry);
            }

            plugins++;
            plugin.Config.SettingChanged += OnSetting;
        }

        Log.LogInfo($"Key hints: {_hints.Count} tags in {plugins} plugins");
        foreach (FoundHint hint in _hints)
        {
            Log.LogInfo($"Key hint found: {hint.PluginGuid} [{hint.Entry.Definition.Section}] {hint.Entry.Definition.Key}, contexts "
                        + (hint.Contexts.Length == 0 ? "all" : string.Join(",", hint.Contexts)));
            foreach (string c in hint.Contexts)
            {
                if (!ContextSet.IsKnown(c)) Log.LogWarning($"Key hint {hint.PluginGuid} {hint.Entry.Definition.Key}: unknown context {c}, row never shows");
            }
        }

        MarkRowsDirty();

        static void OnSetting(object sender, SettingChangedEventArgs e)
        {
            if (_tagged.Contains(e.ChangedSetting)) MarkRowsDirty();
        }
    }

    private void Awake()
    {
        Log = Logger;
        Guard = new FeatureGuard(Log);
        var settings = new SettingList(Config);
        ManagerObjectCheck.Run(Log);
        _harmony = new Harmony(Guid);

        Guard.Start(Feature, () =>
        {
            _show = settings.Bind("Hints", "ShowKeyHints", true, "Show key hints (CRC3D3 style) for plugin features at the bottom right of the screen.");
            _toggleKey = settings.Bind("Hints", "ToggleKey", new KeyboardShortcut(KeyCode.F11), "Hides or shows the key hints.");
            _scale = settings.Bind("Hints", "Scale", 1f, "Size of the key hints (1 = about the size of the CRC3D3 window hints at 1080p).",
                new AcceptableValueRange<float>(0.3f, 3f));
            Config.SettingChanged += static (_, _) => MarkRowsDirty();
            ContextHooks.ContextsChanged += MarkRowsDirty;
            Guard.AddTick(Feature, Tick);
        });

        Guard.Start(ContextHooks.Feature, () => _harmony.PatchAll(typeof(ContextHooks)));
        Guard.Start(FontHooks.Feature, () => _harmony.PatchAll(typeof(FontHooks)));

        Log.LogInfo($"Hooks: {_harmony.GetPatchedMethods().Count()} game methods patched");
    }

    private void Start() => Guard.Run(Feature, Discover);

    private void Update() => Guard.TickAll();
}
