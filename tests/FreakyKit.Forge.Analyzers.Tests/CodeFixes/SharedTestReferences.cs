using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

/// <summary>
/// Shared metadata references and workspace setup for code fix tests.
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

    /// <summary>
    /// Creates a test document and returns the workspace, document, and document ID.
    /// Caller is responsible for disposing the workspace.
    /// </summary>
    internal static (AdhocWorkspace Workspace, Document Document, DocumentId DocumentId) CreateTestDocument(string source)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectMetadataReferences(projectId, References)
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        var document = solution.GetDocument(documentId)!;
        return (workspace, document, documentId);
    }
}
