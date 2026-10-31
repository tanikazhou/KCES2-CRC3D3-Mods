using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using KCES2.Fusebox.Tests.Support;
using Xunit;

namespace KCES2.Fusebox.Tests;

/// <summary>Tests of <see cref="FeatureGuard"/> feature isolation.</summary>
/// <remarks>
/// <para>Exception to one feature must turn off only that feature for this session, log once, and leave other features running.</para>
/// <para>Guard must never write config file: that would keep feature off at next start.</para>
/// </remarks>
public class FeatureGuardTests : IDisposable
{
    private readonly LogCapture _log = new();
    private readonly FeatureGuard _guard;

    public FeatureGuardTests()
    {
        _guard = new FeatureGuard(_log.Source);
    }

    public void Dispose() => _log.Dispose();

    [Fact]
    public void Start_Success_ReturnsTrueAndStaysOn()
    {
        bool ran = false;
        Assert.True(_guard.Start("A", () => ran = true));
        Assert.True(ran);
        Assert.False(_guard.IsOff("A"));
        Assert.Empty(_log.Lines);
    }

    [Fact]
    public void Start_Throws_ReturnsFalseTurnsOffAndLogsOnce()
    {
        Assert.False(_guard.Start("A", static () => throw new InvalidOperationException("boom")));
        Assert.True(_guard.IsOff("A"));
        string e = Assert.Single(_log.Of(LogLevel.Error));
        Assert.StartsWith("A off (start): System.InvalidOperationException: boom", e);
    }

    [Fact]
    public void UnknownFeature_IsNotOff() => Assert.False(_guard.IsOff("never seen"));

    [Fact]
    public void FeatureNames_AreCaseSensitive()
    {
        _guard.Fail("Search", new Exception());
        Assert.True(_guard.IsOff("Search"));
        Assert.False(_guard.IsOff("search"));
    }

    [Fact]
    public void TickAll_RunsInRegistrationOrder()
    {
        var order = new List<string>();
        _guard.AddTick("A", () => order.Add("A"));
        _guard.AddTick("B", () => order.Add("B"));
        _guard.AddTick("A", () => order.Add("A2"));
        _guard.TickAll();
        Assert.Equal(new[] { "A", "B", "A2" }, order);
    }

    [Fact]
    public void TickAll_ErrorTurnsOffOnlyItsFeature()
    {
        int a = 0, b = 0, a2 = 0;
        _guard.AddTick("A", () =>
        {
            a++;
            throw new Exception("tick");
        });
        _guard.AddTick("B", () => b++);
        _guard.AddTick("A", () => a2++);

        _guard.TickAll();
        _guard.TickAll();
        _guard.TickAll();

        // Once first A call fails, second A call is skipped from same pass on.
        Assert.Equal(1, a);
        Assert.Equal(0, a2);
        Assert.Equal(3, b);
        Assert.True(_guard.IsOff("A"));
        Assert.False(_guard.IsOff("B"));
        Assert.StartsWith("A off (tick): ", Assert.Single(_log.Of(LogLevel.Error)));
    }

    [Fact]
    public void SceneReadyAll_ErrorTurnsOffOnlyItsFeatureAndStopsItsTicks()
    {
        int ticks = 0, other = 0;
        _guard.AddSceneReady("A", () => throw new Exception("ready"));
        _guard.AddSceneReady("B", () => other++);
        _guard.AddTick("A", () => ticks++);

        _guard.SceneReadyAll();
        _guard.TickAll();

        Assert.Equal(0, ticks);
        Assert.Equal(1, other);
        Assert.StartsWith("A off (scene ready): ", Assert.Single(_log.Of(LogLevel.Error)));
    }

    [Fact]
    public void StartFailure_StopsLaterTicksAndRuns()
    {
        int ticks = 0, runs = 0;
        _guard.AddTick("A", () => ticks++);
        _guard.Start("A", () => throw new Exception());

        _guard.TickAll();
        _guard.Run("A", () => runs++);

        Assert.Equal(0, ticks);
        Assert.Equal(0, runs);
    }

    [Fact]
    public void Run_Throws_TurnsOffAndLaterRunsDoNothing()
    {
        int n = 0;
        _guard.Run("A", () =>
        {
            n++;
            throw new Exception("x");
        });
        _guard.Run("A", () => n++);

        Assert.Equal(1, n);
        Assert.True(_guard.IsOff("A"));
        Assert.StartsWith("A off (run): ", Assert.Single(_log.Of(LogLevel.Error)));
    }

    [Fact]
    public void Fail_LogsOnlyTheFirstError()
    {
        _guard.Fail("A", new Exception("first"));
        _guard.Fail("A", new Exception("second"));
        _guard.Run("A", static () => throw new Exception("third"));
        string e = Assert.Single(_log.Of(LogLevel.Error));
        Assert.Contains("first", e);
    }

    [Fact]
    public void Fail_FromManyThreads_LogsOnce()
    {
        var lines = new System.Collections.Concurrent.ConcurrentBag<string>();
        using var src = new ManualLogSource("threads");
        src.LogEvent += (_, e) => lines.Add(e.Data.ToString());
        var g = new FeatureGuard(src);

        Parallel.For(0, 200, i => g.Fail("A", new Exception(i.ToString())));

        Assert.Single(lines);
        Assert.True(g.IsOff("A"));
    }

    /// <summary>Checks that turning off other feature inside failing call logs each one once.</summary>
    [Fact]
    public void FailInsideTick_ForOtherFeature_LogsBoth()
    {
        _guard.AddTick("A", () =>
        {
            _guard.Fail("B", new Exception("b"));
            throw new Exception("a");
        });
        _guard.TickAll();
        Assert.True(_guard.IsOff("A"));
        Assert.True(_guard.IsOff("B"));
        Assert.Equal(2, _log.Of(LogLevel.Error).Count());
    }

    [Fact]
    public void TickAll_NoEntries_DoesNothing()
    {
        _guard.TickAll();
        _guard.SceneReadyAll();
        Assert.Empty(_log.Lines);
    }

    /// <summary>Checks that guard never touches settings.</summary>
    /// <remarks>Guard holds no <see cref="ConfigFile"/> or <see cref="ConfigEntryBase"/> field, and config file is unchanged after failure.</remarks>
    [Fact]
    public void Failure_NeverWritesTheConfigFile()
    {
        foreach (FieldInfo f in typeof(FeatureGuard).GetFields(BindingFlags.Instance | BindingFlags.Static |
                                                               BindingFlags.Public | BindingFlags.NonPublic))
        {
            Assert.False(typeof(ConfigFile).IsAssignableFrom(f.FieldType), f.Name);
            Assert.False(typeof(ConfigEntryBase).IsAssignableFrom(f.FieldType), f.Name);
        }

        using var dir = new TempDir();
        string path = dir.File("test.cfg");
        var config = new ConfigFile(path, true);
        ConfigEntry<bool> on = config.Bind("Feature", "Enabled", true, "The feature");
        string before = File.ReadAllText(path);

        _guard.Start("Feature", static () => throw new Exception());
        _guard.Fail("Feature", new Exception());

        Assert.True(on.Value);
        Assert.Equal(before, File.ReadAllText(path));
    }
}
