using D3Fragments.Application;

using IdentityIntegration.Core.Members;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

namespace IdentityIntegration.Web.Pages.MyPage;

public sealed class IndexModel(SignInManager<UserAccount> signIn, IRequestExecutor<GetMemberProfileQuery, MemberProfile?> getProfile) : AppPageModel(signIn)
{
    public MemberProfile? Profile { get; private set; }

    // Gets the linked member (the business user) from the ID of the signed-in Identity user
    public async Task OnGetAsync() =>
        Profile = await getProfile.ExecuteAsync(new GetMemberProfileQuery(CurrentAccountId), HttpContext.RequestAborted);
}
