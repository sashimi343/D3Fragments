using D3Fragments;
using D3Fragments.EFCore;

using IdentityIntegration.Core.Members;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityIntegration.Infrastructure;

// Identity のテーブル（AspNetUsers など）と会員のテーブルを1つのコンテキストに持ち、同じ UnitOfWork で保存する。
// IdentityUserContext を継承するため D3DbContext は継承できない。
// D3DbContext と同じ規約は ApplyD3FragmentsConventions で、保存時の処理は UseD3Fragments（AddD3FragmentsEFCore が呼び出す）で適用する
public sealed class IdentityIntegrationDbContext(DbContextOptions<IdentityIntegrationDbContext> options)
    : IdentityUserContext<UserAccount, UuidV7>(options)
{
    public DbSet<Member> Members => Set<Member>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.ApplyD3FragmentsConventions(options);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity のテーブルの定義
        base.OnModelCreating(builder);

        builder.Entity<Member>(member =>
        {
            member.ComplexValueObject(x => x.Nickname);
            // 1つの Identity のユーザーに会員は1人
            member.HasIndex(x => x.AccountId).IsUnique();
        });
    }
}
