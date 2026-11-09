using System.Collections;
using System.Collections.Specialized;

namespace CRC3D3.KCES2LoaderFix;

/// <summary>Finds and removes Doorstop environment variables (<c>DOORSTOP_*</c>).</summary>
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

    /// <summary>Removes every Doorstop variable from child process environment.</summary>
    /// <returns>Names of removed variables, for log.</returns>
    internal static List<string> RemoveFrom(StringDictionary env)
    {
        var remove = new List<string>();
        foreach (string key in env.Keys)
        {
            if (key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) remove.Add(key);
        }

        foreach (string key in remove) env.Remove(key);
        return remove;
    }
}
