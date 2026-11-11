namespace CRC3D3.KeyHints.Rows;

/// <summary>On or off state of game contexts that key hint tags name.</summary>
/// <remarks>Names are compared ordinal. Unknown name is never on, so its rows never show.</remarks>
internal sealed class ContextSet
{
    /// <summary>Context names that tags can use.</summary>
    public static readonly string[] Known = ["Field", "Dance"];

    private bool _field;
    private bool _dance;

    /// <summary>Determines whether name is a known context.</summary>
    public static bool IsKnown(string name) => name is "Field" or "Dance";

    /// <summary>Sets context state.</summary>
    /// <returns><see langword="true"/> if state changed; <see langword="false"/> for same state or unknown name.</returns>
    public bool Set(string name, bool on)
    {
        switch (name)
        {
            case "Field" when _field != on:
                _field = on;
                return true;
            case "Dance" when _dance != on:
                _dance = on;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Determines whether context is on.</summary>
    public bool IsOn(string name) => name switch
    {
        "Field" => _field,
        "Dance" => _dance,
        _ => false,
    };

    /// <summary>Determines whether row with these contexts can show.</summary>
    /// <returns><see langword="true"/> for empty list or when one listed context is on.</returns>
    public bool AllowsAny(string[] contexts)
    {
        if (contexts.Length == 0)
        {
            return true;
        }

        foreach (string c in contexts)
        {
            if (IsOn(c))
            {
                return true;
            }
        }

        return false;
    }
}
