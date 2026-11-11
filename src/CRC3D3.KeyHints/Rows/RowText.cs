namespace CRC3D3.KeyHints.Rows;

/// <summary>Text of one shown key hint row.</summary>
internal readonly struct RowText
{
    /// <summary>Initializes row text.</summary>
    /// <param name="key">Short key text, e.g. "Ctrl+F8".</param>
    /// <param name="label">Row label.</param>
    public RowText(string key, string label)
    {
        Key = key;
        Label = label;
    }

    /// <summary>Gets short key text.</summary>
    public string Key { get; }

    /// <summary>Gets row label.</summary>
    public string Label { get; }
}
