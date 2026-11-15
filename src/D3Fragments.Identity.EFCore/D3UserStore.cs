using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace D3Fragments.Identity.EFCore;

/// <summary>
///     <para xml:lang="en">A <see cref="UserStore{TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey}"/> that leaves saving to the unit of work. <see cref="UserManager{TUser}"/> operations only change the tracked entities, and the changes are saved together with the domain changes when the unit of work (for example, the <c>UnitOfWorkBehavior</c> of a command) saves. Register it with <see cref="D3FragmentsIdentityBuilderExtensions.AddD3FragmentsStores{TContext}"/>.</para>
///     <para xml:lang="ja">保存をUnit of Workに任せる<see cref="UserStore{TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey}"/>。<see cref="UserManager{TUser}"/>の操作は追跡中のエンティティを変更するだけで、変更はUnit of Work（Commandの<c>UnitOfWorkBehavior</c>など）の保存時に、ドメインの変更とまとめて保存される。<see cref="D3FragmentsIdentityBuilderExtensions.AddD3FragmentsStores{TContext}"/>で登録する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en"><c>AutoSaveChanges</c> is set to <see langword="false"/> by the constructor; do not set it back to <see langword="true"/>. Because nothing is saved until the unit of work saves, a duplicate user name within the same save is detected by the database unique index (as a <see cref="DbUpdateException"/>), not by <see cref="UserManager{TUser}"/>, and a concurrency conflict is reported by the unit of work, not as an <see cref="IdentityResult"/>.</para>
///     <para xml:lang="ja"><c>AutoSaveChanges</c>はコンストラクタで<see langword="false"/>に設定する。<see langword="true"/>に戻さないこと。Unit of Workが保存するまで何も保存しないため、同じ保存単位でのユーザー名の重複は<see cref="UserManager{TUser}"/>ではなくデータベースの一意インデックスで（<see cref="DbUpdateException"/>として）検出される。同時実行の競合も、<see cref="IdentityResult"/>ではなくUnit of Workが報告する。</para>
///     <para xml:lang="en">The standard <see cref="UserStore{TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey}.UpdateAsync"/> attaches the user before updating it. When the user is already tracked, attaching resets its state, so the insert of a new user is lost and the original values of a modified user are overwritten, which breaks a save that contains more than one update (for example, creating a user and adding it to a role). This store only renews <see cref="IdentityUser{TKey}.ConcurrencyStamp"/> for a tracked user.</para>
///     <para xml:lang="ja">標準の<see cref="UserStore{TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey}.UpdateAsync"/>は、更新の前にユーザーをAttachする。ユーザーが追跡中の場合、Attachで状態が戻され、新しいユーザーの追加が失われたり、変更済みのユーザーの元の値が上書きされたりする。このため、1回の保存に複数の更新を含めると（ユーザーを作成してロールに追加するなど）保存に失敗する。このストアは、追跡中のユーザーについては<see cref="IdentityUser{TKey}.ConcurrencyStamp"/>の更新だけを行う。</para>
/// </remarks>
/// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
/// <typeparam name="TRole"><para xml:lang="en">The type of the role.</para><para xml:lang="ja">ロールの型。</para></typeparam>
/// <typeparam name="TContext"><para xml:lang="en">The type of the <see cref="DbContext"/>.</para><para xml:lang="ja"><see cref="DbContext"/>の型。</para></typeparam>
/// <typeparam name="TKey"><para xml:lang="en">The type of the primary key of users and roles.</para><para xml:lang="ja">ユーザー・ロールの主キーの型。</para></typeparam>
/// <typeparam name="TUserClaim"><para xml:lang="en">The type of the user claim.</para><para xml:lang="ja">ユーザークレームの型。</para></typeparam>
/// <typeparam name="TUserRole"><para xml:lang="en">The type of the user role.</para><para xml:lang="ja">ユーザーロールの型。</para></typeparam>
/// <typeparam name="TUserLogin"><para xml:lang="en">The type of the user login.</para><para xml:lang="ja">ユーザーログインの型。</para></typeparam>
/// <typeparam name="TUserToken"><para xml:lang="en">The type of the user token.</para><para xml:lang="ja">ユーザートークンの型。</para></typeparam>
/// <typeparam name="TRoleClaim"><para xml:lang="en">The type of the role claim.</para><para xml:lang="ja">ロールクレームの型。</para></typeparam>
/// <typeparam name="TUserPasskey"><para xml:lang="en">The type of the user passkey.</para><para xml:lang="ja">ユーザーパスキーの型。</para></typeparam>
public class D3UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey>
    : UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim, TUserPasskey>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TContext : DbContext
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>, new()
    where TUserRole : IdentityUserRole<TKey>, new()
    where TUserLogin : IdentityUserLogin<TKey>, new()
    where TUserToken : IdentityUserToken<TKey>, new()
    where TRoleClaim : IdentityRoleClaim<TKey>, new()
    where TUserPasskey : IdentityUserPasskey<TKey>, new()
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance that does not save changes by itself.</para>
    ///     <para xml:lang="ja">自身では変更を保存しない新しいインスタンスを初期化する。</para>
    /// </summary>
    /// <param name="context"><para xml:lang="en">The context used to access the store.</para><para xml:lang="ja">ストアへのアクセスに使うコンテキスト。</para></param>
    /// <param name="describer"><para xml:lang="en">The describer of the errors returned by the store.</para><para xml:lang="ja">ストアが返すエラーを記述するオブジェクト。</para></param>
    public D3UserStore(TContext context, IdentityErrorDescriber? describer = null)
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

        if (Context.Entry(user).State == EntityState.Detached)
        {
            return base.UpdateAsync(user, cancellationToken);
        }

        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Task.FromResult(IdentityResult.Success);
    }
}
