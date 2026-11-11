using BepInEx.Configuration;
using UnityEngine;

namespace CRC3D3.KeyHints.Discovery;

/// <summary>Key hint tag found on one setting of one plugin.</summary>
internal sealed class FoundHint
{
    private readonly TagFields _fields;
    private readonly string _fallbackLabel;

    /// <summary>Initializes found tag.</summary>
    /// <param name="pluginGuid">GUID of plugin that owns setting.</param>
    /// <param name="entry">Setting that carries tag.</param>
    /// <param name="tag">Tag object of plugin.</param>
    /// <param name="fields">Fields of tag class.</param>
    /// <param name="contexts">Context names of tag.</param>
    /// <param name="fallbackLabel">Row text when tag has none.</param>
    /// <param name="index">Discovery order over all plugins.</param>
    public FoundHint(string pluginGuid, ConfigEntryBase entry, object tag, TagFields fields, string[] contexts, string fallbackLabel, int index)
    {
        PluginGuid = pluginGuid;
        Entry = entry;
        Tag = tag;
        _fields = fields;
        Contexts = contexts;
        _fallbackLabel = fallbackLabel;
        Index = index;
    }

    /// <summary>Gets GUID of plugin that owns setting.</summary>
    public string PluginGuid { get; }

    /// <summary>Gets setting that carries tag.</summary>
    public ConfigEntryBase Entry { get; }

    /// <summary>Gets tag object of plugin.</summary>
    public object Tag { get; }

    /// <summary>Gets context names, read once at discovery; empty = all contexts.</summary>
    public string[] Contexts { get; }

    /// <summary>Gets discovery order over all plugins, for stable sort.</summary>
    public int Index { get; }

    /// <summary>Reads current label, order, state and key.</summary>
    /// <returns>Values of this read.</returns>
    /// <remarks>
    /// <para>Reflection with boxing: call only while rows rebuild, never per frame.</para>
    /// <para>Row is active when <c>Active</c> is set, <c>Failed</c> is not, and setting is no <see langword="bool"/> switch set to
    /// <see langword="false"/>. Missing field = default.</para>
    /// </remarks>
    public HintState Read()
    {
        string label = _fields.Label?.GetValue(Tag) as string;
        int order = _fields.Order == null ? 0 : (int)_fields.Order.GetValue(Tag);
        bool active = _fields.Active == null || (bool)_fields.Active.GetValue(Tag);
        bool failed = _fields.Failed != null && (bool)_fields.Failed.GetValue(Tag);
        bool switchOff = Entry.BoxedValue is false;
        active = active && !failed && !switchOff;
        return new HintState(string.IsNullOrEmpty(label) ? _fallbackLabel : label, order, active, Key());
    }

    private KeyboardShortcut Key()
    {
        return _fields.Key?.GetValue(Tag) is KeyboardShortcut fixedKey
            ? fixedKey
            : Entry.BoxedValue switch
            {
                KeyboardShortcut shortcut => shortcut,
                KeyCode code => new KeyboardShortcut(code),
                _ => KeyboardShortcut.Empty,
            };
    }
}
