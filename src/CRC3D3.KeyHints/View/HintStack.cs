using CRC3D3.KeyHints.Rows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CRC3D3.KeyHints.View;

/// <summary>Stack of key hint rows at bottom right, in CRC3D3 "Esc" hint style.</summary>
/// <remarks>
/// <para>Own canvas object (<c>DontDestroyOnLoad</c>), sort order 30000, all names with <c>XUAIGNORE</c>, so XUnity leaves text alone.
/// If other code destroys it, next show builds it again.</para>
/// <para>Row objects are reused: row i shows i-th planned row. New row object only when more rows show than ever before.
/// Text and cap width change only when text changes. Hidden with <see cref="Behaviour.enabled"/> of canvas.</para>
/// </remarks>
internal static class HintStack
{
    private const float CapH = 34f;

    private static readonly List<Row> _rows = [];

    private static GameObject _root;
    private static Canvas _canvas;
    private static RectTransform _stack;
    private static Sprite _capSprite;
    private static TMP_FontAsset _font;
    private static Material _fontMaterial;

    /// <summary>Gets name of font in use, for log.</summary>
    public static string FontName => _font != null ? _font.name : "TMP default";

    /// <summary>Uses CRC3D3 font for all rows, also for rows made later.</summary>
    public static void UseFont(TMP_FontAsset font, Material material)
    {
        _font = font;
        _fontMaterial = material;
        foreach (Row r in _rows)
        {
            if (r.Key == null) continue;

            ApplyFont(r.Key);
            ApplyFont(r.Label);
            r.LastKey = null;
        }
    }

    /// <summary>Shows rows; hides stack for empty list.</summary>
    /// <param name="rows">Planned rows, top to bottom.</param>
    /// <param name="scale">Stack size, 1 = about CRC3D3 window hint size at 1080p.</param>
    public static void Show(List<RowText> rows, float scale)
    {
        if (rows.Count == 0)
        {
            Hide();
            return;
        }

        if (_root == null) Build();

        _stack.localScale = Vector3.one * Mathf.Clamp(scale, 0.3f, 3f);
        for (int i = 0; i < rows.Count; i++)
        {
            if (i == _rows.Count) _rows.Add(MakeRow(i));
            SetRow(_rows[i], rows[i]);
        }

        for (int i = rows.Count; i < _rows.Count; i++)
        {
            if (_rows[i].Object.activeSelf) _rows[i].Object.SetActive(false);
        }

        _canvas.enabled = true;
    }

    /// <summary>Hides stack.</summary>
    public static void Hide()
    {
        if (_canvas != null) _canvas.enabled = false;
    }

    private static void SetRow(Row r, RowText text)
    {
        if (!r.Object.activeSelf) r.Object.SetActive(true);

        if (text.Key != r.LastKey)
        {
            r.LastKey = text.Key;
            r.Key.text = text.Key;
            r.CapLayout.preferredWidth = Mathf.Max(CapH, r.Key.GetPreferredValues(text.Key).x + 18f);
        }

        if (text.Label != r.LastLabel)
        {
            r.LastLabel = text.Label;
            r.Label.text = text.Label;
        }
    }

    private static void Build()
    {
        _rows.Clear();
        _root = new GameObject("CRC3D3 KeyHints XUAIGNORE");
        Object.DontDestroyOnLoad(_root);
        _canvas = _root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 30000;
        CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var stack = new GameObject("Stack XUAIGNORE", typeof(RectTransform));
        stack.transform.SetParent(_root.transform, false);
        _stack = (RectTransform)stack.transform;
        _stack.anchorMin = _stack.anchorMax = new Vector2(1f, 0f);
        _stack.pivot = new Vector2(1f, 0f);
        _stack.anchoredPosition = new Vector2(-28f, 28f);

        VerticalLayoutGroup v = stack.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.LowerRight;
        v.spacing = 8f;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        ContentSizeFitter fit = stack.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (_capSprite == null) _capSprite = CapSprite.Make();
    }

    private static Row MakeRow(int index)
    {
        var row = new GameObject($"Row {index} XUAIGNORE", typeof(RectTransform));
        row.transform.SetParent(_stack, false);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleRight;
        h.spacing = 10f;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;

        var cap = new GameObject("Cap XUAIGNORE", typeof(RectTransform));
        cap.transform.SetParent(row.transform, false);
        Image img = cap.AddComponent<Image>();
        img.sprite = _capSprite;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        LayoutElement le = cap.AddComponent<LayoutElement>();
        le.preferredWidth = CapH;
        le.preferredHeight = CapH;
        le.minHeight = CapH;

        TextMeshProUGUI key = NewText("Key XUAIGNORE", cap.transform, 20f, TextAlignmentOptions.Center);
        RectTransform kr = key.rectTransform;
        kr.anchorMin = Vector2.zero;
        kr.anchorMax = Vector2.one;
        kr.offsetMin = kr.offsetMax = Vector2.zero;
        key.fontStyle = FontStyles.Bold;

        TextMeshProUGUI label = NewText("Label XUAIGNORE", row.transform, 24f, TextAlignmentOptions.MidlineLeft);

        return new Row { Object = row, CapLayout = le, Key = key, Label = label };
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        ApplyFont(t);
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = align;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        t.richText = false;
        return t;
    }

    private static void ApplyFont(TextMeshProUGUI t)
    {
        if (_font == null) return;

        t.font = _font;
        if (_fontMaterial != null) t.fontSharedMaterial = _fontMaterial;
    }

    /// <summary>Objects of one row on screen.</summary>
    private sealed class Row
    {
        public GameObject Object;
        public LayoutElement CapLayout;
        public TextMeshProUGUI Key;
        public TextMeshProUGUI Label;
        public string LastKey;
        public string LastLabel;
    }
}
