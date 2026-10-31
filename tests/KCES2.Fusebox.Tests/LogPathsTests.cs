using AwesomeAssertions;
using Xunit;

namespace KCES2.Fusebox.Tests;

/// <summary>Tests of log privacy guard.</summary>
/// <remarks>Absolute path of program folder must not reach log (could hold username).</remarks>
public class LogPathsTests
{
    private const string Root = "/home/player/Games/KCES2";

    [Theory]
    [InlineData(Root + "/BepInEx/plugins/KCES2.EditAssist/data", "BepInEx/plugins/KCES2.EditAssist/data")]
    [InlineData(Root + "/x.ks", "x.ks")]
    [InlineData(Root, "")]
    [InlineData(Root + "/", "")]
    // Folder compare ignores case (Windows paths).
    [InlineData("/HOME/PLAYER/games/kces2/x.ks", "x.ks")]
    // Outside folder: file name only.
    [InlineData("/home/player/other/secret.ks", "secret.ks")]
    [InlineData("/tmp/a.txt", "a.txt")]
    public void Rel(string path, string expected) => LogPaths.Rel(path, Root).Should().Be(expected);

    [Fact]
    public void Rel_WindowsRoot() =>
        LogPaths.Rel(@"S:\Users\player\KCES2\BepInEx\plugins\x.ks", @"S:\Users\player\KCES2")
            .Should().Be(@"BepInEx\plugins\x.ks");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Rel_WithoutRoot_GivesTheFileName(string root) =>
        LogPaths.Rel("/home/player/x.ks", root).Should().Be("x.ks");

    [Fact]
    public void Rel_SiblingFolderWithTheSamePrefix() => LogPaths.Rel(Root + "Saves/x.ks", Root).Should().Be("x.ks");

    [Theory]
    [InlineData("Could not find file '" + Root + "/x.ks'", "Could not find file './x.ks'")]
    [InlineData(Root + " and " + Root, ". and .")]
    [InlineData("no path here", "no path here")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void Scrub(string text, string expected) => LogPaths.Scrub(text, Root).Should().Be(expected);

    [Fact]
    public void Scrub_BothSeparators()
    {
        const string win = @"S:\Users\player\KCES2";
        LogPaths.Scrub(@"S:\Users\player\KCES2\a.ks, S:/Users/player/KCES2/b.ks", win).Should()
            .Be(".\\a.ks, ./b.ks");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Scrub_WithoutRoot_TextStays(string root) =>
        LogPaths.Scrub("a " + Root, root).Should().Be("a " + Root);

    [Fact]
    public void Scrub_OtherCase() =>
        LogPaths.Scrub(@"S:\USERS\PLAYER\KCES2\a.ks", @"S:\Users\player\KCES2").Should().NotContain("player");
}
