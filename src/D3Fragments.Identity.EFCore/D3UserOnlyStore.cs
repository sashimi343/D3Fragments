using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace D3Fragments.Identity.EFCore;

/// <summary>
///     <para xml:lang="en">A <see cref="UserOnlyStore{TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken, TUserPasskey}"/> (a user store without roles) that leaves saving to the unit of work. See <see cref="D3UserStore{TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey}"/> for the behavior. Register it with <see cref="D3FragmentsIdentityBuilderExtensions.AddD3FragmentsStores{TContext}"/>.</para>
///     <para xml:lang="ja">保存をUnit of Workに任せる<see cref="UserOnlyStore{TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken, TUserPasskey}"/>（ロールを使わないユーザーストア）。動作は<see cref="D3UserStore{TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey}"/>を参照。<see cref="D3FragmentsIdentityBuilderExtensions.AddD3FragmentsStores{TContext}"/>で登録する。</para>
/// </summary>
/// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
/// <typeparam name="TContext"><para xml:lang="en">The type of the <see cref="DbContext"/>.</para><para xml:lang="ja"><see cref="DbContext"/>の型。</para></typeparam>
/// <typeparam name="TKey"><para xml:lang="en">The type of the primary key of users.</para><para xml:lang="ja">ユーザーの主キーの型。</para></typeparam>
/// <typeparam name="TUserClaim"><para xml:lang="en">The type of the user claim.</para><para xml:lang="ja">ユーザークレームの型。</para></typeparam>
/// <typeparam name="TUserLogin"><para xml:lang="en">The type of the user login.</para><para xml:lang="ja">ユーザーログインの型。</para></typeparam>
/// <typeparam name="TUserToken"><para xml:lang="en">The type of the user token.</para><para xml:lang="ja">ユーザートークンの型。</para></typeparam>
/// <typeparam name="TUserPasskey"><para xml:lang="en">The type of the user passkey.</para><para xml:lang="ja">ユーザーパスキーの型。</para></typeparam>
public class D3UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken, TUserPasskey>
    : UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken, TUserPasskey>
    where TUser : IdentityUser<TKey>
    where TContext : DbContext
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>, new()
    where TUserLogin : IdentityUserLogin<TKey>, new()
    where TUserToken : IdentityUserToken<TKey>, new()
    where TUserPasskey : IdentityUserPasskey<TKey>, new()
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance that does not save changes by itself.</para>
    ///     <para xml:lang="ja">自身では変更を保存しない新しいインスタンスを初期化する。</para>
    /// </summary>
    /// <param name="context"><para xml:lang="en">The context used to access the store.</para><para xml:lang="ja">ストアへのアクセスに使うコンテキスト。</para></param>
    /// <param name="describer"><para xml:lang="en">The describer of the errors returned by the store.</para><para xml:lang="ja">ストアが返すエラーを記述するオブジェクト。</para></param>
    public D3UserOnlyStore(TContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }

    /// <inheritdoc />
    public override Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        // 追跡中のユーザーをAttachすると状態が戻されるため、ConcurrencyStampの更新だけ行う（D3UserStoreと同じ）
        if (Context.Entry(user).State == EntityState.Detached)
        {
            return base.UpdateAsync(user, cancellationToken);
        }

        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Task.FromResult(IdentityResult.Success);
    }
}
