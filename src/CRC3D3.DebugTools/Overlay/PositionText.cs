using System.Text;

namespace CRC3D3.DebugTools.Overlay;

/// <summary>Player position texts: overlay line and copy text in script tag form.</summary>
/// <remarks>
/// Digits are written from rounded integers: no culture, no allocation for overlay line, and same text on Mono and .NET
/// (value that rounds to zero shows no minus sign).
/// </remarks>
internal static class PositionText
{
    private const int ScreenScale = 100;
    private const int CopyScale = 1000;
    private const int FacingScale = 10;

    /// <summary>Rounds position and facing to overlay precision: centimeters and 0.1 degree.</summary>
    /// <returns>Values that differ exactly when overlay text differs.</returns>
    public static (long X, long Y, long Z, long Facing) Round(float x, float y, float z, float facing) =>
        (Scaled(x, ScreenScale), Scaled(y, ScreenScale), Scaled(z, ScreenScale), Scaled(facing, FacingScale));

    /// <summary>Appends overlay line, for example "Position X -63.02  Y 0.00  Z -3.69  Facing 184.8".</summary>
    /// <param name="sb">Text to append to.</param>
    /// <param name="v">Values from <see cref="Round"/>.</param>
    public static void AppendScreenLine(StringBuilder sb, (long X, long Y, long Z, long Facing) v)
    {
        sb.Append("Position X ");
        AppendFixed(sb, v.X, ScreenScale);
        sb.Append("  Y ");
        AppendFixed(sb, v.Y, ScreenScale);
        sb.Append("  Z ");
        AppendFixed(sb, v.Z, ScreenScale);
        sb.Append("  Facing ");
        AppendFixed(sb, v.Facing, FacingScale);
    }

    /// <summary>Builds copy text in script tag form: <c>pos={"x":-63.020,"y":0.000,"z":-3.689} rot={"x":0,"y":184.8,"z":0}</c>.</summary>
    /// <returns>Position in millimeters and facing in 0.1 degree.</returns>
    public static string CopyText(float x, float y, float z, float facing)
    {
        var sb = new StringBuilder(80);
        sb.Append("pos={\"x\":");
        AppendFixed(sb, Scaled(x, CopyScale), CopyScale);
        sb.Append(",\"y\":");
        AppendFixed(sb, Scaled(y, CopyScale), CopyScale);
        sb.Append(",\"z\":");
        AppendFixed(sb, Scaled(z, CopyScale), CopyScale);
        sb.Append("} rot={\"x\":0,\"y\":");
        AppendFixed(sb, Scaled(facing, FacingScale), FacingScale);
        sb.Append(",\"z\":0}");
        return sb.ToString();
    }

    private static long Scaled(float value, int scale) => (long)Math.Round((double)value * scale, MidpointRounding.AwayFromZero);

    private static void AppendFixed(StringBuilder sb, long scaled, int scale)
    {
        if (scaled < 0)
        {
            sb.Append('-');
            scaled = -scaled;
        }

        AppendDigits(sb, scaled / scale);
        sb.Append('.');
        long fraction = scaled % scale;
        for (int digit = scale / 10; digit > 0; digit /= 10)
        {
            sb.Append((char)('0' + (fraction / digit % 10)));
        }
    }

    private static void AppendDigits(StringBuilder sb, long value)
    {
        if (value >= 10)
        {
            AppendDigits(sb, value / 10);
        }

        sb.Append((char)('0' + (value % 10)));
    }
}
