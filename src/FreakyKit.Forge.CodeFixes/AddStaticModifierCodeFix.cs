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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddStaticModifierCodeFix)), Shared]
public sealed class AddStaticModifierCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("FKF003");

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var classDecl = root.FindNode(diagnostic.Location.SourceSpan)
            .FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDecl is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add 'static' modifier",
                createChangedDocument: ct => AddStaticAsync(context.Document, classDecl, ct),
                equivalenceKey: "FKF003_AddStatic"),
            diagnostic);
    }

    private static async Task<Document> AddStaticAsync(
        Document document, ClassDeclarationSyntax classDecl, CancellationToken ct)
    {
        var editor = await DocumentEditor.CreateAsync(document, ct);
        var modifiers = editor.Generator.GetModifiers(classDecl);
        editor.SetModifiers(classDecl, modifiers | DeclarationModifiers.Static);
        return editor.GetChangedDocument();
    }
}
