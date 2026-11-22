using D3Fragments;
using D3Fragments.EFCore;

using IdentityIntegration.Core.Members;

using Microsoft.EntityFrameworkCore;

namespace IdentityIntegration.Infrastructure.Members;

public sealed class MemberRepository(IdentityIntegrationDbContext dbContext)
    : RepositoryBase<IdentityIntegrationDbContext, Member>(dbContext), IMemberRepository
{
    public Task<Member?> FindByAccountIdAsync(UuidV7 accountId, CancellationToken cancellationToken) =>
        Set.FirstOrDefaultAsync(member => member.AccountId == accountId, cancellationToken);
}
