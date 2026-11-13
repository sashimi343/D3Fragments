using D3Fragments.Application;

using Xunit.Sdk;

namespace D3Fragments.Testing.Tests;

internal sealed record OrderPlaced(DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed record OrderCanceled(DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed class Order : AggregateRoot
{
    public Order(DateTimeOffset placedAt)
    {
        AddDomainEvent(new OrderPlaced(placedAt));
    }
}

public class ResultAssertTests
{
    [Fact]
    public void IsSuccess_成功の場合は値を返す()
    {
        Assert.Equal(1, ResultAssert.IsSuccess(Result<int>.Success(1)));
    }

    [Fact]
    public void IsSuccess_失敗の場合はエラー内容を含めて失敗する()
    {
        var ex = Assert.ThrowsAny<XunitException>(() => ResultAssert.IsSuccess(Result.Failure(Error.Validation("名前は必須です"))));

        Assert.Contains("名前は必須です", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IsFailure_失敗の場合はエラーを返し_成功の場合は失敗する()
    {
        Assert.Single(ResultAssert.IsFailure(Result.Failure(Error.Validation("e"))));
        Assert.ThrowsAny<XunitException>(() => ResultAssert.IsFailure(Result.Success()));
    }

    [Fact]
    public void HasError_指定した種類のエラーを返す()
    {
        var result = Result.Failure(Error.Validation("v"), Error.NotFound("n"));

        Assert.Equal("n", ResultAssert.HasError(result, ErrorType.NotFound).Message);
        Assert.ThrowsAny<XunitException>(() => ResultAssert.HasError(result, ErrorType.Conflict));
    }

    [Fact]
    public void ContainsError_メッセージの部分一致でエラーを返す()
    {
        var result = Result.Failure(Error.Validation("名前は必須です"));

        Assert.Equal(ErrorType.Validation, ResultAssert.ContainsError(result, "必須").Type);
        Assert.ThrowsAny<XunitException>(() => ResultAssert.ContainsError(result, "長すぎます"));
    }

    [Fact]
    public void IsSuccess_値を持つ結果が失敗の場合は失敗する()
    {
        Assert.ThrowsAny<XunitException>(() => ResultAssert.IsSuccess(Result<int>.Failure(Error.Validation("e"))));
    }

    [Fact]
    public void 各アサーション_nullの結果は例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => ResultAssert.IsSuccess(null!));
        Assert.Throws<ArgumentNullException>(() => ResultAssert.IsSuccess<int>(null!));
        Assert.Throws<ArgumentNullException>(() => ResultAssert.IsFailure(null!));
        Assert.Throws<ArgumentNullException>(() => ResultAssert.HasError(null!, ErrorType.Validation));
        Assert.Throws<ArgumentNullException>(() => ResultAssert.ContainsError(null!, "e"));
    }
}

public class DomainEventAssertTests
{
    [Fact]
    public void Raised_集約が保持するイベントを返す()
    {
        var order = new Order(TestTime.Now);

        Assert.Equal(TestTime.Now, DomainEventAssert.Raised<OrderPlaced>(order).OccurredAt);
        DomainEventAssert.NotRaised<OrderCanceled>(order);
    }

    [Fact]
    public void Raised_イベントがない場合や複数ある場合は失敗する()
    {
        IDomainEvent[] events = [new OrderPlaced(TestTime.Now), new OrderPlaced(TestTime.Now)];

        Assert.ThrowsAny<XunitException>(() => DomainEventAssert.Raised<OrderCanceled>(events));
        Assert.ThrowsAny<XunitException>(() => DomainEventAssert.Raised<OrderPlaced>(events));
        Assert.ThrowsAny<XunitException>(() => DomainEventAssert.NotRaised<OrderPlaced>(events));
    }

    [Fact]
    public void NotRaised_集約が指定したイベントを保持している場合は失敗する()
    {
        Assert.ThrowsAny<XunitException>(() => DomainEventAssert.NotRaised<OrderPlaced>(new Order(TestTime.Now)));
    }

    [Fact]
    public void 各アサーション_nullの集約は例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => DomainEventAssert.Raised<OrderPlaced>((IHasDomainEvents)null!));
        Assert.Throws<ArgumentNullException>(() => DomainEventAssert.NotRaised<OrderPlaced>((IHasDomainEvents)null!));
    }

    [Fact]
    public async Task SpyDomainEventDispatcher_ディスパッチしたイベントを段階ごとに記録する()
    {
        var spy = new SpyDomainEventDispatcher();
        IDomainEventDispatcher dispatcher = spy;

        await dispatcher.DispatchBeforeCommitAsync(new OrderPlaced(TestTime.Now), TestContext.Current.CancellationToken);

        DomainEventAssert.Raised<OrderPlaced>(spy.BeforeCommitEvents);
        Assert.Empty(spy.AfterCommitEvents);

        await dispatcher.DispatchAfterCommitAsync(new OrderPlaced(TestTime.Now));

        DomainEventAssert.Raised<OrderPlaced>(spy.AfterCommitEvents);
    }
}

public class TestDataTests
{
    [Fact]
    public void Unique_呼び出しごとに異なる値を返す()
    {
        var values = Enumerable.Range(0, 100).Select(_ => TestData.Unique("DEV")).ToList();

        Assert.All(values, x => Assert.StartsWith("DEV-", x, StringComparison.Ordinal));
        Assert.Equal(values.Count, values.Distinct().Count());
    }

    [Fact]
    public void CreateProvider_基準時刻を現在時刻とする()
    {
        Assert.Equal(TestTime.Now, TestTime.CreateProvider().GetUtcNow());
    }
}
