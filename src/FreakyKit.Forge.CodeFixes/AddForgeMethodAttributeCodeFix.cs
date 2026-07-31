using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace FreakyKit.Forge.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddForgeMethodAttributeCodeFix)), Shared]
public sealed class AddForgeMethodAttributeCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("FKF002");

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var methodDecl = root.FindNode(diagnostic.Location.SourceSpan)
            .FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDecl is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add [ForgeMethod] attribute",
                createChangedDocument: ct => AddForgeMethodAsync(context.Document, methodDecl, ct),
                equivalenceKey: "FKF002_AddForgeMethod"),
            diagnostic);
    }

    private static async Task<Document> AddForgeMethodAsync(
        Document document, MethodDeclarationSyntax methodDecl, CancellationToken ct)
    {
        var editor = await DocumentEditor.CreateAsync(document, ct);
        var attribute = editor.Generator.Attribute(
            editor.Generator.IdentifierName("ForgeMethod"));
        editor.AddAttribute(methodDecl, attribute);
        return editor.GetChangedDocument();
    }
}
