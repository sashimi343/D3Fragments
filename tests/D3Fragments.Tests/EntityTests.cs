namespace D3Fragments.Tests;

public class EntityTests
{
    private sealed class TestEntity : Entity;

    [Fact]
    public void CreatedAt_UpdatedAt_生成時のシステム日時が設定される()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new TestEntity();
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(entity.CreatedAt, before, after);
        Assert.Equal(entity.CreatedAt, entity.UpdatedAt);
    }

    [Fact]
    public void MarkUpdated_更新日時を現在日時にしてバージョンを1つ進める()
    {
        var entity = new TestEntity();
        var createdAt = entity.CreatedAt;

        ((IUpdateTrackable)entity).MarkUpdated();
        var before = DateTimeOffset.UtcNow;
        ((IUpdateTrackable)entity).MarkUpdated();
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(2, entity.Version);
        Assert.InRange(entity.UpdatedAt, before, after);
        Assert.Equal(createdAt, entity.CreatedAt);
    }
}
