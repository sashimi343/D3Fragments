using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Members;

using D3Fragments.EFCore;

using Microsoft.EntityFrameworkCore;

namespace BasicWebApp.Infrastructure.EFCore;

public class MemberRepository : RepositoryBase<BasicWebAppDbContext, Member>, IMemberRepository
{
    public MemberRepository(BasicWebAppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<bool> ExistsByEmailAddressAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        return await DbContext.Members.AnyAsync(m => m.EmailAddress.Value == emailAddress, cancellationToken);
    }

    public async Task<Member?> FindByEmailAddressAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        return await DbContext.Members.FirstOrDefaultAsync(m => m.EmailAddress.Value == emailAddress, cancellationToken);
    }
}
