using System.Globalization;
using System.Text;
using AwesomeAssertions;
using CRC3D3.DebugTools.Overlay;
using Xunit;

namespace CRC3D3.DebugTools.Tests;

/// <summary>Tests of <see cref="PositionText"/> overlay line and copy text.</summary>
public class PositionTextTests
{
    [Fact]
    public void ScreenLine_TwoDecimalsAndFacingOneDecimal() =>
        Line(-63.02011f, 0f, -3.68884f, 184.8195f).Should().Be("Position X -63.02  Y 0.00  Z -3.69  Facing 184.8");

    [Fact]
    public void ScreenLine_SmallNegativeShowsNoMinus() =>
        Line(-0.001f, -0.004f, 0.004f, 0.02f).Should().Be("Position X 0.00  Y 0.00  Z 0.00  Facing 0.0");

    [Fact]
    public void ScreenLine_NegativeBelowOne() =>
        Line(-0.5f, -0.05f, 1234.5f, 359.94f).Should().Be("Position X -0.50  Y -0.05  Z 1234.50  Facing 359.9");

    [Fact]
    public void CopyText_TagForm() =>
        PositionText.CopyText(-63.02011f, 0f, -3.68884f, 184.8195f)
            .Should().Be("pos={\"x\":-63.020,\"y\":0.000,\"z\":-3.689} rot={\"x\":0,\"y\":184.8,\"z\":0}");

    [Fact]
    public void CopyText_SmallNegativeShowsNoMinus() =>
        PositionText.CopyText(-0.0004f, 0f, 0f, 0f).Should().Be("pos={\"x\":0.000,\"y\":0.000,\"z\":0.000} rot={\"x\":0,\"y\":0.0,\"z\":0}");

    [Fact]
    public void Texts_IgnoreCurrentCulture()
    {
        CultureInfo old = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try
        {
            Line(1.5f, 2.25f, -3.75f, 90f).Should().Be("Position X 1.50  Y 2.25  Z -3.75  Facing 90.0");
            PositionText.CopyText(1.5f, 2.25f, -3.75f, 90f).Should().Be("pos={\"x\":1.500,\"y\":2.250,\"z\":-3.750} rot={\"x\":0,\"y\":90.0,\"z\":0}");
        }
        finally
        {
            CultureInfo.CurrentCulture = old;
        }
    }

    [Fact]
    public void Round_SameWhenScreenTextSame() =>
        PositionText.Round(1.001f, 2f, 3f, 10.01f).Should().Be(PositionText.Round(1.004f, 2f, 3f, 10.04f));

    [Fact]
    public void Round_DiffersWhenScreenTextDiffers() =>
        PositionText.Round(1.004f, 2f, 3f, 10f).Should().NotBe(PositionText.Round(1.006f, 2f, 3f, 10f));

    private static string Line(float x, float y, float z, float facing)
    {
        var sb = new StringBuilder();
        PositionText.AppendScreenLine(sb, PositionText.Round(x, y, z, facing));
        return sb.ToString();
    }
}
