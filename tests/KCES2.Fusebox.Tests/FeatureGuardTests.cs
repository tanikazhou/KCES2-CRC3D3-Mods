using System.Reflection;
using AwesomeAssertions;
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
        _guard.Start("A", () => ran = true).Should().BeTrue();
        ran.Should().BeTrue();
        _guard.IsOff("A").Should().BeFalse();
        _log.Lines.Should().BeEmpty();
    }

    [Fact]
    public void Start_Throws_ReturnsFalseTurnsOffAndLogsOnce()
    {
        _guard.Start("A", static () => throw new InvalidOperationException("boom")).Should().BeFalse();
        _guard.IsOff("A").Should().BeTrue();
        _log.Of(LogLevel.Error).Should().ContainSingle().Which.Should()
            .StartWith("A off (start): System.InvalidOperationException: boom");
    }

    [Fact]
    public void UnknownFeature_IsNotOff() => _guard.IsOff("never seen").Should().BeFalse();

    [Fact]
    public void FeatureNames_AreCaseSensitive()
    {
        _guard.Fail("Search", new Exception());
        _guard.IsOff("Search").Should().BeTrue();
        _guard.IsOff("search").Should().BeFalse();
    }

    [Fact]
    public void TickAll_RunsInRegistrationOrder()
    {
        var order = new List<string>();
        _guard.AddTick("A", () => order.Add("A"));
        _guard.AddTick("B", () => order.Add("B"));
        _guard.AddTick("A", () => order.Add("A2"));
        _guard.TickAll();
        order.Should().Equal("A", "B", "A2");
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
        a.Should().Be(1);
        a2.Should().Be(0);
        b.Should().Be(3);
        _guard.IsOff("A").Should().BeTrue();
        _guard.IsOff("B").Should().BeFalse();
        _log.Of(LogLevel.Error).Should().ContainSingle().Which.Should().StartWith("A off (tick): ");
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

        ticks.Should().Be(0);
        other.Should().Be(1);
        _log.Of(LogLevel.Error).Should().ContainSingle().Which.Should().StartWith("A off (scene ready): ");
    }

    [Fact]
    public void StartFailure_StopsLaterTicksAndRuns()
    {
        int ticks = 0, runs = 0;
        _guard.AddTick("A", () => ticks++);
        _guard.Start("A", () => throw new Exception());

        _guard.TickAll();
        _guard.Run("A", () => runs++);

        ticks.Should().Be(0);
        runs.Should().Be(0);
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

        n.Should().Be(1);
        _guard.IsOff("A").Should().BeTrue();
        _log.Of(LogLevel.Error).Should().ContainSingle().Which.Should().StartWith("A off (run): ");
    }

    [Fact]
    public void Fail_LogsOnlyTheFirstError()
    {
        _guard.Fail("A", new Exception("first"));
        _guard.Fail("A", new Exception("second"));
        _guard.Run("A", static () => throw new Exception("third"));
        _log.Of(LogLevel.Error).Should().ContainSingle().Which.Should().Contain("first");
    }

    [Fact]
    public void Fail_FromManyThreads_LogsOnce()
    {
        var lines = new System.Collections.Concurrent.ConcurrentBag<string>();
        using var src = new ManualLogSource("threads");
        src.LogEvent += (_, e) => lines.Add(e.Data.ToString());
        var g = new FeatureGuard(src);

        Parallel.For(0, 200, i => g.Fail("A", new Exception(i.ToString())));

        lines.Should().ContainSingle();
        g.IsOff("A").Should().BeTrue();
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
        _guard.IsOff("A").Should().BeTrue();
        _guard.IsOff("B").Should().BeTrue();
        _log.Of(LogLevel.Error).Should().HaveCount(2);
    }

    [Fact]
    public void TickAll_NoEntries_DoesNothing()
    {
        _guard.TickAll();
        _guard.SceneReadyAll();
        _log.Lines.Should().BeEmpty();
    }

    /// <summary>Checks that per-frame path allocates nothing, with no feature off (lock skipped) and with one off (lock taken).</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TickAll_AllocatesNothing(bool oneFeatureOff)
    {
        int runs = 0;
        _guard.AddTick("A", () => runs++);
        _guard.AddTick("B", static () => { });
        if (oneFeatureOff) _guard.Fail("B", new Exception());

        _guard.TickAll();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) _guard.TickAll();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().Be(0);
        runs.Should().Be(1001);
    }

    [Fact]
    public void AddTick_DuringTick_RunsInSamePass()
    {
        var order = new List<string>();
        _guard.AddTick("A", () =>
        {
            order.Add("A");
            if (order.Count == 1) _guard.AddTick("B", () => order.Add("B"));
        });

        _guard.TickAll();

        order.Should().Equal("A", "B");
        _log.Lines.Should().BeEmpty();
    }

    [Fact]
    public void Start_Throws_HintsInactiveAndChangedOnce()
    {
        int calls = 0;
        var hint = new KeyHint { Changed = () => calls++ };

        _guard.Start("A", static () => throw new InvalidOperationException("boom"), [hint]);

        hint.Active.Should().BeFalse();
        calls.Should().Be(1);
    }

    [Fact]
    public void Fail_AfterStart_HintsInactiveAndChangedOnce()
    {
        int calls = 0;
        var hint = new KeyHint { Changed = () => calls++ };
        _guard.Start("A", static () => { }, [hint]).Should().BeTrue();
        hint.Active.Should().BeTrue();

        _guard.Fail("A", new Exception());

        hint.Active.Should().BeFalse();
        calls.Should().Be(1);
    }

    [Fact]
    public void Disable_Twice_ChangedOnce()
    {
        int calls = 0;
        var hint = new KeyHint { Changed = () => calls++ };
        _guard.Start("A", static () => { }, [hint]);

        _guard.Fail("A", new Exception());
        _guard.Fail("A", new Exception());

        calls.Should().Be(1);
    }

    [Fact]
    public void Disable_OtherFeature_LeavesHintActive()
    {
        int calls = 0;
        var hint = new KeyHint { Changed = () => calls++ };
        _guard.Start("A", static () => { }, [hint]);

        _guard.Fail("B", new Exception());

        hint.Active.Should().BeTrue();
        calls.Should().Be(0);
    }

    [Fact]
    public void Disable_ChangedThrows_OtherHintsStillSetAndWarnsOnce()
    {
        int calls = 0;
        var bad = new KeyHint { Changed = static () => throw new InvalidOperationException("handler") };
        var good = new KeyHint { Changed = () => calls++ };
        _guard.Start("A", static () => { }, [bad, good]);

        _guard.Fail("A", new Exception());

        bad.Active.Should().BeFalse();
        good.Active.Should().BeFalse();
        calls.Should().Be(1);
        _log.Of(LogLevel.Warning).Should().ContainSingle().Which.Should().Contain("handler");
    }

    [Fact]
    public void Disable_HintWithoutHandler_OnlyInactive()
    {
        var hint = new KeyHint();
        _guard.Start("A", static () => { }, [hint]);

        FluentActions.Invoking(() => _guard.Fail("A", new Exception())).Should().NotThrow();

        hint.Active.Should().BeFalse();
    }

    [Fact]
    public void Disable_ChangedRunsOutsideLock()
    {
        var lockFree = new List<bool>();
        var hint = new KeyHint { Changed = () => lockFree.Add(Task.Run(() => _guard.IsOff("A")).Wait(TimeSpan.FromSeconds(2))) };
        _guard.Start("A", static () => { }, [hint]);

        _guard.Fail("A", new Exception());

        lockFree.Should().Equal([true], "IsOff on another thread must not wait for lock held during Changed");
    }

    /// <summary>Checks that guard never touches settings.</summary>
    /// <remarks>Guard holds no <see cref="ConfigFile"/> or <see cref="ConfigEntryBase"/> field, and config file is unchanged after failure.</remarks>
    [Fact]
    public void Failure_NeverWritesTheConfigFile()
    {
        foreach (FieldInfo f in typeof(FeatureGuard).GetFields(BindingFlags.Instance | BindingFlags.Static |
                                                               BindingFlags.Public | BindingFlags.NonPublic))
        {
            typeof(ConfigFile).IsAssignableFrom(f.FieldType).Should().BeFalse(f.Name);
            typeof(ConfigEntryBase).IsAssignableFrom(f.FieldType).Should().BeFalse(f.Name);
        }

        using var dir = new TempDir();
        string path = dir.File("test.cfg");
        var config = new ConfigFile(path, true);
        ConfigEntry<bool> on = config.Bind("Feature", "Enabled", true, "The feature");
        string before = File.ReadAllText(path);

        _guard.Start("Feature", static () => throw new Exception());
        _guard.Fail("Feature", new Exception());

        on.Value.Should().BeTrue();
        File.ReadAllText(path).Should().Be(before);
    }
}
