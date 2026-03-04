using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FreakyKit.Forge.Analyzers;
using FreakyKit.Forge.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FreakyKit.Forge.Integration.Tests;

/// <summary>
/// Base for integration tests — runs both the generator and the analyzer
/// against the same compilation, mirroring what happens in a real project build.
/// </summary>
public abstract class IntegrationTestBase
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

        refs.Add(MetadataReference.CreateFromFile(typeof(ForgeClassAttribute).Assembly.Location));

        return refs;
    }

    protected static IntegrationResult RunFull(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var initialCompilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: BaseReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Step 1: Run generator
        var generator = new ForgeGenerator();
        var driver = CSharpGeneratorDriver
            .Create(new ISourceGenerator[] { generator.AsSourceGenerator() })
            .RunGeneratorsAndUpdateCompilation(
                initialCompilation,
                out var generatedCompilation,
                out var generatorDiagnostics);

        var runResult = driver.GetRunResult();

        // Step 2: Run analyzer on the (potentially augmented) compilation
        var analyzerDiagnostics = RunAnalyzer(generatedCompilation);

        return new IntegrationResult(
            generatedCompilation,
            runResult,
            generatorDiagnostics,
            analyzerDiagnostics);
    }

    private static ImmutableArray<Diagnostic> RunAnalyzer(Compilation compilation)
    {
        var analyzer = new ForgeAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
    }

    protected sealed class IntegrationResult
    {
        public Compilation OutputCompilation { get; }
        public GeneratorDriverRunResult RunResult { get; }
        public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }
        public ImmutableArray<Diagnostic> AnalyzerDiagnostics { get; }

        public IEnumerable<Diagnostic> AllDiagnostics =>
            GeneratorDiagnostics.Concat(AnalyzerDiagnostics);

        public bool HasErrors => AllDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        public bool HasGeneratedSource => RunResult.GeneratedTrees.Length > 0;

        public IntegrationResult(
            Compilation outputCompilation,
            GeneratorDriverRunResult runResult,
            ImmutableArray<Diagnostic> generatorDiagnostics,
            ImmutableArray<Diagnostic> analyzerDiagnostics)
        {
            OutputCompilation = outputCompilation;
            RunResult = runResult;
            GeneratorDiagnostics = generatorDiagnostics;
            AnalyzerDiagnostics = analyzerDiagnostics;
        }
    }
}
