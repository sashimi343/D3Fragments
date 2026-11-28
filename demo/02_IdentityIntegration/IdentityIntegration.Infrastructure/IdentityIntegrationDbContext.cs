using D3Fragments;
using D3Fragments.EFCore;

using IdentityIntegration.Core.Members;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityIntegration.Infrastructure;

// Holds the Identity tables (AspNetUsers, etc.) and the member table in one context, saved in the same UnitOfWork.
// It inherits IdentityUserContext, so it cannot inherit D3DbContext.
// The same conventions as D3DbContext are applied by ApplyD3FragmentsConventions, and the save-time processing by UseD3Fragments (called by AddD3FragmentsEFCore)
public sealed class IdentityIntegrationDbContext(DbContextOptions<IdentityIntegrationDbContext> options)
    : IdentityUserContext<UserAccount, UuidV7>(options)
{
    public DbSet<Member> Members => Set<Member>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.ApplyD3FragmentsConventions(options);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Definitions of the Identity tables
        base.OnModelCreating(builder);

        builder.Entity<Member>(member =>
        {
            member.ComplexValueObject(x => x.Nickname);
            // One member per Identity user
            member.HasIndex(x => x.AccountId).IsUnique();
        });
    }
}
