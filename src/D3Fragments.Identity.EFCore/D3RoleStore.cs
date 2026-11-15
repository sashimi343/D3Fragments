using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace D3Fragments.Identity.EFCore;

/// <summary>
///     <para xml:lang="en">A <see cref="RoleStore{TRole, TContext, TKey, TUserRole, TRoleClaim}"/> that leaves saving to the unit of work. <see cref="RoleManager{TRole}"/> operations only change the tracked entities, and the changes are saved when the unit of work saves. For a tracked role, <see cref="UpdateAsync"/> only renews <see cref="IdentityRole{TKey}.ConcurrencyStamp"/>, for the same reason as <see cref="D3UserStore{TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey}"/>. Register it with <see cref="D3FragmentsIdentityBuilderExtensions.AddD3FragmentsStores{TContext}"/>.</para>
///     <para xml:lang="ja">保存をUnit of Workに任せる<see cref="RoleStore{TRole, TContext, TKey, TUserRole, TRoleClaim}"/>。<see cref="RoleManager{TRole}"/>の操作は追跡中のエンティティを変更するだけで、変更はUnit of Workの保存時に保存される。追跡中のロールについては、<see cref="D3UserStore{TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey}"/>と同じ理由で、<see cref="UpdateAsync"/>は<see cref="IdentityRole{TKey}.ConcurrencyStamp"/>の更新だけを行う。<see cref="D3FragmentsIdentityBuilderExtensions.AddD3FragmentsStores{TContext}"/>で登録する。</para>
/// </summary>
/// <typeparam name="TRole"><para xml:lang="en">The type of the role.</para><para xml:lang="ja">ロールの型。</para></typeparam>
/// <typeparam name="TContext"><para xml:lang="en">The type of the <see cref="DbContext"/>.</para><para xml:lang="ja"><see cref="DbContext"/>の型。</para></typeparam>
/// <typeparam name="TKey"><para xml:lang="en">The type of the primary key of roles.</para><para xml:lang="ja">ロールの主キーの型。</para></typeparam>
/// <typeparam name="TUserRole"><para xml:lang="en">The type of the user role.</para><para xml:lang="ja">ユーザーロールの型。</para></typeparam>
/// <typeparam name="TRoleClaim"><para xml:lang="en">The type of the role claim.</para><para xml:lang="ja">ロールクレームの型。</para></typeparam>
public class D3RoleStore<TRole, TContext, TKey, TUserRole, TRoleClaim>
    : RoleStore<TRole, TContext, TKey, TUserRole, TRoleClaim>
    where TRole : IdentityRole<TKey>
    where TContext : DbContext
    where TKey : IEquatable<TKey>
    where TUserRole : IdentityUserRole<TKey>, new()
    where TRoleClaim : IdentityRoleClaim<TKey>, new()
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance that does not save changes by itself.</para>
    ///     <para xml:lang="ja">自身では変更を保存しない新しいインスタンスを初期化する。</para>
    /// </summary>
    /// <param name="context"><para xml:lang="en">The context used to access the store.</para><para xml:lang="ja">ストアへのアクセスに使うコンテキスト。</para></param>
    /// <param name="describer"><para xml:lang="en">The describer of the errors returned by the store.</para><para xml:lang="ja">ストアが返すエラーを記述するオブジェクト。</para></param>
    public D3RoleStore(TContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }

    /// <inheritdoc />
    public override Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);

        if (Context.Entry(role).State == EntityState.Detached)
        {
            return base.UpdateAsync(role, cancellationToken);
        }

        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Task.FromResult(IdentityResult.Success);
    }
}
