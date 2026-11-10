#nullable enable
using BepInEx.Logging;

// ReSharper disable once CheckNamespace
namespace KCES2.Fusebox;

/// <summary>
/// Per-feature exception fuse guard.
/// Provides methods for catching exceptions per feature, logging once, and turning off only that feature.
/// </summary>
/// <remarks>
/// <para>Catches exceptions per feature, logs once and turns off only that feature for this session.
/// Never writes config file: error must not switch feature off on disk.</para>
/// <para>Lock guards set of stopped features: some plugins call <see cref="Run"/> or <see cref="Fail"/> from loader threads of KCES2
/// and of games that use it.</para>
/// <para>Keep references to KCES2 and game types and members inside lambdas or other methods.
/// In Awake body or static initializer, Mono fails while it compiles method, outside any try.</para>
/// </remarks>
/// <example>
/// Usage (Plugin.Awake):
/// <code>
/// guard = new FeatureGuard(Log);
/// guard.Start("SearchKeys", () =&gt; { SearchKeys.Init(Config); harmony.PatchAll(typeof(SearchKeys.Hooks));
///     guard.AddTick("SearchKeys", SearchKeys.Tick); });
/// </code>
/// Plugin Update: <c>guard.TickAll()</c>; scene ready (<see cref="SceneReadyDelay"/>): <c>guard.SceneReadyAll()</c>.
/// </example>
internal sealed class FeatureGuard
{
    private readonly ManualLogSource _log;

    private readonly HashSet<string> _off = new(StringComparer.Ordinal);
    private readonly List<Entry> _ticks = [];
    private readonly List<Entry> _sceneReady = [];

    /// <remarks>Key hints per feature, hidden when feature turns off; guarded by same lock as stopped features.</remarks>
    private readonly Dictionary<string, KeyHint[]> _hints = new(StringComparer.Ordinal);

    /// <remarks>Count of stopped features, so <see cref="IsOff"/> skips lock while none is stopped (normal case, called per frame).</remarks>
    private volatile int _offCount;

    /// <summary>Initializes guard that logs to plugin log.</summary>
    /// <param name="log">Plugin log for errors of stopped features.</param>
    public FeatureGuard(ManualLogSource log)
    {
        _log = log;
    }

    /// <summary>Determines whether exception turned feature off, at start or while running.</summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsOff(string feature)
    {
        if (_offCount == 0)
        {
            return false;
        }

        lock (_off)
        {
            return _off.Contains(feature);
        }
    }

    /// <summary>Runs start steps (Bind, patches, registration).</summary>
    /// <param name="feature">Feature name.</param>
    /// <param name="start">Start steps.</param>
    /// <param name="hints">Key hints of feature, or <see langword="null"/>.
    /// When feature turns off, guard sets <see cref="KeyHint.Active"/> to <see langword="false"/> and calls <see cref="KeyHint.Changed"/>.</param>
    /// <returns><see langword="true"/> if start steps ran without exception; otherwise, <see langword="false"/> (feature is then off).</returns>
    public bool Start(string feature, Action start, KeyHint[]? hints = null)
    {
        if (hints is { Length: > 0 })
        {
            lock (_off)
            {
                _hints[feature] = hints;
            }
        }

        try
        {
            start();
            return true;
        }
        catch (Exception e)
        {
            Disable(feature, "start", e);
            return false;
        }
    }

    /// <summary>Registers per-frame call. Main thread only.</summary>
    /// <remarks>Delegate made once; <see cref="TickAll"/> walks list by index, so no allocation per frame. Call added during tick runs in same pass.</remarks>
    public void AddTick(string feature, Action tick) => _ticks.Add(new Entry(feature, tick));

    /// <summary>Registers call that <see cref="SceneReadyAll"/> runs at every scene ready. Main thread only.</summary>
    public void AddSceneReady(string feature, Action ready) => _sceneReady.Add(new Entry(feature, ready));

    /// <summary>Runs per-frame calls of features not off, in registration order. Main thread only.</summary>
    public void TickAll() => RunAll(_ticks, "tick");

    /// <summary>Runs scene-ready calls of features not off, in registration order. Main thread only.</summary>
    public void SceneReadyAll() => RunAll(_sceneReady, "scene ready");

    /// <summary>Guards one call.</summary>
    /// <remarks>For work not registered, e.g. inside event.</remarks>
    public void Run(string feature, Action action)
    {
        if (IsOff(feature))
        {
            return;
        }

        try
        {
            action();
        }
        catch (Exception e)
        {
            Disable(feature, "run", e);
        }
    }

    /// <summary>Turns feature off and logs exception.</summary>
    /// <remarks>For caller's own catch, e.g. inside patch where no delegate is wanted.</remarks>
    public void Fail(string feature, Exception e) => Disable(feature, "run", e);

    private void RunAll(List<Entry> list, string phase)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Entry entry = list[i];
            if (IsOff(entry.Feature))
            {
                continue;
            }

            try
            {
                entry.Action();
            }
            catch (Exception e)
            {
                Disable(entry.Feature, phase, e);
            }
        }
    }

    private void Disable(string feature, string phase, Exception e)
    {
        KeyHint[]? hints;
        lock (_off)
        {
            if (!_off.Add(feature))
            {
                return;
            }

            _offCount = _off.Count;
            _hints.TryGetValue(feature, out hints);
        }

        _log.LogError($"{feature} off ({phase}): {e}");
        if (hints != null)
        {
            HideHints(feature, hints);
        }
    }

    /// <summary>Hides key hint rows of stopped feature.</summary>
    /// <remarks>Runs outside lock: <see cref="KeyHint.Changed"/> is code of key hints plugin.</remarks>
    private void HideHints(string feature, KeyHint[] hints)
    {
        foreach (KeyHint hint in hints)
        {
            if (hint == null)
            {
                continue;
            }

            hint.Active = false;
            try
            {
                hint.Changed?.Invoke();
            }
            catch (Exception e)
            {
                _log.LogWarning($"{feature} key hint not updated: {e.Message}");
            }
        }
    }

    private sealed class Entry
    {
        public readonly string Feature;
        public readonly Action Action;

        public Entry(string feature, Action action)
        {
            Feature = feature;
            Action = action;
        }
    }
}
