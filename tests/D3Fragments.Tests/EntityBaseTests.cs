namespace D3Fragments.Tests;

public class EntityBaseTests
{
    private sealed class TestEntity : EntityBase
    {
    }

    [Fact]
    public void Equals_同じIDのエンティティは等しい()
    {
        var entity = new TestEntity();

        Assert.Equal(entity, entity);
    }

    [Fact]
    public void Equals_異なるIDのエンティティは等しくない()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        Assert.NotEqual(entity1, entity2);
    }

    private sealed class Book(int id) : EntityBase<int>(id);

    private sealed class Magazine(int id) : EntityBase<int>(id);

    [Fact]
    public void Constructor_既定値のIDは例外になる()
    {
        Assert.Throws<ArgumentException>(() => new Book(0));
    }

    [Fact]
    public void Equals_別インスタンスでも同じ型と同じIDなら等しい()
    {
        var a = new Book(1);
        var b = new Book(1);

        Assert.True(a.Equals(a));
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_同じIDでも型が異なれば等しくない()
    {
        var book = new Book(1);
        var magazine = new Magazine(1);

        Assert.False(book.Equals(magazine));
        Assert.False(book == magazine);
        Assert.True(book != magazine);
    }

    [Fact]
    public void Equals_nullやエンティティ以外とは等しくない()
    {
        var book = new Book(1);

        Assert.False(book.Equals(null));
        Assert.False(book.Equals((object)1));
        Assert.False(book == null);
        Assert.False(null == book);
        Assert.True(book != null);
    }

    [Fact]
    public void 等価演算子_null同士は等しい()
    {
        Book? left = null;
        Book? right = null;

        Assert.True(left == right);
        Assert.False(left != right);
    }
}
