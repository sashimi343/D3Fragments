using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace D3Fragments.Application.Tests;

public class D3FragmentsApplicationServiceCollectionExtensionsTests
{
    public sealed record PingQuery(string Message) : IQuery<string>;

    public sealed class PingQueryHandler : IRequestHandler<PingQuery, string>
    {
        public Task<string> HandleAsync(PingQuery request, CancellationToken cancellationToken) => Task.FromResult($"pong:{request.Message}");
    }

    public sealed record PingEvent(DateTimeOffset OccurredAt, List<string> Log) : IDomainEvent;

    public sealed class PingBeforeCommitHandler : IBeforeCommitDomainEventHandler<PingEvent>
    {
        public Task HandleAsync(PingEvent domainEvent, CancellationToken cancellationToken)
        {
            domainEvent.Log.Add(nameof(PingBeforeCommitHandler));
            return Task.CompletedTask;
        }
    }

    public sealed class PingAfterCommitHandler : IAfterCommitDomainEventHandler<PingEvent>
    {
        public Task HandleAsync(PingEvent domainEvent, CancellationToken cancellationToken)
        {
            domainEvent.Log.Add(nameof(PingAfterCommitHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class NullUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public void DiscardChanges()
        {
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> action, CancellationToken cancellationToken) =>
            action(cancellationToken);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IUnitOfWork, NullUnitOfWork>();
        services.AddD3FragmentsApplication([typeof(D3FragmentsApplicationServiceCollectionExtensionsTests).Assembly]);
        return services;
    }

    [Fact]
    public async Task Executorを解決してHandlerを実行できる()
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var executor = scope.ServiceProvider.GetRequiredService<IRequestExecutor<PingQuery, string>>();
        var response = await executor.ExecuteAsync(new PingQuery("hello"), CancellationToken.None);

        Assert.Equal("pong:hello", response);
    }

    [Fact]
    public void 標準Behaviorが登録される()
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var behaviorTypes = scope.ServiceProvider.GetServices<IPipelineBehavior<PingQuery, string>>().Select(x => x.GetType());

        Assert.Equal(
            [typeof(TransactionBehavior<PingQuery, string>), typeof(UnitOfWorkBehavior<PingQuery, string>)],
            behaviorTypes);
    }

    [Fact]
    public async Task ドメインイベントハンドラーが登録されDispatcherから呼ばれる()
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var domainEvent = new PingEvent(DateTimeOffset.UtcNow, []);

        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchBeforeCommitAsync(domainEvent, CancellationToken.None);
        await dispatcher.DispatchAfterCommitAsync(domainEvent);

        Assert.Equal([nameof(PingBeforeCommitHandler), nameof(PingAfterCommitHandler)], domainEvent.Log);
    }

    [Fact]
    public void TimeProviderにSystemが登録される()
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void 複数回呼び出しても重複して登録しない()
    {
        var services = CreateServices();
        var count = services.Count;

        services.AddD3FragmentsApplication([typeof(D3FragmentsApplicationServiceCollectionExtensionsTests).Assembly]);

        Assert.Equal(count, services.Count);
    }

    [Fact]
    public void アセンブリが空の場合はArgumentExceptionをスローする()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>("assemblies", () => services.AddD3FragmentsApplication([]));
    }
}
