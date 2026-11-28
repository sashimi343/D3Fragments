using D3Fragments;

using Microsoft.AspNetCore.Identity;

namespace IdentityIntegration.Infrastructure.Identity;

// The Identity user (a persistence model for authentication). The business user is represented by Member in Core and linked by AccountId (this Id).
// The presentation layer references it to issue cookies with SignInManager<UserAccount>, and DFR0002 allows the Identity user type as an exception.
// The key is UuidV7 (converted to a Guid column by the D3Fragments conventions)
public sealed class UserAccount : IdentityUser<UuidV7>
{
    public UserAccount()
    {
        Id = UuidV7.NewUuidV7();
        SecurityStamp = Guid.NewGuid().ToString();
    }
}
