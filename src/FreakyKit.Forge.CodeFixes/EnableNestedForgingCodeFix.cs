using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FreakyKit.Forge.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnableNestedForgingCodeFix)), Shared]
public sealed class EnableNestedForgingCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("FKF300");

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
                title: "Add AllowNestedForging = true",
                createChangedDocument: ct => EnableNestedForgingAsync(context.Document, methodDecl, ct),
                equivalenceKey: "FKF300_EnableNestedForging"),
            diagnostic);
    }

    private static async Task<Document> EnableNestedForgingAsync(
        Document document, MethodDeclarationSyntax methodDecl, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct);
        if (root is null) return document;

        var existingAttr = methodDecl.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a =>
            {
                var name = a.Name.ToString();
                return name == "ForgeMethod" || name == "ForgeMethodAttribute";
            });

        SyntaxNode newRoot;
        if (existingAttr != null)
        {
            var newArg = SyntaxFactory.AttributeArgument(
                SyntaxFactory.NameEquals("AllowNestedForging"),
                null,
                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));

            var newAttr = existingAttr.ArgumentList is null
                ? existingAttr.WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(newArg)))
                : existingAttr.WithArgumentList(
                    existingAttr.ArgumentList.AddArguments(newArg));

            newRoot = root.ReplaceNode(existingAttr, newAttr);
        }
        else
        {
            var attr = SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("ForgeMethod"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.NameEquals("AllowNestedForging"),
                            null,
                            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))));

            var attrList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(attr))
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

            var newMethodDecl = methodDecl.AddAttributeLists(attrList);
            newRoot = root.ReplaceNode(methodDecl, newMethodDecl);
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
