using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace D3Fragments.EFCore.Tests;

// 連鎖の世代を持つイベント。Commit前のハンドラーが次の世代のイベントを発行するテストに使う
internal sealed record ChainEvent(TestEntity Entity, int Generation, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed class EfUnitOfWorkTests
{
    private sealed class LambdaHandler(Func<ChainEvent, CancellationToken, Task> handle)
        : IBeforeCommitDomainEventHandler<ChainEvent>, IAfterCommitDomainEventHandler<ChainEvent>
    {
        public Task HandleAsync(ChainEvent domainEvent, CancellationToken cancellationToken) => handle(domainEvent, cancellationToken);
    }

    private sealed class RecordingLogger : ILogger<EfUnitOfWork<TestDbContext>>
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, eventId, formatter(state, exception)));
    }

    // SQLiteのインメモリDB上で、実際のDispatcherとハンドラーを使ってUnit of Workを組み立てる
    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ServiceProvider _provider;

        public Harness(Func<ChainEvent, CancellationToken, Task>? beforeCommit = null, Func<ChainEvent, CancellationToken, Task>? afterCommit = null)
        {
            _connection.Open();
            DbContext = CreateContext();
            DbContext.Database.EnsureCreated();

            var services = new ServiceCollection();
            if (beforeCommit is not null)
            {
                services.AddSingleton<IBeforeCommitDomainEventHandler<ChainEvent>>(new LambdaHandler(beforeCommit));
            }
            if (afterCommit is not null)
            {
                services.AddSingleton<IAfterCommitDomainEventHandler<ChainEvent>>(new LambdaHandler(afterCommit));
            }
            _provider = services.BuildServiceProvider();

            var options = Options.Create(new D3FragmentsApplicationOptions());
            var dispatcher = new DomainEventDispatcher(_provider, NullLogger<DomainEventDispatcher>.Instance, options);
            UnitOfWork = new EfUnitOfWork<TestDbContext>(DbContext, dispatcher, new ImmediateDispatchStrategy(dispatcher), Logger, options);
        }

        public TestDbContext DbContext { get; }

        public IUnitOfWork UnitOfWork { get; }

        public RecordingLogger Logger { get; } = new();

        public TestDbContext CreateContext() => new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options);

        public async Task<List<int>> SavedValuesAsync()
        {
            await using var dbContext = CreateContext();
            return await dbContext.Entities.Select(x => x.Value).OrderBy(x => x).ToListAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private static TestEntity AddWithEvent(TestDbContext dbContext, int value)
    {
        var entity = new TestEntity(value);
        entity.Raise(new ChainEvent(entity, 0, DateTimeOffset.UtcNow));
        dbContext.Entities.Add(entity);
        return entity;
    }

    [Fact]
    public async Task Commit前_ハンドラーの変更は業務データと同じ保存で確定する()
    {
        TestDbContext? dbContext = null;
        await using var harness = new Harness(beforeCommit: (e, _) =>
        {
            dbContext!.Entities.Add(new TestEntity(99));
            return Task.CompletedTask;
        });
        dbContext = harness.DbContext;
        AddWithEvent(dbContext, 1);

        await harness.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal([1, 99], await harness.SavedValuesAsync());
    }

    [Fact]
    public async Task Commit前_ハンドラーが発行したイベントも連鎖してディスパッチされ_Commit後にすべて配信される()
    {
        var calls = new List<string>();
        await using var harness = new Harness(
            beforeCommit: (e, _) =>
            {
                calls.Add($"before:{e.Generation}");
                if (e.Generation < 2)
                {
                    e.Entity.Raise(new ChainEvent(e.Entity, e.Generation + 1, DateTimeOffset.UtcNow));
                }
                return Task.CompletedTask;
            },
            afterCommit: (e, _) =>
            {
                calls.Add($"after:{e.Generation}");
                return Task.CompletedTask;
            });
        AddWithEvent(harness.DbContext, 1);

        await harness.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(["before:0", "before:1", "before:2", "after:0", "after:1", "after:2"], calls);
    }

    [Fact]
    public async Task Commit前_イベントが上限を超えて発行され続けると例外になり保存されない()
    {
        await using var harness = new Harness(beforeCommit: (e, _) =>
        {
            e.Entity.Raise(new ChainEvent(e.Entity, e.Generation + 1, DateTimeOffset.UtcNow));
            return Task.CompletedTask;
        });
        AddWithEvent(harness.DbContext, 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.UnitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.Contains(typeof(ChainEvent).FullName!, ex.Message, StringComparison.Ordinal);
        Assert.Empty(await harness.SavedValuesAsync());
    }

    [Fact]
    public async Task Commit前_ハンドラーの例外は伝播し保存されない()
    {
        await using var harness = new Harness(beforeCommit: (_, _) => throw new DomainException(ErrorType.InvalidState, "ng"));
        AddWithEvent(harness.DbContext, 1);

        await Assert.ThrowsAsync<DomainException>(() => harness.UnitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.Empty(await harness.SavedValuesAsync());
    }

    [Fact]
    public async Task Commit後_ハンドラーには呼び出し元のキャンセルトークンを渡さない()
    {
        var canBeCanceled = true;
        await using var harness = new Harness(afterCommit: (_, ct) =>
        {
            canBeCanceled = ct.CanBeCanceled;
            return Task.CompletedTask;
        });
        AddWithEvent(harness.DbContext, 1);
        using var cts = new CancellationTokenSource();

        await harness.UnitOfWork.SaveChangesAsync(cts.Token);

        Assert.False(canBeCanceled);
    }

    [Fact]
    public async Task Commit後_ハンドラーがエンティティを直接変更した場合は破棄してログを出力する()
    {
        await using var harness = new Harness(afterCommit: (e, _) =>
        {
            e.Entity.ChangeValue(5);
            return Task.CompletedTask;
        });
        AddWithEvent(harness.DbContext, 1);

        await harness.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        var entry = Assert.Single(harness.Logger.Entries, x => x.Level == LogLevel.Error);
        Assert.Equal(1002, entry.EventId.Id);
        Assert.Contains("TestEntity: Modified", entry.Message, StringComparison.Ordinal);
        Assert.Empty(harness.DbContext.ChangeTracker.Entries());
        Assert.Equal([1], await harness.SavedValuesAsync());
    }

    [Fact]
    public async Task トランザクション内_Commit後のハンドラーはCommit後に呼ばれ_トランザクション付きの処理を実行できる()
    {
        var calls = new List<string>();
        Harness? harness = null;
        await using var _ = harness = new Harness(
            beforeCommit: (_, _) =>
            {
                calls.Add("before");
                return Task.CompletedTask;
            },
            afterCommit: async (_, ct) =>
            {
                calls.Add("after");
                await harness!.UnitOfWork.ExecuteInTransactionAsync(
                    async innerCt =>
                    {
                        harness.DbContext.Entities.Add(new TestEntity(2));
                        return await harness.UnitOfWork.SaveChangesAsync(innerCt);
                    },
                    ct);
            });

        await harness.UnitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                AddWithEvent(harness.DbContext, 1);
                await harness.UnitOfWork.SaveChangesAsync(ct);
                // 同一トランザクション内で再度保存しても、イベントは二重にディスパッチされない
                await harness.UnitOfWork.SaveChangesAsync(ct);
                calls.Add("action-end");
                return 0;
            },
            CancellationToken.None);

        Assert.Equal(["before", "action-end", "after"], calls);
        Assert.Equal([1, 2], await harness.SavedValuesAsync());
    }

    [Fact]
    public async Task トランザクション内_ネストは例外になる()
    {
        await using var harness = new Harness();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.UnitOfWork.ExecuteInTransactionAsync(
                async ct => await harness.UnitOfWork.ExecuteInTransactionAsync(_ => Task.FromResult(0), ct),
                CancellationToken.None));
    }

    [Fact]
    public async Task DiscardChanges_追跡中の変更が破棄される()
    {
        await using var harness = new Harness();
        harness.DbContext.Entities.Add(new TestEntity(1));

        harness.UnitOfWork.DiscardChanges();

        Assert.Empty(harness.DbContext.ChangeTracker.Entries());
    }
}
