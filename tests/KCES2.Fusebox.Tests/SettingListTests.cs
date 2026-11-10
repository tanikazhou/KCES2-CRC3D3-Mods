using AwesomeAssertions;
using BepInEx.Configuration;
using KCES2.Fusebox.Tests.Support;
using UnityEngine;
using Xunit;

namespace KCES2.Fusebox.Tests;

/// <summary>Tests of <see cref="SettingList"/> labels, order and tags.</summary>
public class SettingListTests : IDisposable
{
    private readonly TempDir _dir = new();
    private readonly ConfigFile _config;
    private readonly SettingList _settings;

    public SettingListTests()
    {
        _config = new ConfigFile(_dir.File("test.cfg"), false);
        _settings = new SettingList(_config);
    }

    public void Dispose() => _dir.Dispose();

    [Theory]
    [InlineData("MouseBackButton", "Mouse back button")]
    [InlineData("Enabled", "Enabled")]
    [InlineData("LoadModsInKCES2", "Load mods in KCES2")]
    [InlineData("KCES2Camera", "KCES2 camera")]
    [InlineData("UIScale", "UI scale")]
    [InlineData("CRC3D3Floor", "CRC3D3 floor")]
    [InlineData("Slot1", "Slot 1")]
    [InlineData("NewGame2Button", "New game 2 button")]
    [InlineData("Top10Items", "Top 10 items")]
    [InlineData("OffsetX", "Offset X")]
    [InlineData("AButton", "A button")]
    [InlineData("openkupa", "Openkupa")]
    [InlineData("Show FPS counter", "Show FPS counter")]
    [InlineData("FreeExploration", "Free exploration")]
    public void ToLabel_PascalCase_GivesSentenceCase(string name, string expected) =>
        SettingList.ToLabel(name).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("--")]
    [InlineData(null)]
    public void ToLabel_NoWords_ReturnsInput(string name) => SettingList.ToLabel(name).Should().Be(name);

    [Fact]
    public void Constructor_NullConfig_Throws() => FluentActions.Invoking(static () => new SettingList(null!)).Should()
        .Throw<ArgumentNullException>();

    [Fact]
    public void Bind_SetsShownNameSectionTitleAndDeclarationOrder()
    {
        ConfigurationManagerAttributes first =
            Tag(_settings.Bind("FreeExploration", "MouseBackButton", true, "First."));
        ConfigurationManagerAttributes second =
            Tag(_settings.Bind("FreeExploration", "Alpha", 1, "Second."));

        first.DispName.Should().Be("Mouse back button");
        first.Category.Should().Be("Free exploration");
        Assert.NotNull(second.Order);
        first.Order.Should().BeGreaterThan(second.Order.Value, "first Bind must sort first (highest order)");
        first.IsAdvanced.Should().BeNull();
    }

    [Fact]
    public void BindAdvanced_SetsIsAdvanced()
    {
        ConfigurationManagerAttributes tag =
            Tag(_settings.BindAdvanced("Wheel", "Step", 1f, "Step.", new AcceptableValueRange<float>(0.1f, 10f)));

        tag.IsAdvanced.Should().BeTrue();
        tag.DispName.Should().Be("Step");
    }

    [Fact]
    public void Bind_Label_OverridesKeyLabel()
    {
        ConfigurationManagerAttributes tag =
            Tag(_settings.Bind("Camera", "Enabled", true, "Camera.", label: "KCES2 camera"));

        tag.DispName.Should().Be("KCES2 camera");
    }

    [Fact]
    public void Bind_CallerAttributes_KeepsCallerFieldsAndFillsOthers()
    {
        var own = new ConfigurationManagerAttributes { ShowRangeAsPercent = true, Order = 5, Category = "Own title" };

        ConfigurationManagerAttributes tag =
            Tag(_settings.BindAdvanced("Hints", "HintOpacity", 0.8f, "Opacity.", attributes: own));

        tag.Should().BeSameAs(own);
        tag.ShowRangeAsPercent.Should().BeTrue();
        tag.Order.Should().Be(5);
        tag.Category.Should().Be("Own title");
        tag.DispName.Should().Be("Hint opacity");
        tag.IsAdvanced.Should().BeTrue();
    }

    [Fact]
    public void Bind_ConfigFile_KeepsSectionKeyAndDescription()
    {
        _settings.Bind("FreeExploration", "MouseBackButton", true, "Mouse back button closes open panel.");
        _config.Save();

        string text = File.ReadAllText(_dir.File("test.cfg"));

        text.Should().Contain("[FreeExploration]")
            .And.Contain("## Mouse back button closes open panel.")
            .And.Contain("MouseBackButton = true");
    }

    [Fact]
    public void Bind_WithHint_PutsHintAfterConfigurationManagerAttributes()
    {
        var hint = new KeyHint { Label = "Noclip" };

        ConfigEntry<KeyboardShortcut> entry =
            _settings.Bind("Noclip", "ToggleKey", new KeyboardShortcut(KeyCode.N), "Key.", hint: hint);

        entry.Description.Tags.Should().HaveCount(2, "ConfigurationManager stops at first unknown tag, so hint goes last");
        entry.Description.Tags[0].Should().BeOfType<ConfigurationManagerAttributes>();
        entry.Description.Tags[1].Should().BeSameAs(hint);
    }

    [Fact]
    public void BindAdvanced_WithHint_PutsHintAfterConfigurationManagerAttributes()
    {
        var hint = new KeyHint();

        ConfigEntry<KeyCode> entry = _settings.BindAdvanced("Noclip", "UpKey", KeyCode.Space, "Key.", hint: hint);

        entry.Description.Tags.Should().HaveCount(2);
        entry.Description.Tags[0].Should().BeOfType<ConfigurationManagerAttributes>()
            .Which.IsAdvanced.Should().BeTrue();
        entry.Description.Tags[1].Should().BeSameAs(hint);
    }

    [Fact]
    public void Bind_WithoutHint_HasOnlyConfigurationManagerAttributes() =>
        Tag(_settings.Bind("A", "B", 1, "C.")).Should().NotBeNull();

    private static ConfigurationManagerAttributes Tag(ConfigEntryBase entry) =>
        entry.Description.Tags
            .Should().ContainSingle()
            .Which.Should().BeOfType<ConfigurationManagerAttributes>()
            .Subject;
}
