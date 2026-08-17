using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreakyKit.Forge.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

/// <summary>
/// Base class for code fix tests where the diagnostic comes from the source generator
/// (FKF524, FKF525, FKF526) rather than the analyzer.
/// </summary>
public abstract class GeneratorCodeFixTestBase
{
    private static readonly IReadOnlyList<MetadataReference> References = SharedTestReferences.References;

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

        // Run the source generator to get generator-emitted diagnostics
        var generator = new ForgeGenerator();
        var driver = CSharpGeneratorDriver
            .Create(new ISourceGenerator[] { generator.AsSourceGenerator() })
            .RunGeneratorsAndUpdateCompilation(
                (CSharpCompilation)compilation, out _, out var generatorDiagnostics);

        var diagnostic = generatorDiagnostics.FirstOrDefault(d => d.Id == diagnosticId);
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
}
