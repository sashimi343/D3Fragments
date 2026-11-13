using System;
using System.Collections.Generic;

using D3Fragments;

namespace BasicWebApp.Domain.Members;

public interface IMemberRepository : IRepository<Member, UuidV7>
{
    public Task<bool> ExistsByEmailAddressAsync(string emailAddress, CancellationToken cancellationToken = default);

    public Task<Member?> FindByEmailAddressAsync(string emailAddress, CancellationToken cancellationToken = default);
}
