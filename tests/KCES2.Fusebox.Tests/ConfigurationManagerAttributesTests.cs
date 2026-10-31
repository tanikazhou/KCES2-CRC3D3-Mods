using System.Reflection;
using AwesomeAssertions;
using BepInEx.Configuration;
using Xunit;

namespace KCES2.Fusebox.Tests;

/// <summary>Tests of <see cref="ConfigurationManagerAttributes"/> name contract with ConfigurationManager.</summary>
/// <remarks>ConfigurationManager v19 finds tag class by name and copies public instance fields by name; rename silently breaks every tag.</remarks>
public class ConfigurationManagerAttributesTests
{
    /// <summary>Field names and types of upstream tag class that ConfigurationManager v19 reads.</summary>
    private static readonly Dictionary<string, Type> _upstream = new()
    {
        ["DispName"] = typeof(string),
        ["Category"] = typeof(string),
        ["Description"] = typeof(string),
        ["Order"] = typeof(int?),
        ["IsAdvanced"] = typeof(bool?),
        ["Browsable"] = typeof(bool?),
        ["ReadOnly"] = typeof(bool?),
        ["DefaultValue"] = typeof(object),
        ["HideDefaultButton"] = typeof(bool?),
        ["HideSettingName"] = typeof(bool?),
        ["ShowRangeAsPercent"] = typeof(bool?),
        ["CustomDrawer"] = typeof(Action<ConfigEntryBase>),
        ["CustomHotkeyDrawer"] = typeof(ConfigurationManagerAttributes.CustomHotkeyDrawerFunc),
        ["ObjToStr"] = typeof(Func<object, string>),
        ["StrToObj"] = typeof(Func<string, object>),
    };

    [Fact]
    public void ClassName_IsWhatConfigurationManagerLooksFor() =>
        nameof(ConfigurationManagerAttributes).Should().Be("ConfigurationManagerAttributes");

    [Fact]
    public void PublicFields_MatchUpstreamNamesAndTypes()
    {
        Dictionary<string, Type> fields = typeof(ConfigurationManagerAttributes)
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(static f => f.Name, static f => f.FieldType);

        fields.Should().Equal(_upstream);
    }

    [Fact]
    public void HotkeyDrawer_HasUpstreamSignature()
    {
        MethodInfo invoke = typeof(ConfigurationManagerAttributes.CustomHotkeyDrawerFunc).GetMethod("Invoke");
        Assert.NotNull(invoke);
        ParameterInfo[] parameters = invoke.GetParameters();

        invoke.ReturnType.Should().Be(typeof(void));
        parameters[0].ParameterType.Should().Be(typeof(ConfigEntryBase));
        parameters[1].ParameterType.Should().Be(typeof(bool).MakeByRefType());
    }
}
