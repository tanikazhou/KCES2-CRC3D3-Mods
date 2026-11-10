using CRC;
using CRC.UI;
using HarmonyLib;
using UnityEngine;

namespace CRC3D3.DebugTools.Overlay;

/// <summary>Screen area of follow character panel: status window of character who follows player.</summary>
/// <remarks>
/// <para>
/// Panel is <c>MainMenuManager.m_heroineParameterWindow</c>; it shows only while main menu is open and character follows player
/// (<c>MainMenuManager.ShowHeroineParameter</c>).
/// </para>
/// <para>
/// References are taken when game UI loads (<see cref="GameUiHooks"/>), not looked up per refresh. Each read checks window lifetime first
/// (Unity null check: managed pointer read for live object), so window destroyed by other code means "not shown", not exception.
/// </para>
/// </remarks>
internal static class FollowPanel
{
    private static readonly AccessTools.FieldRef<MainMenuManager, GameObject> _windowField =
        AccessTools.FieldRefAccess<MainMenuManager, GameObject>("m_heroineParameterWindow");

    private static readonly Vector3[] _corners = new Vector3[4];

    private static GameUIAdapter _owner;
    private static GameObject _window;
    private static RectTransform _rect;
    private static Camera _camera;

    /// <summary>Tries to get screen rectangle of follow character panel, in pixels from bottom left.</summary>
    /// <param name="rect">Panel rectangle; empty when panel does not show.</param>
    /// <returns><see langword="true"/> if panel shows.</returns>
    internal static bool TryGetScreenRect(out Rect rect)
    {
        rect = default;
        if (!_window || !_window.activeInHierarchy) return false;

        _rect.GetWorldCorners(_corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(_camera, _corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(_camera, _corners[2]);
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    /// <summary>Takes panel references from game UI that just loaded.</summary>
    internal static void Capture(GameUIAdapter ui)
    {
        Forget();
        MainMenuManager menu = ui.mainMenuManager;
        if (menu == null) return;

        GameObject window = _windowField(menu);
        if (window == null) return;

        Canvas canvas = window.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;

        Canvas root = canvas.rootCanvas;
        _owner = ui;
        _window = window;
        _rect = (RectTransform)window.transform;
        _camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        Plugin.Log.LogInfo($"Follow character panel found ({root.renderMode})");
    }

    /// <summary>Drops panel references when game UI that owns them goes away.</summary>
    internal static void Forget(GameUIAdapter ui)
    {
        if (ReferenceEquals(ui, _owner)) Forget();
    }

    private static void Forget()
    {
        _owner = null;
        _window = null;
        _rect = null;
        _camera = null;
    }
}
