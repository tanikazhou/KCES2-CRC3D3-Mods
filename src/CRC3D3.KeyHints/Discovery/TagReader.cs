using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace CRC3D3.KeyHints.Discovery;

/// <summary>Finds key hint tags on settings of one plugin.</summary>
/// <remarks>
/// Tag = object in <see cref="ConfigDescription.Tags"/> whose class is named <see cref="TagTypeName"/>, from any assembly,
/// as ConfigurationManager finds <c>ConfigurationManagerAttributes</c>. No type reference to declaring plugins.
/// </remarks>
internal static class TagReader
{
    /// <summary>Class name of key hint tag.</summary>
    public const string TagTypeName = "KeyHint";

    private const string AttributesTypeName = "ConfigurationManagerAttributes";

    /// <summary>Adds key hint tags of plugin config file to list.</summary>
    /// <param name="pluginGuid">GUID of plugin.</param>
    /// <param name="config">Config file of plugin.</param>
    /// <param name="onChanged">Handler written into each tag's change signal.</param>
    /// <param name="log">Log for skipped tags.</param>
    /// <param name="into">List that gets found tags, in setting order.</param>
    /// <remarks>Main thread only. Tag with wrong field type or without key is skipped with one warning.</remarks>
    public static void Read(string pluginGuid, ConfigFile config, Action onChanged, ManualLogSource log, List<FoundHint> into)
    {
        foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in config)
        {
            ConfigEntryBase entry = pair.Value;
            object[] tags = entry.Description?.Tags;
            if (tags == null)
            {
                continue;
            }

            foreach (object tag in tags)
            {
                if (tag?.GetType().Name == TagTypeName)
                {
                    Add(pluginGuid, entry, tag, tags, onChanged, log, into);
                }
            }
        }
    }

    /// <summary>Splits comma-separated context list.</summary>
    /// <returns>Trimmed names without empty ones; empty array for <see langword="null"/>.</returns>
    public static string[] ParseContexts(string context) =>
        string.IsNullOrEmpty(context)
            ? []
            : [.. context.Split(',').Select(static c => c.Trim()).Where(static c => c.Length > 0)];

    private static void Add(string pluginGuid, ConfigEntryBase entry, object tag, object[] tags, Action onChanged, ManualLogSource log,
        List<FoundHint> into)
    {
        string where = $"{pluginGuid} [{entry.Definition.Section}] {entry.Definition.Key}";
        var fields = TagFields.For(tag.GetType(), out string error);
        if (fields == null)
        {
            log.LogWarning($"Key hints: {where}: tag skipped, {error}");
            return;
        }

        bool keySetting = entry.SettingType == typeof(KeyboardShortcut) || entry.SettingType == typeof(KeyCode);
        if (!keySetting && fields.Key?.GetValue(tag) == null)
        {
            log.LogWarning($"Key hints: {where}: tag skipped, setting has no key and tag sets no Key");
            return;
        }

        fields.Changed?.SetValue(tag, onChanged);
        string[] contexts = ParseContexts(fields.Context?.GetValue(tag) as string);
        into.Add(new FoundHint(pluginGuid, entry, tag, fields, contexts, ShownName(entry, tags), into.Count));
    }

    /// <summary>Gets shown name from ConfigurationManager tag, else setting key.</summary>
    private static string ShownName(ConfigEntryBase entry, object[] tags)
    {
        foreach (object tag in tags)
        {
            if (tag?.GetType().Name == AttributesTypeName
                && tag.GetType().GetField("DispName", BindingFlags.Instance | BindingFlags.Public)?.GetValue(tag) is string name
                && name.Length > 0)
            {
                return name;
            }
        }

        return entry.Definition.Key;
    }
}
