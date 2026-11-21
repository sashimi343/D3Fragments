using System.Collections.Generic;
using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using D3Fragments.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D3Fragments.Analyzers.Rules;

// DFR0017: DBへの書き込みとCookieの操作を同時に行う SignInManager のメソッドの呼び出し
// DBへの書き込みをCommand（Unit of Work）で、Cookieの発行をプレゼンテーション層で行えないため、分けた拡張メソッドを案内する
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SignInManagerAnalyzer : DiagnosticAnalyzer
{
    // メソッド名と、代わりに使う拡張メソッド（D3Fragments.Identity.EFCore の DB 側、D3Fragments.AspNetCore の Cookie 側）
    private static readonly Dictionary<string, string> Replacements = new()
    {
        ["PasswordSignInAsync"] = "VerifyPasswordAsync and CompleteSignInAsync / BeginTwoFactorSignInAsync",
        ["CheckPasswordSignInAsync"] = "VerifyPasswordAsync",
        ["TwoFactorSignInAsync"] = "VerifyTwoFactorCodeAsync and CompleteTwoFactorSignInAsync",
        ["TwoFactorAuthenticatorSignInAsync"] = "VerifyTwoFactorCodeAsync and CompleteTwoFactorSignInAsync",
        ["TwoFactorRecoveryCodeSignInAsync"] = "VerifyRecoveryCodeAsync and CompleteTwoFactorSignInAsync",
        ["PasskeySignInAsync"] = "PerformPasskeyAssertionAsync, VerifyPasskeyAsync and CompleteSignInAsync",
        ["ExternalLoginSignInAsync"] = "VerifyExternalLoginAsync and CompleteSignInAsync / BeginTwoFactorSignInAsync",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0017);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            if (knownTypes.SignInManager is null
                || new ArchitectureLayerResolver(knownTypes).Resolve(start.Compilation.Assembly) == ArchitectureLayerType.None)
            {
                return;
            }

            start.RegisterOperationAction(c => AnalyzeInvocation(c, knownTypes), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, KnownTypes knownTypes)
    {
        var method = ((IInvocationOperation)context.Operation).TargetMethod;
        if (!Replacements.TryGetValue(method.Name, out var replacement) || !method.ContainingType.InheritsFrom(knownTypes.SignInManager))
        {
            return;
        }

        // SignInManager の派生クラスでのオーバーライド（base の呼び出し）は、メソッドを実装する側のため対象外
        var from = context.ContainingSymbol as INamedTypeSymbol ?? context.ContainingSymbol.ContainingType;
        if (from is null || from.InheritsFrom(knownTypes.SignInManager))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DFR0017,
            context.Operation.Syntax.GetLocation(),
            from.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
            "SignInManager." + method.Name,
            replacement));
    }
}
