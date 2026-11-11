using UnityEngine;

namespace CRC3D3.KeyHints.View;

/// <summary>Key cap sprite in style of CRC3D3 window hint "Esc".</summary>
/// <remarks>
/// CRC3D3 cap is one picture with baked text (<c>nw_button_esc</c>, 56x55: fill 253,0,17 at alpha 0.6, 2 px border 186,0,12).
/// This sprite has same colors and is 9-sliced, so it stretches to key text.
/// </remarks>
internal static class CapSprite
{
    private const int Size = 32;
    private const int Border = 2;
    private const int Radius = 5;

    /// <summary>Makes cap sprite with rounded corners.</summary>
    /// <returns>New sprite; caller keeps it for session.</returns>
    public static Sprite Make()
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var fill = new Color32(253, 0, 17, 153);
        var edge = new Color32(186, 0, 12, 255);
        var px = new Color32[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                // Distance outside rounded rectangle, for corners.
                float cx = Mathf.Clamp(x + 0.5f, Radius, Size - Radius);
                float cy = Mathf.Clamp(y + 0.5f, Radius, Size - Radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float inside = Mathf.Clamp01(Radius - d + 0.5f);
                bool isEdge = x < Border || y < Border || x >= Size - Border || y >= Size - Border || d > Radius - Border;
                Color32 c = isEdge ? edge : fill;
                c.a = (byte)(c.a * inside);
                px[(y * Size) + x] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, true);
        const float slice = Radius + 2;
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(slice, slice, slice, slice));
    }
}
