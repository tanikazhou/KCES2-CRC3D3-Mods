using System.Collections;
using AwesomeAssertions;
using Xunit;

namespace CRC3D3.KCES2LoaderFix.Tests;

/// <summary>Tests for detection of Doorstop variables in CRC3D3 environment.</summary>
/// <remarks>
/// If one stays (<c>DOORSTOP_INITIALIZED</c> or <c>DOORSTOP_DISABLE</c>), KCES2 started by CRC3D3 loads neither BepInEx nor translation.
/// Other variables with similar names must stay.
/// </remarks>
public class DoorstopEnvTests
{
    [Fact]
    public void Names_MixedCase_FindsOnlyPrefix() =>
        DoorstopEnv.Names(Vars()).Should().BeEquivalentTo("DOORSTOP_", "DOORSTOP_INITIALIZED", "Doorstop_Process_Path", "doorstop_invoke_dll_path");

    [Fact]
    public void Names_Empty_ReturnsEmpty() => DoorstopEnv.Names(new Hashtable()).Should().BeEmpty();

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
