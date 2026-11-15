using System.Globalization;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace D3Fragments.Identity.EFCore;

/// <summary>
///     <para xml:lang="en">Extension methods that register the ASP.NET Core Identity stores of D3Fragments.</para>
///     <para xml:lang="ja">D3FragmentsのASP.NET Core Identity用ストアを登録する拡張メソッド。</para>
/// </summary>
public static class D3FragmentsIdentityBuilderExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Registers the user store (and the role store when roles are used) that leave saving to the unit of work, in place of <c>AddEntityFrameworkStores</c>.</para>
    ///     <para xml:lang="ja">保存をUnit of Workに任せるユーザーストア（ロールを使う場合はロールストアも）を、<c>AddEntityFrameworkStores</c>の代わりに登録する。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">The generic arguments of the stores are resolved in the same way as <c>AddEntityFrameworkStores</c>. When <typeparamref name="TContext"/> derives from <c>IdentityDbContext</c> (with roles) or <c>IdentityUserContext</c> (without roles), the entity types of that base type are used, including the passkey type; otherwise the default Identity entity types are used. The stores are registered with the scoped lifetime and replace stores that are already registered (for example, by <c>AddEntityFrameworkStores</c>). <typeparamref name="TContext"/> itself is not registered; register it with <c>AddD3FragmentsEFCore</c>.</para>
    ///     <para xml:lang="ja">ストアの型引数は<c>AddEntityFrameworkStores</c>と同じ方法で解決する。<typeparamref name="TContext"/>が<c>IdentityDbContext</c>（ロールあり）または<c>IdentityUserContext</c>（ロールなし）から派生している場合は、その基底型のエンティティ型（パスキーの型を含む）を使い、それ以外の場合はIdentityの既定のエンティティ型を使う。ストアはScopedで登録し、登録済みのストア（<c>AddEntityFrameworkStores</c>で登録したものなど）を置き換える。<typeparamref name="TContext"/>自体は登録しない。<c>AddD3FragmentsEFCore</c>で登録すること。</para>
    /// </remarks>
    /// <typeparam name="TContext">
    ///     <para xml:lang="en">The type of the <see cref="DbContext"/> that contains the Identity entities.</para>
    ///     <para xml:lang="ja">Identityのエンティティを含む<see cref="DbContext"/>の型。</para>
    /// </typeparam>
    /// <param name="builder">
    ///     <para xml:lang="en">The Identity builder.</para>
    ///     <para xml:lang="ja">Identityのビルダー。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same Identity builder for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じIdentityのビルダー。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="builder"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="builder"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when the user type does not derive from <see cref="IdentityUser{TKey}"/>, or the role type does not derive from <see cref="IdentityRole{TKey}"/>.</para>
    ///     <para xml:lang="ja">ユーザーの型が<see cref="IdentityUser{TKey}"/>を、またはロールの型が<see cref="IdentityRole{TKey}"/>を継承していない場合にスローされる。</para>
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddIdentityCore&lt;Account&gt;()
    ///     .AddRoles&lt;IdentityRole&lt;Guid&gt;&gt;()
    ///     .AddD3FragmentsStores&lt;AppDbContext&gt;()
    ///     .AddSignInManager();
    /// </code>
    /// </example>
    public static IdentityBuilder AddD3FragmentsStores<TContext>(this IdentityBuilder builder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var userType = builder.UserType;
        var keyType = FindGenericBaseType(userType, typeof(IdentityUser<>))?.GenericTypeArguments[0]
            ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidOperationException_NotIdentityUser, userType));
        var contextType = typeof(TContext);

        if (builder.RoleType is { } roleType)
        {
            if (FindGenericBaseType(roleType, typeof(IdentityRole<>)) is null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidOperationException_NotIdentityRole, roleType));
            }

            // IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken, TUserPasskey> の型引数
            var types = FindGenericBaseType(contextType, typeof(IdentityDbContext<,,,,,,,,>))?.GenericTypeArguments
                ?? [userType, roleType, keyType, Closed(typeof(IdentityUserClaim<>)), Closed(typeof(IdentityUserRole<>)), Closed(typeof(IdentityUserLogin<>)), Closed(typeof(IdentityRoleClaim<>)), Closed(typeof(IdentityUserToken<>)), Closed(typeof(IdentityUserPasskey<>))];
            Replace(
                typeof(IUserStore<>).MakeGenericType(userType),
                typeof(D3UserStore<,,,,,,,,,>).MakeGenericType(userType, roleType, contextType, types[2], types[3], types[4], types[5], types[7], types[6], types[8]));
            Replace(
                typeof(IRoleStore<>).MakeGenericType(roleType),
                typeof(D3RoleStore<,,,,>).MakeGenericType(roleType, contextType, types[2], types[4], types[6]));
        }
        else
        {
            // IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken, TUserPasskey> の型引数
            var types = FindGenericBaseType(contextType, typeof(IdentityUserContext<,,,,,>))?.GenericTypeArguments
                ?? [userType, keyType, Closed(typeof(IdentityUserClaim<>)), Closed(typeof(IdentityUserLogin<>)), Closed(typeof(IdentityUserToken<>)), Closed(typeof(IdentityUserPasskey<>))];
            Replace(
                typeof(IUserStore<>).MakeGenericType(userType),
                typeof(D3UserOnlyStore<,,,,,,>).MakeGenericType(userType, contextType, types[1], types[2], types[3], types[4], types[5]));
        }

        return builder;

        Type Closed(Type definition) => definition.MakeGenericType(keyType);

        void Replace(Type serviceType, Type implementationType)
        {
            builder.Services.RemoveAll(serviceType);
            builder.Services.AddScoped(serviceType, implementationType);
        }
    }

    // 基底型をたどり、指定したジェネリック型定義を閉じた型を返す（AddEntityFrameworkStoresと同じ探し方）
    private static Type? FindGenericBaseType(Type type, Type genericTypeDefinition)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericTypeDefinition)
            {
                return current;
            }
        }
        return null;
    }
}
