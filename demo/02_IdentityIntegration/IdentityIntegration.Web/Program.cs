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

// Identity cookies (application, external login, waiting for two-factor authentication, remember this browser)
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options => options.LoginPath = "/Account/Login");
// The demo checks the security stamp every time (to reflect setting changes and restarts in the cookie immediately)
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
    // The cookie claims are rebuilt at every check, so carry over the sign-in methods (amr, external login provider)
    options.OnRefreshingPrincipal = context =>
    {
        Claim[] methods = [.. context.CurrentPrincipal!.FindAll(claim => claim.Type is "amr" or ClaimTypes.AuthenticationMethod)];
        context.NewPrincipal!.Identities.First().AddClaims(methods.Where(claim => !context.NewPrincipal.HasClaim(claim.Type, claim.Value)));
        return Task.CompletedTask;
    };
});

// External login (Keycloak). Aspire passes the Keycloak URL (HTTPS with the development certificate) in services__keycloak__https__0
string keycloakUrl = builder.Configuration["services:keycloak:https:0"] ?? "https://localhost:8080";
builder.Services.AddAuthentication()
    .AddOpenIdConnect(ExternalLoginProviders.Keycloak, "Keycloak", options =>
    {
        options.Authority = $"{keycloakUrl}/realms/identity-integration";
        options.ClientId = "identity-integration-web";
        options.ResponseType = "code";
        options.Scope.Add("email");
        // The external login result is stored in Identity's external login cookie and read with GetExternalLoginInfoAsync
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

// The database is SQLite in-memory. The connection is kept open to keep the content until the app exits
string connectionString = $"Data Source=IdentityIntegration-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
builder.Services.AddSingleton(_ => new SqliteConnection(connectionString));

// Register D3Fragments-related classes
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

app.UseRequestLocalization("en-US");
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
