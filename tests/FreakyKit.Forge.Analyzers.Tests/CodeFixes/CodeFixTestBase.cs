using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreakyKit.Forge.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

public abstract class CodeFixTestBase
{
    protected abstract CodeFixProvider CreateCodeFixProvider();

    protected async Task VerifyCodeFixAsync(string source, string expected, string diagnosticId)
    {
        var (workspace, document, documentId) = SharedTestReferences.CreateTestDocument(source);
        using (workspace)
        {
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
    }

    protected async Task VerifyNoCodeFixAsync(string source, string diagnosticId)
    {
        var (workspace, document, _) = SharedTestReferences.CreateTestDocument(source);
        using (workspace)
        {
            var compilation = (await document.Project.GetCompilationAsync())!;

            var diagnostics = await compilation
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ForgeAnalyzer()))
                .GetAnalyzerDiagnosticsAsync();

            var diagnostic = diagnostics.FirstOrDefault(d => d.Id == diagnosticId);
            Assert.Null(diagnostic);
        }
    }
}
