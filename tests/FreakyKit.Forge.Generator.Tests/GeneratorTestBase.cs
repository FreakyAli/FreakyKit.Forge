using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FreakyKit.Forge.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Base class for source generator tests.
/// Uses direct CSharpGeneratorDriver to keep assertions precise and fast.
/// </summary>
public abstract class GeneratorTestBase
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

        // FreakyKit.Forge attributes
        refs.Add(MetadataReference.CreateFromFile(typeof(ForgeAttribute).Assembly.Location));

        return refs;
    }

    protected static ForgeRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: BaseReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ForgeGenerator();
        var driver = CSharpGeneratorDriver
            .Create(new ISourceGenerator[] { generator.AsSourceGenerator() })
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var compilationDiagnostics = outputCompilation.GetDiagnostics();
        return new ForgeRunResult(outputCompilation, runResult, diagnostics, compilationDiagnostics);
    }

    protected static void AssertNoErrors(ForgeRunResult result)
    {
        var errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }

    protected static void AssertHasError(ForgeRunResult result, string diagnosticId)
    {
        Assert.Contains(result.Diagnostics, d =>
            d.Severity == DiagnosticSeverity.Error &&
            d.Id == diagnosticId);
    }

    protected static void AssertNoGeneratedSource(ForgeRunResult result)
    {
        var generated = result.RunResult.GeneratedTrees;
        Assert.Empty(generated);
    }

    protected static string AssertSingleGeneratedFile(ForgeRunResult result)
    {
        var generated = result.RunResult.GeneratedTrees;
        Assert.Single(generated);
        return generated[0].GetText().ToString();
    }

    protected static string AssertGeneratedFiles(ForgeRunResult result, int count)
    {
        var generated = result.RunResult.GeneratedTrees;
        Assert.Equal(count, generated.Length);
        return generated.Select(t => t.GetText().ToString()).Aggregate((a, b) => a + "\n" + b);
    }

    protected sealed class ForgeRunResult
    {
        public Compilation OutputCompilation { get; }
        public GeneratorDriverRunResult RunResult { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<Diagnostic> CompilationDiagnostics { get; }

        public bool HasCompilationErrors =>
            CompilationDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

        public ForgeRunResult(
            Compilation compilation,
            GeneratorDriverRunResult runResult,
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<Diagnostic> compilationDiagnostics)
        {
            OutputCompilation = compilation;
            RunResult = runResult;
            Diagnostics = diagnostics;
            CompilationDiagnostics = compilationDiagnostics;
        }
    }
}
