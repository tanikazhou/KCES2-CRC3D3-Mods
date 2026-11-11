using CRC;

namespace CRC3D3.KeyHints.Game;

/// <summary>Context "Dance": dance scene loaded.</summary>
internal static class DanceContext
{
    /// <summary>Context name in tags.</summary>
    public const string Name = "Dance";

    private static DanceMain _current;

    /// <summary>Marks dance scene as loaded.</summary>
    /// <returns><see langword="true"/> while dance scene is current.</returns>
    public static bool Loaded(DanceMain dance)
    {
        _current = dance;
        return true;
    }

    /// <summary>Marks dance scene as gone.</summary>
    /// <returns><see langword="true"/> while another dance scene is still current.</returns>
    public static bool Destroyed(DanceMain dance)
    {
        if (ReferenceEquals(_current, dance))
        {
            _current = null;
        }

        return _current != null;
    }
}
