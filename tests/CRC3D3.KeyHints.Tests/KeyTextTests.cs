using AwesomeAssertions;
using BepInEx.Configuration;
using CRC3D3.KeyHints.Rows;
using UnityEngine;
using Xunit;

namespace CRC3D3.KeyHints.Tests;

/// <summary>Tests of <see cref="KeyText"/> short key names.</summary>
public class KeyTextTests
{
    [Theory]
    [InlineData(KeyCode.F11, "F11")]
    [InlineData(KeyCode.N, "N")]
    [InlineData(KeyCode.Space, "Space")]
    [InlineData(KeyCode.Alpha1, "1")]
    [InlineData(KeyCode.Keypad1, "Num1")]
    [InlineData(KeyCode.Escape, "Esc")]
    [InlineData(KeyCode.Return, "Enter")]
    [InlineData(KeyCode.LeftControl, "Ctrl")]
    [InlineData(KeyCode.None, "")]
    public void Of_SingleKey(KeyCode key, string expected) => KeyText.Of(new KeyboardShortcut(key)).Should().Be(expected);

    [Fact]
    public void Of_WithModifier() =>
        KeyText.Of(new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl)).Should().Be("Ctrl+F8");

    [Fact]
    public void Of_RightModifiersSameName() =>
        KeyText.Of(new KeyboardShortcut(KeyCode.F8, KeyCode.RightShift, KeyCode.RightAlt)).Should().Be("Shift+Alt+F8");
}
