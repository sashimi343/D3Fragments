using D3Fragments.EFCore;
using D3Fragments.Identity.EFCore;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Core.Members;
using IdentityIntegration.Infrastructure.Email;
using IdentityIntegration.Infrastructure.Identity;
using IdentityIntegration.Infrastructure.Members;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityIntegration.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityIntegrationInfrastructure(this IServiceCollection services, string sqliteConnectionString, SmtpEndpoint smtp)
    {
        services.AddD3FragmentsEFCore<IdentityIntegrationDbContext>(
            [typeof(IdentityIntegrationDbContext).Assembly],
            options => options.UseSqlite(sqliteConnectionString));

        // Use AddIdentityCore instead of AddIdentity; cookies are registered in the presentation layer with AddIdentityCookies
        services.AddIdentityCore<UserAccount>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                // Creates the passkey table (AspNetUserPasskeys)
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddSignInManager()
            .AddDefaultTokenProviders()
            // Stores that leave saving to the UnitOfWork (SaveChanges is not called for each UserManager operation)
            .AddD3FragmentsStores<IdentityIntegrationDbContext>();

        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IdentityAccountService>();
        services.AddScoped<IAccountAuthenticator>(provider => provider.GetRequiredService<IdentityAccountService>());
        services.AddScoped<IAccountManager>(provider => provider.GetRequiredService<IdentityAccountService>());
        services.AddSingleton(smtp);
        services.AddSingleton<IAccountEmailSender, SmtpAccountEmailSender>();
        return services;
    }
}
