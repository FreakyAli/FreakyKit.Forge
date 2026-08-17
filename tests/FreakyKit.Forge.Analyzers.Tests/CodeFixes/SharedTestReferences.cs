using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

/// <summary>
/// Shared metadata references for code fix tests.
/// Used by both CodeFixTestBase (analyzer-driven diagnostics) and
/// GeneratorCodeFixTestBase (generator-driven diagnostics).
/// </summary>
internal static class SharedTestReferences
{
    internal static readonly IReadOnlyList<MetadataReference> References = BuildReferences();

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        var refs = new List<MetadataReference>();
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var runtimeDll = Path.Combine(runtimePath, "System.Runtime.dll");
        if (File.Exists(runtimeDll))
            refs.Add(MetadataReference.CreateFromFile(runtimeDll));

        var netstandard = Path.Combine(runtimePath, "netstandard.dll");
        if (File.Exists(netstandard))
            refs.Add(MetadataReference.CreateFromFile(netstandard));

        refs.Add(MetadataReference.CreateFromFile(typeof(ForgeAttribute).Assembly.Location));

        return refs;
    }
}
