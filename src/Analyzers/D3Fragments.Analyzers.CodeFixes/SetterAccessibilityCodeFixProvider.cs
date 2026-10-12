using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace D3Fragments.Analyzers.CodeFixes;

// DFR0003 / DFR0010: プロパティのセッター（set / init）を private set に変更する。フィールドは対象外
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SetterAccessibilityCodeFixProvider)), Shared]
public sealed class SetterAccessibilityCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0003.Id, DiagnosticDescriptors.DFR0010.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindToken(context.Span.Start).Parent is not PropertyDeclarationSyntax property
            || property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration)) is not { } accessor)
        {
            return;
        }

        var title = DiagnosticDescriptors.Resources.GetString("CodeFix_MakeSetterPrivate")!;
        context.RegisterCodeFix(
            CodeAction.Create(title, _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(accessor, MakePrivate(accessor)))), title),
            context.Diagnostics);
    }

    private static AccessorDeclarationSyntax MakePrivate(AccessorDeclarationSyntax accessor)
    {
        var first = accessor.Modifiers.Count > 0 ? accessor.Modifiers[0] : accessor.Keyword;
        return AccessorDeclaration(
            SyntaxKind.SetAccessorDeclaration,
            accessor.AttributeLists,
            TokenList(Token(first.LeadingTrivia, SyntaxKind.PrivateKeyword, TriviaList(Space))),
            Token(TriviaList(), SyntaxKind.SetKeyword, accessor.Keyword.TrailingTrivia),
            accessor.Body,
            accessor.ExpressionBody,
            accessor.SemicolonToken);
    }
}
