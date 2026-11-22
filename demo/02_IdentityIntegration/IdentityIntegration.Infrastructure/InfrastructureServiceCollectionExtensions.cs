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

        // AddIdentity ではなく AddIdentityCore を使い、Cookie はプレゼンテーション層で AddIdentityCookies により登録する
        services.AddIdentityCore<UserAccount>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                // パスキーのテーブル（AspNetUserPasskeys）を作る
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<JapaneseIdentityErrorDescriber>()
            // 保存を UnitOfWork に任せるストア（UserManager の各操作で SaveChanges を呼ばない）
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
