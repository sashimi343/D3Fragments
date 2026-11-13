namespace D3Fragments.Tests;

public class DeletionTokenTests
{
    private sealed class TestAggregate : AggregateRoot
    {
        public DeletionToken<TestAggregate> Delete() => IssueDeletionToken(this);

        public DeletionToken<TestAggregate> DeleteOther(TestAggregate other) => IssueDeletionToken(other);
    }

    [Fact]
    public void IssueDeletionToken_自身を渡すと自身を保持するトークンを発行する()
    {
        var aggregate = new TestAggregate();

        var token = aggregate.Delete();

        Assert.Same(aggregate, token.Entity);
    }

    [Fact]
    public void IssueDeletionToken_自身以外を渡すと例外をスローする()
    {
        var aggregate = new TestAggregate();
        var other = new TestAggregate();

        Assert.Throws<ArgumentException>(() => aggregate.DeleteOther(other));
    }
}
