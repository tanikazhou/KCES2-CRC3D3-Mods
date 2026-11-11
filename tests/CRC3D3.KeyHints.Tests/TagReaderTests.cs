using AwesomeAssertions;
using BepInEx.Configuration;
using BepInEx.Logging;
using CRC3D3.KeyHints.Discovery;
using KCES2.Fusebox;
using KCES2.Fusebox.Tests.Support;
using UnityEngine;
using Xunit;
using BadHint = CRC3D3.KeyHints.Tests.Tags.Bad.KeyHint;
using OldHint = CRC3D3.KeyHints.Tests.Tags.Old.KeyHint;
using OtherHint = CRC3D3.KeyHints.Tests.Tags.Other.KeyHint;

namespace CRC3D3.KeyHints.Tests;

/// <summary>Tests of <see cref="TagReader"/>: tags found by class and field name, key and label rules.</summary>
public class TagReaderTests : IDisposable
{
    private readonly TempDir _dir = new();
    private readonly LogCapture _log = new();
    private readonly ConfigFile _config;
    private readonly List<FoundHint> _found = [];
    private int _changes;

    public TagReaderTests()
    {
        _config = new ConfigFile(_dir.File("test.cfg"), false);
    }

    public void Dispose()
    {
        _log.Dispose();
        _dir.Dispose();
    }

    [Fact]
    public void Read_ForeignTagClass_Found()
    {
        Bind("Noclip", "UpKey", KeyCode.Space, new OtherHint { Label = "Fly up", Order = 101 });

        Read();

        FoundHint hint = _found.Should().ContainSingle().Subject;
        hint.PluginGuid.Should().Be("test.plugin");
        HintState state = hint.Read();
        state.Label.Should().Be("Fly up");
        state.Order.Should().Be(101);
        state.Active.Should().BeTrue();
        state.Key.MainKey.Should().Be(KeyCode.Space);
    }

    [Fact]
    public void Read_KeyboardShortcutSetting_KeyFromValue()
    {
        ConfigEntry<KeyboardShortcut> e = Bind("Noclip", "ToggleKey", new KeyboardShortcut(KeyCode.N), new KeyHint());

        Read();
        e.Value = new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl);

        HintState state = _found.Should().ContainSingle().Subject.Read();
        state.Key.MainKey.Should().Be(KeyCode.F8, "key is read from setting at each read, so rebinds show");
        state.Key.Modifiers.Should().Equal(KeyCode.LeftControl);
    }

    [Fact]
    public void Read_FewerFields_UsesDefaults()
    {
        Bind("A", "Key", KeyCode.A, new OldHint { Label = "Old" });

        Read();

        FoundHint hint = _found.Should().ContainSingle().Subject;
        hint.Contexts.Should().BeEmpty();
        HintState state = hint.Read();
        state.Label.Should().Be("Old");
        state.Order.Should().Be(0);
        state.Active.Should().BeTrue();
    }

    [Fact]
    public void Read_WrongFieldType_SkipsTagWarnsOnce()
    {
        Bind("A", "Bad", KeyCode.A, new BadHint { Order = "1" });
        Bind("A", "Good", KeyCode.B, new KeyHint { Label = "Good" });

        Read();

        _found.Should().ContainSingle().Which.Read().Label.Should().Be("Good");
        _log.Of(LogLevel.Warning).Should().ContainSingle().Which.Should().Contain("Order");
    }

    [Fact]
    public void Read_SetsChanged()
    {
        var hint = new KeyHint();
        Bind("A", "Key", KeyCode.A, hint);

        Read();
        hint.Changed.Should().NotBeNull();
        hint.Changed!.Invoke();

        _changes.Should().Be(1);
    }

    [Fact]
    public void Read_FixedKeyOnBoolSetting()
    {
        Bind("Hints", "DoorHints", true, new KeyHint { Key = new KeyboardShortcut(KeyCode.E), Label = "Open door" });

        Read();

        _found.Should().ContainSingle().Which.Read().Key.MainKey.Should().Be(KeyCode.E);
    }

    [Fact]
    public void Read_BoolSettingWithoutKey_SkippedWithWarning()
    {
        Bind("Hints", "DoorHints", true, new KeyHint());

        Read();

        _found.Should().BeEmpty();
        _log.Of(LogLevel.Warning).Should().ContainSingle().Which.Should().Contain("has no key");
    }

    [Fact]
    public void Read_LabelFallsBackToDispName()
    {
        new SettingList(_config).Bind("Overlay", "ToggleKey", new KeyboardShortcut(KeyCode.F3), "Key.", hint: new KeyHint());

        Read();

        _found.Should().ContainSingle().Which.Read().Label.Should().Be("Toggle key");
    }

    [Fact]
    public void Read_LabelFallsBackToKeyWithoutAttributes()
    {
        Bind("Overlay", "ToggleKey", KeyCode.F3, new KeyHint());

        Read();

        _found.Should().ContainSingle().Which.Read().Label.Should().Be("ToggleKey");
    }

    [Fact]
    public void Read_ContextList_Parsed()
    {
        Bind("A", "Key", KeyCode.A, new KeyHint { Context = " Field, Dance ,," });

        Read();

        _found.Should().ContainSingle().Which.Contexts.Should().Equal("Field", "Dance");
    }

    [Fact]
    public void Read_IndexKeepsBindOrderAcrossPlugins()
    {
        Bind("B", "First", KeyCode.B, new KeyHint());
        Bind("A", "Second", KeyCode.C, new KeyHint());
        Read();
        var other = new ConfigFile(_dir.File("other.cfg"), false);
        other.Bind("A", "Third", KeyCode.D, new ConfigDescription("Test.", null, new KeyHint()));

        TagReader.Read("other.plugin", other, () => _changes++, _log.Source, _found);

        _found.Select(static f => f.Index).Should().Equal(0, 1, 2);
        _found.Select(static f => f.Entry.Definition.Key).Should().Equal("First", "Second", "Third");
    }

    [Fact]
    public void Read_ActiveChangeVisibleAtNextRead()
    {
        var hint = new KeyHint();
        Bind("A", "Key", KeyCode.A, hint);
        Read();

        hint.Active = false;
        hint.Label = "Noclip: on";

        HintState state = _found.Single().Read();
        state.Active.Should().BeFalse();
        state.Label.Should().Be("Noclip: on");
    }

    [Fact]
    public void Read_EntryWithoutTags_Ignored()
    {
        _config.Bind("A", "Key", KeyCode.A, "No tags.");

        Read();

        _found.Should().BeEmpty();
        _log.Lines.Should().BeEmpty();
    }

    private ConfigEntry<T> Bind<T>(string section, string key, T value, object tag) =>
        _config.Bind(section, key, value, new ConfigDescription("Test.", null, tag));

    private void Read() => TagReader.Read("test.plugin", _config, () => _changes++, _log.Source, _found);
}
