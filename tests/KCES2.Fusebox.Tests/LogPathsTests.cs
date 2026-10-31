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
    public void Rel(string path, string expected) => Assert.Equal(expected, LogPaths.Rel(path, Root));

    [Fact]
    public void Rel_WindowsRoot() => Assert.Equal("BepInEx\\plugins\\x.ks",
        LogPaths.Rel("S:\\Users\\player\\KCES2\\BepInEx\\plugins\\x.ks", "S:\\Users\\player\\KCES2"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Rel_WithoutRoot_GivesTheFileName(string root) =>
        Assert.Equal("x.ks", LogPaths.Rel("/home/player/x.ks", root));

    [Fact]
    public void Rel_SiblingFolderWithTheSamePrefix() => Assert.Equal("x.ks", LogPaths.Rel(Root + "Saves/x.ks", Root));

    [Theory]
    [InlineData("Could not find file '" + Root + "/x.ks'", "Could not find file './x.ks'")]
    [InlineData(Root + " and " + Root, ". and .")]
    [InlineData("no path here", "no path here")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void Scrub(string text, string expected) => Assert.Equal(expected, LogPaths.Scrub(text, Root));

    [Fact]
    public void Scrub_BothSeparators()
    {
        const string win = "S:\\Users\\player\\KCES2";
        Assert.Equal(".\\a.ks, ./b.ks",
            LogPaths.Scrub("S:\\Users\\player\\KCES2\\a.ks, S:/Users/player/KCES2/b.ks", win));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Scrub_WithoutRoot_TextStays(string root) =>
        Assert.Equal("a " + Root, LogPaths.Scrub("a " + Root, root));

    [Fact]
    public void Scrub_OtherCase() =>
        Assert.DoesNotContain("player", LogPaths.Scrub("S:\\USERS\\PLAYER\\KCES2\\a.ks", "S:\\Users\\player\\KCES2"));
}
