using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace D3Fragments.Analyzers.CodeFixes;

// DFR0006: コンストラクタのアクセシビリティを private に変更する。プライマリ コンストラクター・入れ子クラスは対象外
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TypeSafeEnumConstructorCodeFixProvider)), Shared]
public sealed class TypeSafeEnumConstructorCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.DFR0006.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindToken(context.Span.Start).Parent is not ConstructorDeclarationSyntax constructor)
        {
            return;
        }

        var title = DiagnosticDescriptors.Resources.GetString("CodeFix_MakeConstructorPrivate")!;
        context.RegisterCodeFix(
            CodeAction.Create(title, _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(constructor, MakePrivate(constructor)))), title),
            context.Diagnostics);
    }

    private static ConstructorDeclarationSyntax MakePrivate(ConstructorDeclarationSyntax constructor)
    {
        // 先頭のトークンが持つインデント等のトリビアを private に移す
        var first = constructor.Modifiers.Count > 0 ? constructor.Modifiers[0] : constructor.Identifier;
        var modifiers = TokenList(
            new[] { Token(first.LeadingTrivia, SyntaxKind.PrivateKeyword, TriviaList(Space)) }
                .Concat(constructor.Modifiers.Where(m => !SyntaxFacts.IsAccessibilityModifier(m.Kind())).Select(m => m == first ? m.WithLeadingTrivia() : m)));

        return constructor
            .WithIdentifier(constructor.Modifiers.Count > 0 ? constructor.Identifier : constructor.Identifier.WithLeadingTrivia())
            .WithModifiers(modifiers);
    }
}
