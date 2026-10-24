using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenMultiSeat.Tests;

/// <summary>
/// Every [DllImport] in the product must name an export that actually exists in its DLL.
///
/// Added after live testing found five WTS imports declared as "WtsEnumerateSessions",
/// "WtsFreeMemory", "WtsQuerySessionInformation", "WtsQueryUserToken" and
/// "WtsGetActiveConsoleSessionId" with no EntryPoint. Export lookup is case-sensitive and the real
/// names are "WTS...", and WTSGetActiveConsoleSessionId lives in kernel32, not wtsapi32. Each call
/// threw EntryPointNotFoundException at runtime, which callers caught and turned into "no
/// sessions", so session enumeration had silently never worked. The build can't catch this; only
/// resolving the export can. This test does that for every import, the same way the runtime does.
/// </summary>
[TestClass]
public class NativeInteropEntryPointTests
{
    private static readonly Assembly[] ProductAssemblies =
    [
        typeof(OpenMultiSeat.Sessions.SessionEnumerator).Assembly,
        typeof(OpenMultiSeat.Devices.HidDeviceEnumerator).Assembly,
        typeof(OpenMultiSeat.Displays.DisplayEnumerator).Assembly,
        typeof(OpenMultiSeat.GUI.App).Assembly,
    ];

    [TestMethod]
    public void EveryDllImport_ResolvesToARealExport()
    {
        var checkedCount = 0;
        var unresolved = new List<string>();

        foreach (var method in AllPInvokeMethods())
        {
            var import = method.GetCustomAttribute<DllImportAttribute>()!;
            checkedCount++;

            if (!NativeLibrary.TryLoad(import.Value, out var library))
            {
                unresolved.Add($"{Describe(method)}: couldn't load \"{import.Value}\"");
                continue;
            }

            var candidates = CandidateExportNames(import, method.Name);
            if (!candidates.Any(name => NativeLibrary.TryGetExport(library, name, out _)))
                unresolved.Add($"{Describe(method)}: \"{import.Value}\" exports none of [{string.Join(", ", candidates)}]");
        }

        Assert.IsTrue(checkedCount > 0, "Found no [DllImport] methods at all; the reflection scan itself is broken.");
        Assert.AreEqual(0, unresolved.Count,
            $"{unresolved.Count} of {checkedCount} P/Invoke declaration(s) name an export that doesn't exist:\n" +
            string.Join("\n", unresolved));
    }

    /// <summary>Mirrors the runtime's own lookup: the declared name (EntryPoint, else the method
    /// name) and, unless ExactSpelling, that name with the CharSet's A/W suffix.</summary>
    private static string[] CandidateExportNames(DllImportAttribute import, string methodName)
    {
        var name = import.EntryPoint ?? methodName;
        if (import.ExactSpelling)
            return [name];

        return import.CharSet is CharSet.Unicode or CharSet.Auto
            ? [name + "W", name]
            : [name, name + "A"];
    }

    private static IEnumerable<MethodInfo> AllPInvokeMethods() =>
        ProductAssemblies
            .SelectMany(LoadableTypes)
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<DllImportAttribute>() != null);

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }

    private static string Describe(MethodInfo method) => $"{method.DeclaringType?.FullName}.{method.Name}";
}
