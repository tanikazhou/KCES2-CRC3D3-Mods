using BepInEx.Configuration;

namespace CRC3D3.KeyHints.Tests.Tags.Other;

// Tests set only fields they need; reader reads all by reflection.
#pragma warning disable CS0649

/// <summary>Copy of tag class as another plugin compiles it: same name and fields, other type.</summary>
internal sealed class KeyHint
{
    public string Label;
    public int Order;
    public string Context;
    public bool Active = true;
    public KeyboardShortcut? Key;
    public Action Changed;
}
