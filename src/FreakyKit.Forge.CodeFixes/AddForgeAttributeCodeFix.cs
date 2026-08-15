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

/// <summary>
/// Code fix for FKF524 ([ForgeUses] without [Forge]),
/// FKF525 ([ForgeMethod] without [Forge] class),
/// and FKF526 ([ForgeConverter] without [Forge] class).
/// Fix: add [Forge] attribute to the containing class.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddForgeAttributeCodeFix)), Shared]
public sealed class AddForgeAttributeCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("FKF524", "FKF525", "FKF526");

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        // For FKF525/FKF526 the diagnostic is on the method; for FKF524 it's on the class.
        // In all cases we want the containing class.
        var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDecl is null) return;

        // Don't offer the fix if [Forge] already exists
        if (HasForgeAttribute(classDecl)) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add [Forge] attribute",
                createChangedDocument: ct => AddForgeAsync(context.Document, classDecl, ct),
                equivalenceKey: $"{diagnostic.Id}_AddForge"),
            diagnostic);
    }

    private static bool HasForgeAttribute(ClassDeclarationSyntax classDecl)
    {
        return classDecl.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a =>
            {
                var name = a.Name.ToString();
                return name == "Forge" || name == "ForgeAttribute";
            });
    }

    private static async Task<Document> AddForgeAsync(
        Document document, ClassDeclarationSyntax classDecl, CancellationToken ct)
    {
        var editor = await DocumentEditor.CreateAsync(document, ct);
        var attribute = editor.Generator.Attribute(
            editor.Generator.IdentifierName("Forge"));
        editor.AddAttribute(classDecl, attribute);
        return editor.GetChangedDocument();
    }
}
