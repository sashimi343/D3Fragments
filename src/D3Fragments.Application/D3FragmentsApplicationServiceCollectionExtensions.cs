using System.Reflection;

using D3Fragments.Application.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Provides extension methods that register the use case execution infrastructure of D3Fragments.</para>
///     <para xml:lang="ja">D3Fragmentsのユースケース実行基盤を登録する拡張メソッドを提供する。</para>
/// </summary>
public static class D3FragmentsApplicationServiceCollectionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Registers the request executor, the standard pipeline behaviors, the domain event dispatcher, and the handlers found in the specified assemblies.</para>
    ///     <para xml:lang="ja">リクエストExecutor、標準のPipeline Behavior、ドメインイベントDispatcher、および指定したアセンブリ内のハンドラーを登録する。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">This method registers <see cref="RequestExecutor{TRequest, TResponse}"/>, <see cref="TransactionBehavior{TRequest, TResponse}"/>, <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/>, <see cref="DomainEventDispatcher"/>, and the non-abstract, non-generic implementations of <see cref="IRequestHandler{TRequest, TResponse}"/> and <see cref="IDomainEventHandler{TEvent}"/> with the scoped lifetime. It also registers <see cref="TimeProvider.System"/> as a singleton. Services that are already registered are not replaced, and calling this method more than once does not register duplicates. <see cref="IUnitOfWork"/> is not registered by this method.</para>
    ///     <para xml:lang="ja">このメソッドは、<see cref="RequestExecutor{TRequest, TResponse}"/>、<see cref="TransactionBehavior{TRequest, TResponse}"/>、<see cref="UnitOfWorkBehavior{TRequest, TResponse}"/>、<see cref="DomainEventDispatcher"/>、および抽象型・ジェネリック型定義を除く<see cref="IRequestHandler{TRequest, TResponse}"/>と<see cref="IDomainEventHandler{TEvent}"/>の実装をScopedで登録する。また<see cref="TimeProvider.System"/>をSingletonで登録する。登録済みのサービスは置き換えず、複数回呼び出しても重複して登録しない。<see cref="IUnitOfWork"/>はこのメソッドでは登録しない。</para>
    /// </remarks>
    /// <param name="services">
    ///     <para xml:lang="en">The service collection to add the services to.</para>
    ///     <para xml:lang="ja">サービスを追加するサービスコレクション。</para>
    /// </param>
    /// <param name="assemblies">
    ///     <para xml:lang="en">The assemblies to scan for handlers. At least one assembly is required.</para>
    ///     <para xml:lang="ja">ハンドラーを走査するアセンブリ。1つ以上必要である。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same service collection for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じサービスコレクション。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="services"/> or <paramref name="assemblies"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="services"/>または<paramref name="assemblies"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="assemblies"/> is empty.</para>
    ///     <para xml:lang="ja"><paramref name="assemblies"/>が空の場合にスローされる。</para>
    /// </exception>
    public static IServiceCollection AddD3FragmentsApplication(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        Assembly[] targets = [.. assemblies.Distinct()];
        if (targets.Length == 0)
        {
            throw new ArgumentException(ExceptionMessages.ArgumentException_AssembliesEmpty, nameof(assemblies));
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped(typeof(IRequestExecutor<,>), typeof(RequestExecutor<,>));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>)));
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        var implementationTypes = targets
            .SelectMany(x => x.DefinedTypes)
            .Where(x => x is { IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var implementationType in implementationTypes)
        {
            foreach (var serviceType in implementationType.ImplementedInterfaces.Where(x => x.IsGenericType))
            {
                var definition = serviceType.GetGenericTypeDefinition();
                if (definition == typeof(IRequestHandler<,>))
                {
                    services.TryAddScoped(serviceType, implementationType);
                }
                else if (definition == typeof(IDomainEventHandler<>))
                {
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
                }
            }
        }

        return services;
    }
}
