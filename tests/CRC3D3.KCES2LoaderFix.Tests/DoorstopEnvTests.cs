using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using AwesomeAssertions;
using Xunit;

namespace CRC3D3.KCES2LoaderFix.Tests;

/// <summary>Tests for removal of Doorstop variables from KCES2 start environment.</summary>
/// <remarks>
/// If one stays (<c>DOORSTOP_INITIALIZED</c> above all), KCES2 started by CRC3D3 loads neither BepInEx nor translation.
/// Other variables with similar names (and <c>WINEDLLOVERRIDES</c>) must stay.
/// </remarks>
public class DoorstopEnvTests
{
    [Fact]
    public void Names_MixedCase_FindsOnlyPrefix() =>
        DoorstopEnv.Names(Vars()).Should().BeEquivalentTo("DOORSTOP_", "DOORSTOP_INITIALIZED", "Doorstop_Process_Path", "doorstop_invoke_dll_path");

    [Fact]
    public void Names_Empty_ReturnsEmpty() => DoorstopEnv.Names(new Hashtable()).Should().BeEmpty();

    [Fact]
    public void RemoveFrom_StringDictionary_KeepsOtherVariables()
    {
        var env = new StringDictionary();
        foreach (DictionaryEntry e in Vars())
        {
            if (e.Key is string k) env[k] = (string)e.Value;
        }

        List<string> removed = DoorstopEnv.RemoveFrom(env);

        removed.Should().HaveCount(4);
        env.Keys.Cast<string>().Should().NotContain(key => key.StartsWith("DOORSTOP_", StringComparison.OrdinalIgnoreCase));
        env["WINEDLLOVERRIDES"].Should().Be("winhttp=n,b");
        env["DOORSTOP"].Should().Be("keep");
        env["DOORSTOPX"].Should().Be("keep");
        env["XDOORSTOP_A"].Should().Be("keep");
        env["PATH"].Should().Be("/bin");
    }

    [Fact]
    public void RemoveFrom_NothingToRemove_ReturnsEmpty()
    {
        var env = new StringDictionary { ["PATH"] = "/bin" };

        DoorstopEnv.RemoveFrom(env).Should().BeEmpty();
        env.Count.Should().Be(1);
    }

    [Fact]
    public void RemoveFrom_ProcessStartInfo_RemovesInheritedAndAdded()
    {
        // Same type as in KcesModsFix: ProcessStartInfo.EnvironmentVariables (copy of this process environment).
        const string mark = "DOORSTOP_U3_TEST_MARK";
        Environment.SetEnvironmentVariable(mark, "1");

        try
        {
            DoorstopEnv.Names(Environment.GetEnvironmentVariables()).Should().Contain(mark);

            var si = new ProcessStartInfo("x") { UseShellExecute = false };
            si.EnvironmentVariables["doorstop_lower_case"] = "";
            si.EnvironmentVariables["WINEDLLOVERRIDES"] = "winhttp=n,b";
            List<string> removed = DoorstopEnv.RemoveFrom(si.EnvironmentVariables);

            removed.Should().Contain(mark).And.Contain("doorstop_lower_case");
            si.EnvironmentVariables.Keys.Cast<string>().Should().NotContain(key => key.StartsWith("DOORSTOP_", StringComparison.OrdinalIgnoreCase));
            si.EnvironmentVariables.ContainsKey("WINEDLLOVERRIDES").Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(mark, null);
        }
    }

    private static Hashtable Vars() => new()
    {
        ["DOORSTOP_INITIALIZED"] = "TRUE",
        ["doorstop_invoke_dll_path"] = "x",
        ["Doorstop_Process_Path"] = "",
        ["DOORSTOP_"] = "y",
        ["DOORSTOP"] = "keep",
        ["DOORSTOPX"] = "keep",
        ["XDOORSTOP_A"] = "keep",
        ["WINEDLLOVERRIDES"] = "winhttp=n,b",
        ["PATH"] = "/bin",
        [42] = "not a string key",
    };
}
