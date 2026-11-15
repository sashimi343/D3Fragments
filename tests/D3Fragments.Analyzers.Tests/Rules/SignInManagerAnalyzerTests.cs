using D3Fragments.Analyzers.Tests.Helpers;
using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.SignInManagerAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class SignInManagerAnalyzerTests
{
    private const ArchitectureLayerType All =
        ArchitectureLayerType.Presentation | ArchitectureLayerType.Application | ArchitectureLayerType.Domain | ArchitectureLayerType.Infrastructure;

    private const string Usings = """
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Identity;

        """;

    private static Microsoft.CodeAnalysis.Testing.DiagnosticResult Expect(int location, string from, string method, string replacement) =>
        Verifier.Diagnostic(DiagnosticDescriptors.DFR0017).WithLocation(location).WithArguments(from, "SignInManager." + method, replacement);

    [Fact]
    public async Task CookieとDBへの書き込みを同時に行うメソッドの呼び出し_ReportsDfr0017()
    {
        const string source = Usings + """
            public sealed class LoginController(SignInManager<IdentityUser> signInManager)
            {
                public async Task LoginAsync(IdentityUser user)
                {
                    await {|#0:signInManager.PasswordSignInAsync(user, "password", false, true)|};
                    await {|#1:signInManager.PasswordSignInAsync("name", "password", false, true)|};
                    await {|#2:signInManager.CheckPasswordSignInAsync(user, "password", true)|};
                    await {|#3:signInManager.TwoFactorSignInAsync("Email", "code", false, false)|};
                    await {|#4:signInManager.TwoFactorAuthenticatorSignInAsync("code", false, false)|};
                    await {|#5:signInManager.TwoFactorRecoveryCodeSignInAsync("code")|};
                    await {|#6:signInManager.PasskeySignInAsync("{}")|};
                    await {|#7:signInManager.ExternalLoginSignInAsync("provider", "key", false)|};
                    await {|#8:signInManager.ExternalLoginSignInAsync("provider", "key", false, true)|};
                }
            }
            """;

        await Verifier.VerifyAsync(source, All,
            Expect(0, "LoginController", "PasswordSignInAsync", "VerifyPasswordAsync and CompleteSignInAsync / BeginTwoFactorSignInAsync"),
            Expect(1, "LoginController", "PasswordSignInAsync", "VerifyPasswordAsync and CompleteSignInAsync / BeginTwoFactorSignInAsync"),
            Expect(2, "LoginController", "CheckPasswordSignInAsync", "VerifyPasswordAsync"),
            Expect(3, "LoginController", "TwoFactorSignInAsync", "VerifyTwoFactorCodeAsync and CompleteTwoFactorSignInAsync"),
            Expect(4, "LoginController", "TwoFactorAuthenticatorSignInAsync", "VerifyTwoFactorCodeAsync and CompleteTwoFactorSignInAsync"),
            Expect(5, "LoginController", "TwoFactorRecoveryCodeSignInAsync", "VerifyRecoveryCodeAsync and CompleteTwoFactorSignInAsync"),
            Expect(6, "LoginController", "PasskeySignInAsync", "PerformPasskeyAssertionAsync, VerifyPasskeyAsync and CompleteSignInAsync"),
            Expect(7, "LoginController", "ExternalLoginSignInAsync", "VerifyExternalLoginAsync and CompleteSignInAsync / BeginTwoFactorSignInAsync"),
            Expect(8, "LoginController", "ExternalLoginSignInAsync", "VerifyExternalLoginAsync and CompleteSignInAsync / BeginTwoFactorSignInAsync"));
    }

    [Fact]
    public async Task SignInManagerの派生クラスのインスタンスでの呼び出し_ReportsDfr0017()
    {
        const string source = Usings + """
            public sealed class AppSignInManager : SignInManager<IdentityUser>
            {
                public AppSignInManager() : base(null!, null!, null!, null!, null!, null!, null!)
                {
                }
            }

            public sealed class LoginHandler(AppSignInManager signInManager)
            {
                public Task LoginAsync(IdentityUser user) => {|#0:signInManager.CheckPasswordSignInAsync(user, "password", true)|};
            }
            """;

        await Verifier.VerifyAsync(source, All, Expect(0, "LoginHandler", "CheckPasswordSignInAsync", "VerifyPasswordAsync"));
    }

    [Fact]
    public async Task Cookieだけ_またはDBへの書き込みだけを行うメソッドの呼び出し_NoDiagnostic()
    {
        const string source = Usings + """
            public sealed class LoginController(SignInManager<IdentityUser> signInManager)
            {
                public async Task LoginAsync(IdentityUser user)
                {
                    await signInManager.SignInAsync(user, false);
                    await signInManager.SignInWithClaimsAsync(user, false, []);
                    await signInManager.RefreshSignInAsync(user);
                    await signInManager.SignOutAsync();
                    await signInManager.RememberTwoFactorClientAsync(user);
                    await signInManager.GetTwoFactorAuthenticationUserAsync();
                    await signInManager.PerformPasskeyAssertionAsync("{}");
                    await signInManager.IsTwoFactorEnabledAsync(user);
                }
            }
            """;

        await Verifier.VerifyAsync(source, All);
    }

    [Fact]
    public async Task SignInManagerの派生クラスでのオーバーライドからの呼び出し_NoDiagnostic()
    {
        const string source = Usings + """
            public sealed class AppSignInManager : SignInManager<IdentityUser>
            {
                public AppSignInManager() : base(null!, null!, null!, null!, null!, null!, null!)
                {
                }

                public override Task<SignInResult> PasswordSignInAsync(IdentityUser user, string password, bool isPersistent, bool lockoutOnFailure) =>
                    base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
            }
            """;

        await Verifier.VerifyAsync(source, All);
    }

    [Fact]
    public async Task 同名のメソッドでもSignInManagerのものでなければ_NoDiagnostic()
    {
        const string source = Usings + """
            public sealed class FakeSignIn
            {
                public Task PasswordSignInAsync() => Task.CompletedTask;
            }

            public sealed class LoginController(FakeSignIn signIn)
            {
                public Task LoginAsync() => signIn.PasswordSignInAsync();
            }
            """;

        await Verifier.VerifyAsync(source, All);
    }

    [Fact]
    public async Task ArchitectureLayerが設定されていないアセンブリ_NoDiagnostic()
    {
        const string source = Usings + """
            public sealed class LoginController(SignInManager<IdentityUser> signInManager)
            {
                public Task LoginAsync(IdentityUser user) => signInManager.PasswordSignInAsync(user, "password", false, true);
            }
            """;

        await Verifier.VerifyAsync(source);
    }
}
