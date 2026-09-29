namespace D3Fragments.Tests;

public class DeletionTokenTests
{
    private sealed class TestAggregate(DateTimeOffset createdAt) : AggregateRoot(createdAt)
    {
        public DeletionToken<TestAggregate> Delete() => AllowDeletion(this);

        public DeletionToken<TestAggregate> DeleteOther(TestAggregate other) => AllowDeletion(other);
    }

    [Fact]
    public void AllowDeletion_自身を渡すと自身を保持するトークンを発行する()
    {
        var aggregate = new TestAggregate(DateTimeOffset.UtcNow);

        var token = aggregate.Delete();

        Assert.Same(aggregate, token.Entity);
    }

    [Fact]
    public void AllowDeletion_自身以外を渡すと例外をスローする()
    {
        var aggregate = new TestAggregate(DateTimeOffset.UtcNow);
        var other = new TestAggregate(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => aggregate.DeleteOther(other));
    }
}
