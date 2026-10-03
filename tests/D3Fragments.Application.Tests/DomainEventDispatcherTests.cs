using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.Application.Tests;

public class DomainEventDispatcherTests
{
    private sealed record SampleEvent(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed record OtherEvent(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class CallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class FirstHandler(CallLog log) : IDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add(nameof(FirstHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler(CallLog log) : IDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add(nameof(SecondHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class OtherEventHandler(CallLog log) : IDomainEventHandler<OtherEvent>
    {
        public Task HandleAsync(OtherEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add(nameof(OtherEventHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken) => throw new InvalidOperationException("ng");
    }

    private static (DomainEventDispatcher Dispatcher, CallLog Log) CreateDispatcher(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CallLog>();
        configure(services);
        var provider = services.BuildServiceProvider();
        return (new DomainEventDispatcher(provider), provider.GetRequiredService<CallLog>());
    }

    [Fact]
    public async Task 複数のハンドラーが登録順に呼ばれる()
    {
        var (dispatcher, log) = CreateDispatcher(services =>
        {
            services.AddScoped<IDomainEventHandler<SampleEvent>, FirstHandler>();
            services.AddScoped<IDomainEventHandler<SampleEvent>, SecondHandler>();
        });

        await dispatcher.DispatchAsync(new SampleEvent(DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Equal([nameof(FirstHandler), nameof(SecondHandler)], log.Calls);
    }

    [Fact]
    public async Task ハンドラーが登録されていない場合は何もしない()
    {
        var (dispatcher, log) = CreateDispatcher(_ => { });

        await dispatcher.DispatchAsync(new SampleEvent(DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Empty(log.Calls);
    }

    [Fact]
    public async Task 別のイベント型のハンドラーは呼ばれない()
    {
        var (dispatcher, log) = CreateDispatcher(services =>
        {
            services.AddScoped<IDomainEventHandler<SampleEvent>, FirstHandler>();
            services.AddScoped<IDomainEventHandler<OtherEvent>, OtherEventHandler>();
        });

        await dispatcher.DispatchAsync(new SampleEvent(DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Equal([nameof(FirstHandler)], log.Calls);
    }

    [Fact]
    public async Task ハンドラーが例外を投げた場合は後続のハンドラーを呼ばずに例外を伝播する()
    {
        var (dispatcher, log) = CreateDispatcher(services =>
        {
            services.AddScoped<IDomainEventHandler<SampleEvent>, FirstHandler>();
            services.AddScoped<IDomainEventHandler<SampleEvent>, ThrowingHandler>();
            services.AddScoped<IDomainEventHandler<SampleEvent>, SecondHandler>();
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(new SampleEvent(DateTimeOffset.UtcNow), CancellationToken.None));

        Assert.Equal([nameof(FirstHandler)], log.Calls);
    }
}
