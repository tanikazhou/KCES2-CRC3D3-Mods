#nullable enable
using BepInEx.Configuration;

// ReSharper disable once CheckNamespace
namespace KCES2.Fusebox;

// Plugin sets only fields it needs; ConfigurationManager reads all by reflection.
#pragma warning disable CS0649

/// <summary>Setting tag that changes how BepInEx.ConfigurationManager shows setting.</summary>
/// <remarks>
/// <para>ConfigurationManager finds class by name and copies public instance fields whose names match its own setting properties.
/// Class name, field names and field types must stay. Fields with other names are ignored.</para>
/// <para>No reference to ConfigurationManager: without it installed, tag has no effect and plugin runs normally.
/// Class stays internal, one copy per plugin, so plugins do not conflict.</para>
/// <para>Null field = ConfigurationManager default.</para>
/// </remarks>
internal sealed class ConfigurationManagerAttributes
{
    /// <summary>Shown name; default is key.</summary>
    public string? DispName;

    /// <summary>Shown section title; default is section name.</summary>
    /// <remarks>ConfigurationManager groups settings by this title.</remarks>
    public string? Category;

    /// <summary>Tooltip text; default is config file description.</summary>
    public string? Description;

    /// <summary>Sort key inside section, highest first; ties sort by shown name.</summary>
    public int? Order;

    /// <summary>Indicates whether setting shows only with "Advanced settings" on, in yellow.</summary>
    public bool? IsAdvanced;

    /// <summary>Indicates whether setting shows in window at all.</summary>
    /// <remarks><see langword="false"/> hides setting from window; config file keeps it.</remarks>
    public bool? Browsable;

    /// <summary>Indicates whether window shows value without editor.</summary>
    public bool? ReadOnly;

    /// <summary>Value for reset button; default is Bind default value.</summary>
    public object? DefaultValue;

    /// <summary>Indicates whether reset button is hidden.</summary>
    public bool? HideDefaultButton;

    /// <summary>Indicates whether shown name is hidden, to give custom drawer full width.</summary>
    public bool? HideSettingName;

    /// <summary>Indicates whether range slider shows value as percent.</summary>
    public bool? ShowRangeAsPercent;

    /// <summary>Own IMGUI drawing of value editor.</summary>
    /// <remarks>Use this or <see cref="CustomHotkeyDrawer"/>, not both.</remarks>
    public Action<ConfigEntryBase>? CustomDrawer;

    /// <summary>Own IMGUI drawing of key editor that can read keyboard input.</summary>
    /// <remarks>Use this or <see cref="CustomDrawer"/>, not both.</remarks>
    public CustomHotkeyDrawerFunc? CustomHotkeyDrawer;

    /// <summary>Conversion of value to text box text.</summary>
    /// <remarks>Used only together with <see cref="StrToObj"/>.</remarks>
    public Func<object, string>? ObjToStr;

    /// <summary>Conversion of text box text to value.</summary>
    /// <remarks>Used only together with <see cref="ObjToStr"/>.</remarks>
    public Func<string, object>? StrToObj;

    /// <summary>Key editor drawing; ConfigurationManager converts it to its own delegate type of same signature.</summary>
    /// <param name="setting">Setting to draw.</param>
    /// <param name="isCurrentlyAcceptingInput">Set to <see langword="true"/> while editor waits for key.</param>
    public delegate void CustomHotkeyDrawerFunc(ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);
}
