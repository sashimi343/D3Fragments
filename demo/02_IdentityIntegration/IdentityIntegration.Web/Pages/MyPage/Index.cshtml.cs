using D3Fragments.Application;

using IdentityIntegration.Core.Members;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

namespace IdentityIntegration.Web.Pages.MyPage;

public sealed class IndexModel(SignInManager<UserAccount> signIn, IRequestExecutor<GetMemberProfileQuery, MemberProfile?> getProfile) : AppPageModel(signIn)
{
    public MemberProfile? Profile { get; private set; }

    // ログイン中の Identity のユーザーの ID から、紐づく会員（業務上の利用者）を取得する
    public async Task OnGetAsync() =>
        Profile = await getProfile.ExecuteAsync(new GetMemberProfileQuery(CurrentAccountId), HttpContext.RequestAborted);
}
