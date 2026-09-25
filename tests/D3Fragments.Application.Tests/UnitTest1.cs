using D3Fragments.Application;
using D3Fragments;

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
        Func<TResponse, bool> shouldCommit,
        CancellationToken cancellationToken)
    {
        var response = await action(cancellationToken);
        if (!shouldCommit(response))
        {
            DiscardChanges();
        }
        return response;
    }
}

internal sealed record NoopCommand : ICommand<Result>;

internal sealed record NoopQuery : IQuery<Result>;

internal sealed record TransactionalCommand : IRequest<Result>, ITransactionalCommand;

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
    public async Task Command失敗時はDiscardChangesが呼ばれSaveChangesは呼ばれない()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new UnitOfWorkBehavior<NoopCommand, Result>(unitOfWork);

        await behavior.HandleAsync(new NoopCommand(), _ => Task.FromResult(Result.Failure(Error.Validation("ng"))), CancellationToken.None);

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
    public async Task ITransactionalCommandの場合は失敗時にDiscardChangesが呼ばれる()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<TransactionalCommand, Result>(unitOfWork);

        await behavior.HandleAsync(new TransactionalCommand(), _ => Task.FromResult(Result.Failure(Error.Validation("ng"))), CancellationToken.None);

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
