using System.Globalization;
using System.Reflection;

using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Provides extension methods that register the EF Core integration of D3Fragments.</para>
///     <para xml:lang="ja">D3FragmentsのEF Core連携機能を登録する拡張メソッドを提供する。</para>
/// </summary>
public static class D3FragmentsEFCoreServiceCollectionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Registers the <see cref="DbContext"/>, the unit of work, the domain event dispatch strategy, and the repositories found in the specified assemblies.</para>
    ///     <para xml:lang="ja"><see cref="DbContext"/>、Unit of Work、ドメインイベントのDispatch戦略、および指定したアセンブリ内のRepositoryを登録する。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">This method registers <typeparamref name="TContext"/> with <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments(DbContextOptionsBuilder, IEnumerable{Assembly})"/> applied after <paramref name="configureDbContext"/>. It also registers <see cref="EfUnitOfWork{TContext}"/> as <see cref="IUnitOfWork"/> and <see cref="ImmediateDispatchStrategy"/> as <see cref="IDomainEventDispatchStrategy"/> with the scoped lifetime. To use another dispatch strategy, register it before calling this method. For each non-abstract, non-generic type, every implemented interface that is or derives from <see cref="IRepository{TAggregate, TId}"/> is registered with the scoped lifetime. Services that are already registered are not replaced. <see cref="IDomainEventDispatcher"/>, which <see cref="ImmediateDispatchStrategy"/> depends on, is registered by <c>AddD3FragmentsApplication</c>. <c>ILogger&lt;T&gt;</c> must be registered separately; the generic host and ASP.NET Core register it by default.</para>
    ///     <para xml:lang="ja">このメソッドは、<paramref name="configureDbContext"/>の後に<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments(DbContextOptionsBuilder, IEnumerable{Assembly})"/>を適用した<typeparamref name="TContext"/>を登録する。また<see cref="EfUnitOfWork{TContext}"/>を<see cref="IUnitOfWork"/>として、<see cref="ImmediateDispatchStrategy"/>を<see cref="IDomainEventDispatchStrategy"/>としてScopedで登録する。別のDispatch戦略を使う場合は、このメソッドの呼び出し前に登録する。抽象型・ジェネリック型定義を除く各型について、実装している<see cref="IRepository{TAggregate, TId}"/>およびその派生インターフェースをすべてScopedで登録する。登録済みのサービスは置き換えない。<see cref="ImmediateDispatchStrategy"/>が依存する<see cref="IDomainEventDispatcher"/>は<c>AddD3FragmentsApplication</c>で登録する。<c>ILogger&lt;T&gt;</c>は別途登録が必要である（汎用ホストおよびASP.NET Coreは既定で登録する）。</para>
    /// </remarks>
    /// <typeparam name="TContext">
    ///     <para xml:lang="en">The type of the application's <see cref="DbContext"/>.</para>
    ///     <para xml:lang="ja">アプリケーションの<see cref="DbContext"/>の型。</para>
    /// </typeparam>
    /// <param name="services">
    ///     <para xml:lang="en">The service collection to add the services to.</para>
    ///     <para xml:lang="ja">サービスを追加するサービスコレクション。</para>
    /// </param>
    /// <param name="configureDbContext">
    ///     <para xml:lang="en">The action that configures the <see cref="DbContext"/>, such as selecting the database provider.</para>
    ///     <para xml:lang="ja">データベースプロバイダーの選択など、<see cref="DbContext"/>を構成するアクション。</para>
    /// </param>
    /// <param name="assemblies">
    ///     <para xml:lang="en">The assemblies to scan for <see cref="TypeSafeEnum{TEnum, TKey}"/> types and repositories. At least one assembly is required.</para>
    ///     <para xml:lang="ja"><see cref="TypeSafeEnum{TEnum, TKey}"/>型とRepositoryを走査するアセンブリ。1つ以上必要である。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same service collection for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じサービスコレクション。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="services"/>, <paramref name="configureDbContext"/>, or <paramref name="assemblies"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="services"/>、<paramref name="configureDbContext"/>、または<paramref name="assemblies"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="assemblies"/> is empty.</para>
    ///     <para xml:lang="ja"><paramref name="assemblies"/>が空の場合にスローされる。</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when more than one implementation is found for the same repository interface.</para>
    ///     <para xml:lang="ja">同じRepositoryインターフェースの実装が複数見つかった場合にスローされる。</para>
    /// </exception>
    public static IServiceCollection AddD3FragmentsEFCore<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        IEnumerable<Assembly> assemblies)
        where TContext : D3DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);
        ArgumentNullException.ThrowIfNull(assemblies);

        Assembly[] targets = [.. assemblies.Distinct()];
        if (targets.Length == 0)
        {
            throw new ArgumentException(ExceptionMessages.ArgumentException_AssembliesEmpty, nameof(assemblies));
        }

        services.AddDbContext<TContext>(options =>
        {
            configureDbContext(options);
            options.UseD3Fragments(targets);
        });
        services.TryAddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();
        services.TryAddScoped<IDomainEventDispatchStrategy, ImmediateDispatchStrategy>();
        AddRepositories(services, targets.SelectMany(x => x.DefinedTypes));

        return services;
    }

    // 重複検出をテストするため、走査対象の型を直接受け取れるようにしている
    internal static void AddRepositories(IServiceCollection services, IEnumerable<Type> types)
    {
        var implementations = new Dictionary<Type, Type>();
        foreach (var implementationType in types.Where(x => x is { IsAbstract: false, IsGenericTypeDefinition: false }))
        {
            foreach (var serviceType in implementationType.GetInterfaces().Where(IsRepositoryInterface))
            {
                if (!implementations.TryAdd(serviceType, implementationType) && implementations[serviceType] != implementationType)
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidOperationException_DuplicateRepositoryImplementation,
                        serviceType,
                        implementations[serviceType],
                        implementationType));
                }
            }
        }

        foreach (var (serviceType, implementationType) in implementations)
        {
            services.TryAddScoped(serviceType, implementationType);
        }
    }

    private static bool IsRepositoryInterface(Type type) =>
        IsClosedRepository(type) || type.GetInterfaces().Any(IsClosedRepository);

    private static bool IsClosedRepository(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRepository<,>);
}
