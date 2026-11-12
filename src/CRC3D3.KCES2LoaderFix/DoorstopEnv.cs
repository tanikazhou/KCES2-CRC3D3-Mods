using System.Collections;

namespace CRC3D3.KCES2LoaderFix;

/// <summary>Finds Doorstop environment variables (<c>DOORSTOP_*</c>).</summary>
/// <remarks>
/// Names compare without case, as Windows environment variables do.
/// Pure code without CRC3D3 types: unit tests link this file.
/// </remarks>
/// <seealso cref="KcesModsFix"/>
internal static class DoorstopEnv
{
    private const string Prefix = "DOORSTOP_";

    /// <summary>Finds Doorstop variable names in environment (<see cref="Environment.GetEnvironmentVariables()"/>).</summary>
    internal static List<string> Names(IDictionary vars)
    {
        var found = new List<string>();
        foreach (DictionaryEntry e in vars)
        {
            if (e.Key is string name && name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) found.Add(name);
        }

        return found;
    }
}
