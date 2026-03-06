using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using FreakyKit.Forge.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Base class providing helpers for analyzer verification.
/// Uses direct compilation + WithAnalyzers() — no external testing SDK required.
/// </summary>
public abstract class AnalyzerTestBase
{
    private static readonly IReadOnlyList<MetadataReference> BaseReferences = BuildBaseReferences();

    private static IReadOnlyList<MetadataReference> BuildBaseReferences()
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

    protected static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: BaseReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new ForgeAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None).Result;
    }

    protected static void AssertNoDiagnostics(string source)
    {
        var diagnostics = GetAnalyzerDiagnostics(source).Where(d => d.Id.StartsWith("FKF")).ToList();
        Assert.Empty(diagnostics);
    }

    protected static void AssertDiagnosticIds(string source, params string[] expectedIds)
    {
        var actualIds = GetAnalyzerDiagnostics(source)
            .Where(d => d.Id.StartsWith("FKF"))
            .Select(d => d.Id)
            .OrderBy(x => x)
            .ToList();
        var sorted = expectedIds.OrderBy(x => x).ToList();
        Assert.Equal(sorted, actualIds);
    }

    protected static void AssertContainsDiagnostic(string source, string id) =>
        Assert.Contains(GetAnalyzerDiagnostics(source), d => d.Id == id);

    protected static void AssertNotContainsDiagnostic(string source, string id) =>
        Assert.DoesNotContain(GetAnalyzerDiagnostics(source), d => d.Id == id);

    protected static void AssertDiagnosticSeverity(string source, string id, DiagnosticSeverity expected)
    {
        var diag = GetAnalyzerDiagnostics(source).FirstOrDefault(d => d.Id == id);
        Assert.NotNull(diag);
        Assert.Equal(expected, diag!.Severity);
    }

    // Synchronous wrappers matching the old API names to avoid changing all test files
    protected static void VerifyNoDiagnostics(string source) => AssertNoDiagnostics(source);
    protected static void VerifyDiagnostic(string source, string expectedId) => AssertContainsDiagnostic(source, expectedId);
}
