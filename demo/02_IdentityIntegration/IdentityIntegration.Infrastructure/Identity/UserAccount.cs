using D3Fragments;

using Microsoft.AspNetCore.Identity;

namespace IdentityIntegration.Infrastructure.Identity;

// Identity のユーザー（認証のための永続化モデル）。業務上の利用者は Core の Member で表し、AccountId（この Id）で紐づける。
// プレゼンテーション層が SignInManager<UserAccount> で Cookie を発行するため参照するが、DFR0002 は Identity のユーザー型を例外として許可する。
// キーは UuidV7（D3Fragments の規約で Guid の列に変換する）
public sealed class UserAccount : IdentityUser<UuidV7>
{
    public UserAccount()
    {
        Id = UuidV7.NewUuidV7();
        SecurityStamp = Guid.NewGuid().ToString();
    }
}
