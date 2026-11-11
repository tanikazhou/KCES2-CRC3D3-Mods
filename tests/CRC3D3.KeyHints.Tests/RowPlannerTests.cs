using AwesomeAssertions;
using BepInEx.Configuration;
using CRC3D3.KeyHints.Discovery;
using CRC3D3.KeyHints.Rows;
using KCES2.Fusebox;
using KCES2.Fusebox.Tests.Support;
using UnityEngine;
using Xunit;

namespace CRC3D3.KeyHints.Tests;

/// <summary>Tests of <see cref="RowPlanner"/>: which rows show, in which order.</summary>
public class RowPlannerTests : IDisposable
{
    private static readonly KeyboardShortcut _f11 = new(KeyCode.F11);

    private readonly TempDir _dir = new();
    private readonly ConfigFile _config;
    private readonly List<FoundHint> _hints = [];
    private readonly List<HintState> _states = [];
    private readonly ContextSet _contexts = new();
    private readonly List<RowText> _rows = [];

    public RowPlannerTests()
    {
        _config = new ConfigFile(_dir.File("test.cfg"), false);
    }

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Plan_SortsByOrderThenIndex()
    {
        Add("C", 20, KeyCode.C);
        Add("A", 10, KeyCode.A);
        Add("B", 20, KeyCode.B);

        Plan();

        Labels().Should().Equal("A", "C", "B", "Hide hints");
    }

    [Fact]
    public void Plan_InactiveHidden()
    {
        Add("On", 1, KeyCode.A);
        Add("Off", 2, KeyCode.B, active: false);

        Plan();

        Labels().Should().Equal("On", "Hide hints");
    }

    [Fact]
    public void Plan_ContextOffHidden()
    {
        Add("Field", 1, KeyCode.A, "Field");
        Add("Always", 2, KeyCode.B);

        Plan();
        Labels().Should().Equal("Always", "Hide hints");

        _contexts.Set("Field", true);
        Plan();
        Labels().Should().Equal("Field", "Always", "Hide hints");
    }

    [Fact]
    public void Plan_KeyNone_HidesRow()
    {
        Add("Unset", 1, KeyCode.None);

        Plan();

        _rows.Should().BeEmpty();
    }

    [Fact]
    public void Plan_SameKeyTwice_ShowsBoth()
    {
        Add("First", 1, KeyCode.N);
        Add("Second", 2, KeyCode.N);

        Plan();

        _rows.Take(2).Select(static r => r.Key).Should().Equal("N", "N");
        Labels().Should().Equal("First", "Second", "Hide hints");
    }

    [Fact]
    public void Plan_ToggleRowLastWhenAnyRow()
    {
        Add("Noclip", 100, KeyCode.N);

        Plan();

        _rows.Last().Key.Should().Be("F11");
        _rows.Last().Label.Should().Be("Hide hints");
    }

    [Fact]
    public void Plan_NoRows_NoToggleRow()
    {
        Add("Field", 1, KeyCode.A, "Field");

        Plan();

        _rows.Should().BeEmpty();
    }

    [Fact]
    public void Plan_ToggleKeyNone_NoToggleRow()
    {
        Add("Noclip", 1, KeyCode.N);

        RowPlanner.Plan(_hints, _states, _contexts, KeyboardShortcut.Empty, _rows);

        Labels().Should().Equal("Noclip");
    }

    [Fact]
    public void Plan_ClearsRowsFirst()
    {
        _rows.Add(new RowText("X", "Old"));
        Add("New", 1, KeyCode.A);

        Plan();

        Labels().Should().Equal("New", "Hide hints");
    }

    private void Add(string label, int order, KeyCode key, string context = null, bool active = true)
    {
        var tag = new KeyHint { Label = label, Order = order, Context = context };
        ConfigEntry<KeyCode> entry = _config.Bind("S", label + _hints.Count, key, new ConfigDescription("Test.", null, tag));
        var fields = TagFields.For(typeof(KeyHint), out _);
        _hints.Add(new FoundHint("p", entry, tag, fields, TagReader.ParseContexts(context), label, _hints.Count));
        _states.Add(new HintState(label, order, active, new KeyboardShortcut(key)));
    }

    private void Plan() => RowPlanner.Plan(_hints, _states, _contexts, _f11, _rows);

    private IEnumerable<string> Labels() => _rows.Select(static r => r.Label);
}
