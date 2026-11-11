#nullable enable
using BepInEx.Configuration;

// ReSharper disable once CheckNamespace
namespace KCES2.Fusebox;

// Plugin sets only fields it needs; key hints plugin reads all by reflection.
#pragma warning disable CS0649

/// <summary>Setting tag that declares key hint row for key setting.</summary>
/// <remarks>
/// <para>Key hints plugin finds class by name and reads public instance fields by name, as ConfigurationManager does with
/// <see cref="ConfigurationManagerAttributes"/>. Class name, field names and field types must stay. New fields can come later:
/// reader treats missing field as default.</para>
/// <para>No reference to key hints plugin: without it installed, tag has no effect and plugin runs normally.
/// Class stays internal, one copy per plugin, so plugins do not conflict.</para>
/// <para>Plugin writes <see cref="Label"/> and <see cref="Active"/> on main thread, then calls <see cref="Changed"/>.
/// Keep tag object in field to change it later. Only <see cref="FeatureGuard"/> writes <see cref="Failed"/>.</para>
/// <para>On <see langword="bool"/> setting with fixed <see cref="Key"/>, setting value is feature switch: <see langword="false"/> hides row.</para>
/// <para>Keep tag field out of class with static fields of game types: <see cref="FeatureGuard"/> rule for Awake bodies applies to that class.</para>
/// </remarks>
/// <example>
/// <code>
/// _hint = new KeyHint { Context = "Field", Order = 100, Label = "Noclip" };
/// _key = settings.Bind("Noclip", "ToggleKey", new KeyboardShortcut(KeyCode.N), "Key.", hint: _hint);
/// _hint.Label = on ? "Noclip: on" : "Noclip";
/// _hint.Changed?.Invoke();
/// </code>
/// </example>
internal sealed class KeyHint
{
    /// <summary>Row text, English; <see langword="null"/> = shown name of setting.</summary>
    public string? Label;

    /// <summary>Sort key of row, lowest first.</summary>
    public int Order;

    /// <summary>Comma-separated contexts in which row shows, e.g. "Field" or "Field,Dance".</summary>
    /// <remarks><see langword="null"/> or empty = row shows whenever hints show.</remarks>
    public string? Context;

    /// <summary>Indicates whether plugin state allows row now.</summary>
    /// <remarks>Plugin writes it; <see cref="Failed"/> hides row whatever this holds.</remarks>
    public bool Active = true;

    /// <summary>Indicates whether feature of row failed and is off for session.</summary>
    /// <remarks>Written only by <see cref="FeatureGuard"/>, possibly on loader thread.</remarks>
    public bool Failed;

    /// <summary>Fixed key for setting that is no key, e.g. game key E on feature switch.</summary>
    /// <remarks><see langword="null"/> = key from setting value. On <see langword="bool"/> setting, value <see langword="false"/> hides row.</remarks>
    public KeyboardShortcut? Key;

    /// <summary>Change signal, filled by key hints plugin; <see langword="null"/> while none runs.</summary>
    /// <remarks>Safe to call from any thread: handler only marks rows for rebuild.</remarks>
    public Action? Changed;
}
