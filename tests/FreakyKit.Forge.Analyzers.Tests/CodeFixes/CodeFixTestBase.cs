using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreakyKit.Forge.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

public abstract class CodeFixTestBase
{
    private static readonly IReadOnlyList<MetadataReference> References = BuildReferences();

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

    protected abstract CodeFixProvider CreateCodeFixProvider();

    protected async Task VerifyCodeFixAsync(string source, string expected, string diagnosticId)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectMetadataReferences(projectId, References)
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        var document = solution.GetDocument(documentId)!;
        var compilation = (await document.Project.GetCompilationAsync())!;

        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ForgeAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();

        var diagnostic = diagnostics.FirstOrDefault(d => d.Id == diagnosticId);
        Assert.NotNull(diagnostic);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document, diagnostic!,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await CreateCodeFixProvider().RegisterCodeFixesAsync(context);
        Assert.NotEmpty(actions);

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var changedSolution = operations.OfType<ApplyChangesOperation>().First().ChangedSolution;
        var changedText = (await changedSolution.GetDocument(documentId)!.GetTextAsync()).ToString();

        Assert.Equal(expected.Trim(), changedText.Trim());
    }

    protected async Task VerifyNoCodeFixAsync(string source, string diagnosticId)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectMetadataReferences(projectId, References)
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        var document = solution.GetDocument(documentId)!;
        var compilation = (await document.Project.GetCompilationAsync())!;

        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ForgeAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();

        var diagnostic = diagnostics.FirstOrDefault(d => d.Id == diagnosticId);
        Assert.Null(diagnostic);
    }
}
