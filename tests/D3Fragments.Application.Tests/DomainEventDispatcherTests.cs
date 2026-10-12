using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D3Fragments.Application.Tests;

public class DomainEventDispatcherTests
{
    private sealed record SampleEvent(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed record OtherEvent(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class CallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class FirstHandler(CallLog log) : IBeforeCommitDomainEventHandler<SampleEvent>, IAfterCommitDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add(nameof(FirstHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler(CallLog log) : IBeforeCommitDomainEventHandler<SampleEvent>, IAfterCommitDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add(nameof(SecondHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class OtherEventHandler(CallLog log) : IBeforeCommitDomainEventHandler<OtherEvent>
    {
        public Task HandleAsync(OtherEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add(nameof(OtherEventHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IBeforeCommitDomainEventHandler<SampleEvent>, IAfterCommitDomainEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent domainEvent, CancellationToken cancellationToken) => throw new InvalidOperationException("ng");
    }

    // ログの出力内容を記録するロガー
    private sealed class RecordingLogger : ILogger<DomainEventDispatcher>
    {
        public List<(LogLevel Level, EventId EventId, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, eventId, exception, formatter(state, exception)));
    }

    private static (DomainEventDispatcher Dispatcher, CallLog Log, RecordingLogger Logger) CreateDispatcher(
        Action<IServiceCollection> configure,
        D3FragmentsApplicationOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CallLog>();
        configure(services);
        var provider = services.BuildServiceProvider();
        var logger = new RecordingLogger();
        return (new DomainEventDispatcher(provider, logger, Options.Create(options ?? new())), provider.GetRequiredService<CallLog>(), logger);
    }

    [Fact]
    public async Task Commit前_複数のハンドラーが登録順に呼ばれる()
    {
        var (dispatcher, log, _) = CreateDispatcher(services =>
        {
            services.AddScoped<IBeforeCommitDomainEventHandler<SampleEvent>, FirstHandler>();
            services.AddScoped<IBeforeCommitDomainEventHandler<SampleEvent>, SecondHandler>();
        });

        await dispatcher.DispatchBeforeCommitAsync(new SampleEvent(DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Equal([nameof(FirstHandler), nameof(SecondHandler)], log.Calls);
    }

    [Fact]
    public async Task ハンドラーが登録されていない場合は何もしない()
    {
        var (dispatcher, log, _) = CreateDispatcher(_ => { });

        await dispatcher.DispatchBeforeCommitAsync(new SampleEvent(DateTimeOffset.UtcNow), CancellationToken.None);
        await dispatcher.DispatchAfterCommitAsync(new SampleEvent(DateTimeOffset.UtcNow));

        Assert.Empty(log.Calls);
    }

    [Fact]
    public async Task 別のイベント型や別の段階のハンドラーは呼ばれない()
    {
        var (dispatcher, log, _) = CreateDispatcher(services =>
        {
            services.AddScoped<IBeforeCommitDomainEventHandler<SampleEvent>, FirstHandler>();
            services.AddScoped<IAfterCommitDomainEventHandler<SampleEvent>, SecondHandler>();
        });

        await dispatcher.DispatchBeforeCommitAsync(new SampleEvent(DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Equal([nameof(FirstHandler)], log.Calls);
    }

    [Fact]
    public async Task Commit前_ハンドラーが例外を投げた場合は後続のハンドラーを呼ばずに例外を伝播する()
    {
        var (dispatcher, log, _) = CreateDispatcher(services =>
        {
            services.AddScoped<IBeforeCommitDomainEventHandler<SampleEvent>, FirstHandler>();
            services.AddScoped<IBeforeCommitDomainEventHandler<SampleEvent>, ThrowingHandler>();
            services.AddScoped<IBeforeCommitDomainEventHandler<SampleEvent>, SecondHandler>();
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchBeforeCommitAsync(new SampleEvent(DateTimeOffset.UtcNow), CancellationToken.None));

        Assert.Equal([nameof(FirstHandler)], log.Calls);
    }

    [Fact]
    public async Task Commit後_ハンドラーが例外を投げてもログを出力して後続のハンドラーを呼ぶ()
    {
        var (dispatcher, log, logger) = CreateDispatcher(services =>
        {
            services.AddScoped<IAfterCommitDomainEventHandler<SampleEvent>, FirstHandler>();
            services.AddScoped<IAfterCommitDomainEventHandler<SampleEvent>, ThrowingHandler>();
            services.AddScoped<IAfterCommitDomainEventHandler<SampleEvent>, SecondHandler>();
        });

        await dispatcher.DispatchAfterCommitAsync(new SampleEvent(DateTimeOffset.UtcNow));

        Assert.Equal([nameof(FirstHandler), nameof(SecondHandler)], log.Calls);
        var entry = Assert.Single(logger.Entries, x => x.Level == LogLevel.Error);
        Assert.Equal(1001, entry.EventId.Id);
        Assert.IsType<InvalidOperationException>(entry.Exception);
        Assert.Contains(typeof(ThrowingHandler).FullName!, entry.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(SampleEvent).FullName!, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Commit後_ログのイベントIDをオプションで変更できる()
    {
        var (dispatcher, _, logger) = CreateDispatcher(
            services => services.AddScoped<IAfterCommitDomainEventHandler<SampleEvent>, ThrowingHandler>(),
            new D3FragmentsApplicationOptions { AfterCommitHandlerFailedEventId = new EventId(5001, "Custom") });

        await dispatcher.DispatchAfterCommitAsync(new SampleEvent(DateTimeOffset.UtcNow));

        Assert.Equal(5001, Assert.Single(logger.Entries, x => x.Level == LogLevel.Error).EventId.Id);
    }
}
