using System.Security.Claims;

using D3Fragments.AspNetCore;
using D3Fragments.Application;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure;
using IdentityIntegration.Infrastructure.Email;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options => options.Conventions.AuthorizeFolder("/MyPage"));

// Identity の Cookie（アプリケーション・外部ログイン・二要素認証待ち・このブラウザを記憶する）
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options => options.LoginPath = "/Account/Login");
// デモでは security stamp を毎回確認する（設定の変更や再起動を、すぐ Cookie に反映するため）
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
    // 確認のたびに Cookie のクレームが作り直されるため、サインインの方法（amr・外部ログインのプロバイダー）を引き継ぐ
    options.OnRefreshingPrincipal = context =>
    {
        Claim[] methods = [.. context.CurrentPrincipal!.FindAll(claim => claim.Type is "amr" or ClaimTypes.AuthenticationMethod)];
        context.NewPrincipal!.Identities.First().AddClaims(methods.Where(claim => !context.NewPrincipal.HasClaim(claim.Type, claim.Value)));
        return Task.CompletedTask;
    };
});

// 外部ログイン（Keycloak）。Aspire が Keycloak の URL（開発用の証明書による HTTPS）を services__keycloak__https__0 で渡す
string keycloakUrl = builder.Configuration["services:keycloak:https:0"] ?? "https://localhost:8080";
builder.Services.AddAuthentication()
    .AddOpenIdConnect(ExternalLoginProviders.Keycloak, "Keycloak", options =>
    {
        options.Authority = $"{keycloakUrl}/realms/identity-integration";
        options.ClientId = "identity-integration-web";
        options.ResponseType = "code";
        options.Scope.Add("email");
        // 外部ログインの結果は Identity の外部ログイン用の Cookie に入れ、GetExternalLoginInfoAsync で読み取る
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

// DB は SQLite のインメモリ。接続を開いたままにして、アプリの終了まで内容を保つ
string connectionString = $"Data Source=IdentityIntegration-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
builder.Services.AddSingleton(_ => new SqliteConnection(connectionString));

// D3Fragments関連クラスの登録
builder.Services.AddD3FragmentsApplication([typeof(SignInWithPasswordCommand).Assembly]);
builder.Services.AddIdentityIntegrationInfrastructure(
    connectionString,
    SmtpEndpoint.Parse(builder.Configuration.GetConnectionString("mailpit") ?? "Endpoint=smtp://localhost:1025"));
builder.Services.AddD3FragmentsAspNetCore();

var app = builder.Build();

await app.Services.GetRequiredService<SqliteConnection>().OpenAsync();
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentityIntegrationDbContext>().Database.EnsureCreatedAsync();
}

app.UseRequestLocalization("ja-JP");
app.UseExceptionHandler("/Error");
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public static class ExternalLoginProviders
{
    public const string Keycloak = "Keycloak";
}
