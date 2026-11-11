using System.Text;
using BepInEx.Configuration;
using CRC;
using KCES2.Fusebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CRC3D3.DebugTools.Overlay;

/// <summary>Text panel at top right with lines from debug sections, shown and hidden by key.</summary>
/// <remarks>
/// <para>
/// Event driven: sections mark overlay dirty from game hooks (<see cref="MarkDirty"/>), and text is built only then. Per frame while overlay
/// is on: key test, two flags, and two timers (empty notice, follow panel tracking). Sections are called in <see cref="WriteSections"/>, in
/// one place, so all work of a refresh is visible there. Text goes to TextMeshPro through <c>SetText(StringBuilder)</c>, so this plugin makes
/// no string of it (XUnity's <c>SetText</c> hook reads it back, once per change); label changes only when text or size changed.
/// </para>
/// <para>
/// While follow character panel shows (<see cref="FollowPanel"/>) and overlaps panel, panel moves below it. After each main menu window
/// change (<see cref="GameUiHooks"/>), its position is read <see cref="TrackPerSecond"/> times per second until it hides or stays still for
/// <see cref="TrackStableSamples"/> reads (menu window animates).
/// Panel hides while sections append nothing, unless setting <c>ShowWhenEmpty</c> is on; after toggle key turns overlay on, notice shows for
/// <see cref="EmptyNoticeSeconds"/> instead. Hiding switches <c>Canvas.enabled</c>, not <c>SetActive</c> (no rebuild on show).
/// </para>
/// <para>
/// Own canvas object (<c>DontDestroyOnLoad</c>), names with <c>XUAIGNORE</c>, so XUnity does not translate script names and labels. If other
/// code destroys it, next refresh builds it again. Font: CRC3D3 Japanese font asset, taken from game UI when it loads, else TMP default.
/// Not CRC3D3 root UI font asset: its only own glyph, <c>_</c>, has SDF peak below edge value and never draws; its fallback is the Japanese
/// asset. Rich text off: text shows game data as it is.
/// </para>
/// </remarks>
internal static class DebugOverlay
{
    internal const string Feature = "DebugOverlay";

    /// <summary>Key hint row for toggle key, shown in town while CRC3D3.KeyHints runs.</summary>
    internal static readonly KeyHint Hint = new() { Context = "Field", Order = 300, Label = "Debug overlay" };

    private const string FontName = "ja_BIZUDGothic-Regular SDF";
    private const string EmptyLine = "Nothing to show";
    private const string NoticeLine = "On. Nothing to show, hides in a few seconds";
    private const float Padding = 10f;
    private const float Margin = 16f;
    private const float FollowGap = 8f;
    private const float EmptyNoticeSeconds = 3f;
    private const float TrackPerSecond = 10f;
    private const int TrackStableSamples = 5;

    private static readonly StringBuilder _text = new(4096);
    private static readonly StringBuilder _shown = new(4096);

    private static ConfigEntry<KeyboardShortcut> _key;
    private static ConfigEntry<int> _fontSize;
    private static ConfigEntry<bool> _showWhenEmpty;
    private static string _title;
    private static bool _dirty;
    private static float _noticeUntil;
    private static bool _trackFollow;
    private static float _nextTrack;
    private static bool _placeDue;
    private static GameObject _root;
    private static Canvas _canvas;
    private static RectTransform _panel;
    private static TextMeshProUGUI _label;
    private static TMP_FontAsset _font;
    private static bool _visible;
    private static bool _belowFollow;
    private static Rect _lastFollow;
    private static int _stableSamples;

    /// <summary>Gets a value indicating whether overlay is on (toggle key), shown or hidden while empty.</summary>
    internal static bool IsOn { get; private set; }

    internal static void Bind(SettingList settings)
    {
        _key = settings.Bind("Overlay", "ToggleKey", new KeyboardShortcut(KeyCode.F3),
            "Key that shows or hides the debug overlay at the top right of the screen.", hint: Hint);
        _fontSize = settings.Bind("Overlay", "FontSize", 18,
            "Text size of the debug overlay, in points at 1080 lines screen height.",
            new AcceptableValueRange<int>(10, 40));
        _showWhenEmpty = settings.Bind("Overlay", "ShowWhenEmpty", false,
            "Keep the debug overlay visible when no feature has lines to show. "
            + "When off, the overlay hides then, and after the toggle key a short notice shows for a few seconds.");
        _title = $"Debug overlay ({_key.Value})";
        _key.SettingChanged += static (_, _) =>
        {
            _title = $"Debug overlay ({_key.Value})";
            MarkDirty();
        };
        _fontSize.SettingChanged += static (_, _) => MarkDirty();
        _showWhenEmpty.SettingChanged += static (_, _) => MarkDirty();
    }

    /// <summary>Requests text rebuild at next frame while overlay is on.</summary>
    internal static void MarkDirty() => _dirty = true;

    /// <summary>Starts reading follow character panel position, from when main menu shows it until it hides.</summary>
    internal static void TrackFollowPanel()
    {
        _trackFollow = true;
        _nextTrack = 0f;
        _stableSamples = 0;
    }

    /// <summary>Takes CRC3D3 Japanese font asset from fonts of game UI that just loaded.</summary>
    internal static void UseGameFont(GameUIAdapter ui)
    {
        if (_font != null) return;

        var seen = new HashSet<TMP_FontAsset>();
        foreach (TMP_Text t in ui.GetComponentsInChildren<TMP_Text>(true))
        {
            TMP_FontAsset font = t.font;
            if (font == null || !seen.Add(font)) continue;

            TMP_FontAsset found = font.name == FontName ? font : font.fallbackFontAssetTable?.Find(static f => f != null && f.name == FontName);
            if (found == null) continue;

            _font = found;
            if (_label)
            {
                _label.font = found;
                _shown.Length = 0;
            }

            MarkDirty();
            Plugin.Log.LogInfo($"Debug overlay font: {found.name}, from game UI font {font.name}");
            return;
        }

        Plugin.Log.LogWarning($"Debug overlay font: {FontName} not in game UI fonts ({seen.Count} checked), TMP default font kept");
    }

    /// <summary>Handles toggle key, refreshes text when dirty, tracks follow panel position while it shows.</summary>
    internal static void Tick()
    {
        if (KeyInput.IsDownEitherSide(_key.Value)) Toggle();
        if (!IsOn) return;

        if (_noticeUntil > 0f && Time.unscaledTime >= _noticeUntil)
        {
            _noticeUntil = 0f;
            _dirty = true;
        }

        if (_trackFollow && Time.unscaledTime >= _nextTrack)
        {
            _nextTrack = Time.unscaledTime + (1f / TrackPerSecond);
            _placeDue = true;
        }

        if (_dirty)
        {
            Refresh();
        }
        else if (_placeDue && _visible && _root)
        {
            Place();
        }
    }

    private static void Toggle()
    {
        IsOn = !IsOn;
        Plugin.Log.LogInfo($"Debug overlay {(IsOn ? "on" : "off")}");
        if (IsOn)
        {
            EventInfo.DropCache();
            _noticeUntil = Time.unscaledTime + EmptyNoticeSeconds;
            _dirty = true;
            return;
        }

        _noticeUntil = 0f;
        if (_canvas) SetVisible(false);
    }

    // ReSharper disable Unity.PerformanceAnalysis - Runs only when _dirty is set: game hook, setting, toggle, notice end.
    private static void Refresh()
    {
        _dirty = false;
        if (!_root || !_label)
        {
            if (_root) Object.Destroy(_root);
            Build();
        }

        _text.Clear();
        _text.Append(_title).Append('\n');
        int bodyStart = _text.Length;
        WriteSections(_text);
        int length = _text.Length;
        while (length > 0 && char.IsWhiteSpace(_text[length - 1])) length--;
        _text.Length = length;

        bool empty = length < bodyStart;
        bool always = _showWhenEmpty.Value;
        bool notice = empty && !always && _noticeUntil > 0f;
        SetVisible(!empty || always || notice);
        if (!_visible) return;

        if (empty) _text.Append('\n').Append(notice ? NoticeLine : EmptyLine);
        UpdateLabel();
        Place();
    }

    /// <summary>Appends lines of each section whose feature is on; exception turns off that feature and drops its partial lines.</summary>
    private static void WriteSections(StringBuilder sb)
    {
        WriteSection(sb, PlayerPosition.Feature, PlayerPosition.Write);
        WriteSection(sb, EventInfo.Feature, EventInfo.Write);
    }

    private static void WriteSection(StringBuilder sb, string feature, Action<StringBuilder> write)
    {
        if (Plugin.Guard.IsOff(feature)) return;

        int start = sb.Length;
        try
        {
            write(sb);
        }
        catch (Exception e)
        {
            sb.Length = start;
            Plugin.Guard.Fail(feature, e);
        }
    }

    /// <summary>Gives text to label when text or font size changed, then fits panel to it.</summary>
    private static void UpdateLabel()
    {
        int size = _fontSize.Value;
        if (SameAsShown() && Mathf.Approximately(_label.fontSize, size)) return;

        _shown.Length = 0;
        for (int i = 0; i < _text.Length; i++) _shown.Append(_text[i]);
        _label.fontSize = size;
        _label.SetText(_text);
        Vector2 preferred = _label.GetPreferredValues();
        _panel.sizeDelta = new Vector2(preferred.x + (2f * Padding), preferred.y + (2f * Padding));
    }

    private static bool SameAsShown()
    {
        if (_text.Length != _shown.Length) return false;

        for (int i = 0; i < _text.Length; i++)
        {
            if (_text[i] != _shown[i]) return false;
        }

        return true;
    }

    private static void SetVisible(bool visible)
    {
        if (visible == _visible) return;

        _visible = visible;
        _canvas.enabled = visible;
        Plugin.Log.LogDebug(visible ? "Debug overlay panel shown" : "Debug overlay panel hidden");
    }

    /// <summary>Puts panel at top right, below follow character panel while that panel shows and overlaps it.</summary>
    private static void Place()
    {
        _placeDue = false;
        float scale = _canvas.scaleFactor;
        float right = Screen.width - (Margin * scale);
        float left = right - (_panel.sizeDelta.x * scale);
        bool followShown = FollowPanel.TryGetScreenRect(out Rect follow);
        _stableSamples = followShown && follow == _lastFollow ? _stableSamples + 1 : 0;
        _trackFollow = followShown && _stableSamples < TrackStableSamples;
        _lastFollow = followShown ? follow : default;
        bool below = followShown && follow.xMax > left && follow.xMin < right;
        float top = below ? Mathf.Max(Margin, ((Screen.height - follow.yMin) / scale) + FollowGap) : Margin;
        var position = new Vector2(-Margin, -top);
        if (_panel.anchoredPosition != position) _panel.anchoredPosition = position;
        if (below == _belowFollow) return;

        _belowFollow = below;
        Plugin.Log.LogInfo(below ? $"Debug overlay below follow character panel, top {top:0}" : "Debug overlay at top right");
    }

    private static void Build()
    {
        _root = new GameObject("DebugOverlay XUAIGNORE");
        Object.DontDestroyOnLoad(_root);
        _canvas = _root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32000;
        CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        var panel = new GameObject("Panel XUAIGNORE", typeof(RectTransform));
        panel.transform.SetParent(_root.transform, false);
        _panel = (RectTransform)panel.transform;
        _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(1f, 1f);
        _panel.anchoredPosition = new Vector2(-Margin, -Margin);
        Image back = panel.AddComponent<Image>();
        back.color = new Color(0f, 0f, 0f, 0.6f);
        back.raycastTarget = false;

        var text = new GameObject("Text XUAIGNORE", typeof(RectTransform));
        text.transform.SetParent(_panel, false);
        var rt = (RectTransform)text.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(Padding, Padding);
        rt.offsetMax = new Vector2(-Padding, -Padding);
        _label = text.AddComponent<TextMeshProUGUI>();
        if (_font != null) _label.font = _font;
        _label.fontSize = _fontSize.Value;
        _label.color = Color.white;
        _label.alignment = TextAlignmentOptions.TopLeft;
        _label.enableWordWrapping = false;
        _label.richText = false;
        _label.raycastTarget = false;
        _shown.Length = 0;
        _visible = true;
        _belowFollow = false;
        _lastFollow = default;
        Plugin.Log.LogInfo($"Debug overlay built at top right, font {(_font != null ? _font.name : "TMP default")}");
    }
}
