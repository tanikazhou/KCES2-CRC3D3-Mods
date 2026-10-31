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
        lock (_off)
        {
            return _off.Contains(feature);
        }
    }

    /// <summary>Runs start steps (Bind, patches, registration).</summary>
    /// <returns><see langword="true"/> if start steps ran without exception; otherwise, <see langword="false"/> (feature is then off).</returns>
    public bool Start(string feature, Action start)
    {
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

    /// <summary>Registers per-frame call.</summary>
    /// <remarks>Delegate made once, so no allocation per frame.</remarks>
    public void AddTick(string feature, Action tick) => _ticks.Add(new Entry { Feature = feature, Action = tick });

    /// <summary>Registers call that <see cref="SceneReadyAll"/> runs at every scene ready.</summary>
    public void AddSceneReady(string feature, Action ready) => _sceneReady.Add(new Entry { Feature = feature, Action = ready });

    /// <summary>Runs per-frame calls of features not off, in registration order.</summary>
    public void TickAll() => RunAll(_ticks, "tick");

    /// <summary>Runs scene-ready calls of features not off, in registration order.</summary>
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
        foreach (Entry entry in list.Where(entry => !IsOff(entry.Feature)))
        {
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
        lock (_off)
        {
            if (!_off.Add(feature))
            {
                return;
            }
        }

        _log.LogError($"{feature} off ({phase}): {e}");
    }

    private sealed class Entry
    {
        public string Feature;
        public Action Action;
    }
}
