using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FreakyKit.Forge.CodeFixes;

/// <summary>
/// Code fix for FKF109 (member has both [ForgeIgnore] and [ForgeMap] — ignore wins)
/// and FKF112 (self-referencing [ForgeMap] — maps to own name, no-op).
/// Fix: remove the [ForgeMap] attribute.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveForgeMapCodeFix)), Shared]
public sealed class RemoveForgeMapCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("FKF109", "FKF112");

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        // The diagnostic is reported on the member (property or field).
        // We need to find the [ForgeMap] attribute on it.
        var memberDecl = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDecl is null) return;

        var forgeMapAttr = FindForgeMapAttribute(memberDecl);
        if (forgeMapAttr is null) return;

        var title = diagnostic.Id == "FKF109"
            ? "Remove [ForgeMap] (keep [ForgeIgnore])"
            : "Remove [ForgeMap] (self-referencing)";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: ct => RemoveForgeMapAsync(context.Document, forgeMapAttr, ct),
                equivalenceKey: $"{diagnostic.Id}_RemoveForgeMap"),
            diagnostic);
    }

    private static AttributeSyntax FindForgeMapAttribute(MemberDeclarationSyntax memberDecl)
    {
        return memberDecl.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a =>
            {
                var name = a.Name.ToString();
                return name == "ForgeMap" || name == "ForgeMapAttribute";
            });
    }

    private static async Task<Document> RemoveForgeMapAsync(
        Document document, AttributeSyntax attribute, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct);
        if (root is null) return document;

        var attributeList = attribute.FirstAncestorOrSelf<AttributeListSyntax>();
        if (attributeList is null) return document;

        SyntaxNode newRoot;
        if (attributeList.Attributes.Count == 1)
        {
            // Only attribute in the list — remove the entire [ForgeMap] list
            newRoot = root.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia);
        }
        else
        {
            // Multiple attributes in the list — remove just [ForgeMap]
            var newList = attributeList.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
            newRoot = root.ReplaceNode(attributeList, newList);
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
