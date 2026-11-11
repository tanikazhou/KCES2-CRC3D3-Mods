using CRC;
using CrcMove.Controller;

namespace CRC3D3.KeyHints.Game;

/// <summary>Context "Field": player walks in town and field takes input.</summary>
internal static class FieldContext
{
    /// <summary>Context name in tags.</summary>
    public const string Name = "Field";

    /// <summary>Reads state from game.</summary>
    /// <returns><see langword="true"/> while player controller exists and game input layer is enabled and active.</returns>
    /// <remarks>
    /// Same test as field keys of other plugins, without focused text field (no event for it).
    /// Main menu and map turn input layer off, so state is off while they show. Called only from game hooks, never per frame.
    /// </remarks>
    public static bool Read()
    {
        GameManager gm = GameMain.GameManager;
        InputManager input = GameMain.InputManager;
        GameInputLayer layer = gm != null ? gm.inputLayer : null;
        return PlayerMoveController.Instance != null && layer != null && layer.IsInputLayerEnabled && input != null && input.IsActiveLayer(layer);
    }
}
