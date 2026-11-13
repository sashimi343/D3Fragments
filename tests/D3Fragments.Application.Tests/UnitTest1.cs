namespace D3Fragments.Application.Tests;

file sealed class FakeUnitOfWork : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public bool DiscardChangesCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }

    public void DiscardChanges() => DiscardChangesCalled = true;

    public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            return await action(cancellationToken);
        }
        catch
        {
            DiscardChanges();
            throw;
        }
    }
}

internal sealed record NoopCommand : ICommand<Result>;

internal sealed record NoopQuery : IQuery<Result>;

internal sealed record PlainRequest : IRequest<Result>;

internal sealed record TransactionalCommand : IRequest<Result>, ITransactionalCommand;

internal sealed class DomainFailureException() : Exception("ng");

public class UnitOfWorkBehaviorTests
{
    [Fact]
    public async Task Command成功時はSaveChangesが呼ばれる()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new UnitOfWorkBehavior<NoopCommand, Result>(unitOfWork);

        await behavior.HandleAsync(new NoopCommand(), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.False(unitOfWork.DiscardChangesCalled);
    }

    [Fact]
    public async Task Handlerが例外を投げた場合はDiscardChangesが呼ばれSaveChangesは呼ばれない()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new UnitOfWorkBehavior<NoopCommand, Result>(unitOfWork);

        await Assert.ThrowsAsync<DomainFailureException>(() =>
            behavior.HandleAsync(new NoopCommand(), _ => throw new DomainFailureException(), CancellationToken.None));

        Assert.False(unitOfWork.SaveChangesCalled);
        Assert.True(unitOfWork.DiscardChangesCalled);
    }

    [Fact]
    public async Task Query実行時はSaveChangesもDiscardChangesも呼ばれない()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new UnitOfWorkBehavior<NoopQuery, Result>(unitOfWork);

        await behavior.HandleAsync(new NoopQuery(), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        Assert.False(unitOfWork.SaveChangesCalled);
        Assert.False(unitOfWork.DiscardChangesCalled);
    }

    // CommandとQueryのどちらのマーカーもない場合、変更が黙って失われないよう保存する
    [Fact]
    public async Task IRequestを直接実装したリクエストはCommandと同様に保存される()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new UnitOfWorkBehavior<PlainRequest, Result>(unitOfWork);

        await behavior.HandleAsync(new PlainRequest(), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        Assert.True(unitOfWork.SaveChangesCalled);
    }
}

public class TransactionBehaviorTests
{
    [Fact]
    public async Task ITransactionalCommandではない場合はExecuteInTransactionAsyncを呼ばない()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<NoopCommand, Result>(unitOfWork);
        var called = false;

        await behavior.HandleAsync(new NoopCommand(), _ =>
        {
            called = true;
            return Task.FromResult(Result.Success());
        }, CancellationToken.None);

        Assert.True(called);
        Assert.False(unitOfWork.DiscardChangesCalled);
    }

    [Fact]
    public async Task ITransactionalCommandの場合はHandlerが例外を投げるとDiscardChangesが呼ばれる()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<TransactionalCommand, Result>(unitOfWork);

        await Assert.ThrowsAsync<DomainFailureException>(() =>
            behavior.HandleAsync(new TransactionalCommand(), _ => throw new DomainFailureException(), CancellationToken.None));

        Assert.True(unitOfWork.DiscardChangesCalled);
    }
}

public class RequestExecutorTests
{
    private sealed class RecordingHandler : IRequestHandler<NoopCommand, Result>
    {
        public Task<Result> HandleAsync(NoopCommand request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    private sealed class OrderRecordingBehavior(int order, List<int> executionOrder) : IPipelineBehavior<NoopCommand, Result>
    {
        public int Order => order;

        public async Task<Result> HandleAsync(NoopCommand request, RequestHandlerDelegate<Result> next, CancellationToken cancellationToken)
        {
            executionOrder.Add(order);
            return await next(cancellationToken);
        }
    }

    [Fact]
    public async Task Behaviorは値が小さいものほど外側で実行される()
    {
        var executionOrder = new List<int>();
        var behaviors = new IPipelineBehavior<NoopCommand, Result>[]
        {
            new OrderRecordingBehavior(300, executionOrder),
            new OrderRecordingBehavior(100, executionOrder),
            new OrderRecordingBehavior(200, executionOrder),
        };
        var executor = new RequestExecutor<NoopCommand, Result>(new RecordingHandler(), behaviors);

        await executor.ExecuteAsync(new NoopCommand(), CancellationToken.None);

        Assert.Equal([100, 200, 300], executionOrder);
    }
}

file sealed class FakeExecutor(Func<Task<int>> action) : IRequestExecutor<FakeQuery, int>
{
    public Task<int> ExecuteAsync(FakeQuery request, CancellationToken cancellationToken) => action();
}

internal sealed record FakeQuery : IQuery<int>;

public class RequestExecutorExtensionsTests
{
    [Fact]
    public async Task TryExecuteAsync_成功時はレスポンスを保持する成功結果を返す()
    {
        var result = await new FakeExecutor(() => Task.FromResult(42)).TryExecuteAsync(new FakeQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TryExecuteAsync_DomainExceptionは失敗結果に変換する()
    {
        var error = Error.Conflict("ng", target: "Name");

        var result = await new FakeExecutor(() => throw new DomainException(error)).TryExecuteAsync(new FakeQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Same(error, Assert.Single(result.Errors));
    }

    [Fact]
    public async Task TryExecuteAsync_DomainException以外の例外はそのまま伝播する()
    {
        var executor = new FakeExecutor(() => throw new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.TryExecuteAsync(new FakeQuery(), CancellationToken.None));
    }
}
