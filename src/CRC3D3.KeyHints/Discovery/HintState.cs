using BepInEx.Configuration;

namespace CRC3D3.KeyHints.Discovery;

/// <summary>Values of one key hint at one read.</summary>
internal readonly struct HintState
{
    /// <summary>Initializes values of one read.</summary>
    /// <param name="label">Row text.</param>
    /// <param name="order">Sort key, lowest first.</param>
    /// <param name="active">Plugin state, feature state and switch allow row.</param>
    /// <param name="key">Key, <see cref="KeyboardShortcut.Empty"/> for no key.</param>
    public HintState(string label, int order, bool active, KeyboardShortcut key)
    {
        Label = label;
        Order = order;
        Active = active;
        Key = key;
    }

    /// <summary>Gets row text.</summary>
    public string Label { get; }

    /// <summary>Gets sort key, lowest first.</summary>
    public int Order { get; }

    /// <summary>Gets a value indicating whether plugin state, feature state and switch allow row.</summary>
    public bool Active { get; }

    /// <summary>Gets key; <see cref="KeyboardShortcut.Empty"/> for no key.</summary>
    public KeyboardShortcut Key { get; }
}
