using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

namespace KCES2.Fusebox.Tests;

/// <summary>Check for real UnityEngine.CoreModule.dll in test output.</summary>
/// <remarks>
/// <para>NuGet Unity DLLs are reference stubs without method bodies, so tests that run Unity code (Mathf, Color32, etc.) skip without real one.</para>
/// <para>Check inside method is too late: method that uses Unity types fails when it is compiled, before it runs.</para>
/// </remarks>
/// <example>
/// Use:
/// <code>
/// [Fact(Skip = UnityRuntime.Reason, SkipUnless = nameof(UnityRuntime.Available), SkipType = typeof(UnityRuntime))]
/// </code>
/// </example>
public static class UnityRuntime
{
    /// <summary>Text shown for skipped test.</summary>
    /// <remarks>Pass it as <see cref="FactAttribute.Skip"/> in attribute.</remarks>
    public const string Reason = "Needs the real UnityEngine.CoreModule.dll: set UnityManagedDir " +
                                 "(Managed folder of KCES2 or CRC3D3) to run this test.";

    /// <summary>Indicates whether build copied real Unity DLL (<c>UnityRuntime</c> in <c>Tests.props</c>).</summary>
    public static bool Available { get; } = typeof(UnityRuntime).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Any(static a => a.Key == "UnityRuntime" && a.Value == "true");

    /// <summary>Registers hook that loads copied Unity DLLs from output folder.</summary>
    /// <remarks>
    /// Copied DLLs (CoreModule and forwarding UnityEngine.dll) are not in deps.json, so .NET does not load them by itself.
    /// Hook is registered before any test method is compiled.
    /// </remarks>
    [ModuleInitializer]
    internal static void RegisterLoader()
    {
        AssemblyLoadContext.Default.Resolving += static (context, name) =>
        {
            if (name.Name != "UnityEngine.CoreModule" && name.Name != "UnityEngine") return null;

            string path = Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
    }
}
