using System.Reflection;
using AwesomeAssertions;
using BepInEx.Configuration;
using Xunit;

namespace KCES2.Fusebox.Tests;

/// <summary>Tests of <see cref="KeyHint"/> defaults and name contract with key hint readers.</summary>
/// <remarks>Readers find tag class and fields by name; rename silently breaks every hint.</remarks>
public class KeyHintTests
{
    /// <summary>Field names and types that key hint readers look for.</summary>
    private static readonly Dictionary<string, Type> _contract = new()
    {
        ["Label"] = typeof(string),
        ["Order"] = typeof(int),
        ["Context"] = typeof(string),
        ["Active"] = typeof(bool),
        ["Key"] = typeof(KeyboardShortcut?),
        ["Changed"] = typeof(Action),
    };

    [Fact]
    public void ClassName_IsWhatReadersLookFor() => nameof(KeyHint).Should().Be("KeyHint");

    [Fact]
    public void PublicFields_MatchContractNamesAndTypes() =>
        typeof(KeyHint)
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(static f => f.Name, static f => f.FieldType)
            .Should().BeEquivalentTo(_contract);

    [Fact]
    public void Defaults_ActiveWithoutKeyOrHandler()
    {
        var hint = new KeyHint();

        hint.Active.Should().BeTrue();
        hint.Key.Should().BeNull();
        hint.Changed.Should().BeNull();
        hint.Order.Should().Be(0);
    }

    [Fact]
    public void ChangedNull_InvokeIsSafe() =>
        FluentActions.Invoking(static () => new KeyHint().Changed?.Invoke()).Should().NotThrow();
}
