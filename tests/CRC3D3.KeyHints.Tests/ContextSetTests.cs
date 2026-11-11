using AwesomeAssertions;
using CRC3D3.KeyHints.Rows;
using Xunit;

namespace CRC3D3.KeyHints.Tests;

/// <summary>Tests of <see cref="ContextSet"/> state and context checks.</summary>
public class ContextSetTests
{
    private readonly ContextSet _set = new();

    [Fact]
    public void Known_FieldAndDance() => ContextSet.Known.Should().Equal("Field", "Dance");

    [Fact]
    public void New_AllOff()
    {
        _set.IsOn("Field").Should().BeFalse();
        _set.IsOn("Dance").Should().BeFalse();
    }

    [Fact]
    public void Set_ReturnsChangedOnlyOnChange()
    {
        _set.Set("Field", true).Should().BeTrue();
        _set.Set("Field", true).Should().BeFalse();
        _set.IsOn("Field").Should().BeTrue();
        _set.Set("Field", false).Should().BeTrue();
        _set.IsOn("Field").Should().BeFalse();
    }

    [Fact]
    public void EmptyContexts_Allowed() => _set.AllowsAny([]).Should().BeTrue();

    [Fact]
    public void AllowsAny_OneOfListOn()
    {
        _set.Set("Dance", true);

        _set.AllowsAny(["Field", "Dance"]).Should().BeTrue();
        _set.AllowsAny(["Field"]).Should().BeFalse();
    }

    [Fact]
    public void UnknownName_NeverOn()
    {
        _set.Set("Shop", true).Should().BeFalse();

        _set.IsOn("Shop").Should().BeFalse();
        _set.AllowsAny(["Shop"]).Should().BeFalse();
        ContextSet.IsKnown("Shop").Should().BeFalse();
        ContextSet.IsKnown("Field").Should().BeTrue();
    }

    [Fact]
    public void Names_AreCaseSensitive() => ContextSet.IsKnown("field").Should().BeFalse();
}
