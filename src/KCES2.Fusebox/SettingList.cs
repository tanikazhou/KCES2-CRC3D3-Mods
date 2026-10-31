#nullable enable
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Configuration;

// ReSharper disable once CheckNamespace
namespace KCES2.Fusebox;

/// <summary>Binds plugin settings with shown name, section title and order for ConfigurationManager.</summary>
/// <remarks>
/// <para>Shown name comes from PascalCase key, section title from section name ("MouseBackButton" -> "Mouse back button").
/// Config file keeps section, key and description as given.</para>
/// <para>ConfigurationManager sorts settings of one section by <see cref="ConfigurationManagerAttributes.Order"/>, highest first.
/// Each Bind gets lower order than one before, so window keeps declaration order.</para>
/// <para>ConfigurationManager groups settings by shown section title.
/// Setting bound outside list keeps raw section name, so it shows in own group when section name has two or more words.</para>
/// </remarks>
/// <example>
/// <code>
/// var settings = new SettingList(Config);
/// Enabled = settings.Bind("Unlock", "Enabled", false, "Remove value limits of sliders.");
/// Step = settings.BindAdvanced("Wheel", "Step", 1f, "Value change per wheel notch.", new AcceptableValueRange&lt;float&gt;(0.1f, 10f));
/// Camera = settings.Bind("Camera", "Enabled", true, "Move camera to nails.", label: "KCES2 camera");
/// </code>
/// </example>
internal sealed class SettingList
{
    private const int FirstOrder = 10000;

    /// <summary>Words of PascalCase name.</summary>
    /// <remarks>Run of capitals and digits stays one word ("KCES2", "UI" in "UIScale"); lone number between words
    /// is its own word ("Slot1").</remarks>
    private static readonly Regex _words = new(@"[A-Z0-9]+(?=[A-Z][a-z])|[A-Z0-9]{2,}(?![a-z])|[A-Z]?[a-z]+|\d+|[A-Z]",
        RegexOptions.CultureInvariant);

    private readonly ConfigFile _config;
    private int _nextOrder = FirstOrder;

    /// <summary>Initializes list that binds into plugin config file.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
    public SettingList(ConfigFile config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>Makes sentence-case label from PascalCase name.</summary>
    /// <returns>Label, e.g. "Load mods in KCES2" for "LoadModsInKCES2"; <paramref name="name"/> when it has no letter or digit.</returns>
    /// <remarks>Words with lowercase letters become lowercase after first word; acronyms, numbers and single capitals stay.</remarks>
    public static string? ToLabel(string? name)
    {
        if (name == null) return null;

        MatchCollection words = _words.Matches(name);
        if (words.Count == 0) return name;

        var label = new StringBuilder(name.Length + words.Count);
        foreach (Match word in words)
        {
            string w = word.Value;
            if (label.Length == 0)
            {
                label.Append(char.ToUpperInvariant(w[0])).Append(w, 1, w.Length - 1);
            }
            else
            {
                label.Append(' ').Append(w.Any(char.IsLower) ? w.ToLowerInvariant() : w);
            }
        }

        return label.ToString();
    }

    /// <summary>Binds setting that ConfigurationManager always shows.</summary>
    /// <param name="section">Config file section.</param>
    /// <param name="key">Config file key, PascalCase.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="description">Config file comment and tooltip.</param>
    /// <param name="acceptableValues">Allowed values, or <see langword="null"/>.</param>
    /// <param name="label">Shown name; <see langword="null"/> = made from <paramref name="key"/>.</param>
    /// <param name="attributes">Tag with more fields, one object per setting; list adds its own fields where caller left them null.</param>
    public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description,
        AcceptableValueBase? acceptableValues = null,
        string? label = null, ConfigurationManagerAttributes? attributes = null) =>
        Add(section, key, defaultValue, description, acceptableValues, label, attributes, false);

    /// <summary>Binds setting that ConfigurationManager shows only with "Advanced settings" on.</summary>
    /// <param name="section">Config file section.</param>
    /// <param name="key">Config file key, PascalCase.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="description">Config file comment and tooltip.</param>
    /// <param name="acceptableValues">Allowed values, or <see langword="null"/>.</param>
    /// <param name="label">Shown name; <see langword="null"/> = made from <paramref name="key"/>.</param>
    /// <param name="attributes">Tag with more fields, one object per setting; list adds its own fields where caller left them null.</param>
    public ConfigEntry<T> BindAdvanced<T>(string section, string key, T defaultValue, string description,
        AcceptableValueBase? acceptableValues = null,
        string? label = null, ConfigurationManagerAttributes? attributes = null) =>
        Add(section, key, defaultValue, description, acceptableValues, label, attributes, true);

    private ConfigEntry<T> Add<T>(string section, string key, T defaultValue, string description,
        AcceptableValueBase? acceptableValues,
        string? label, ConfigurationManagerAttributes? attributes, bool advanced)
    {
        attributes ??= new ConfigurationManagerAttributes();
        attributes.DispName = label ?? attributes.DispName ?? ToLabel(key);
        attributes.Category ??= ToLabel(section);
        attributes.Order ??= _nextOrder--;
        if (advanced) attributes.IsAdvanced = true;

        return _config.Bind(section, key, defaultValue,
            new ConfigDescription(description, acceptableValues, attributes));
    }
}
