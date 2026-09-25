using System.Diagnostics.CodeAnalysis;

using D3Fragments;

namespace D3Fragments.Tests;

public class ResultTests
{
    [Fact]
    public void GetValueOrThrow_失敗結果の場合は例外をスローする()
    {
        var result = Result<int>.Failure(Error.Validation("invalid"));

        Assert.Throws<DomainException>(() => result.GetValueOrThrow());
    }

    [Fact]
    public void Merge_両方成功の場合は成功を返す()
    {
        var merged = Result.Success().Merge(Result.Success());

        Assert.True(merged.IsSuccess);
    }

    [Fact]
    public void Merge_一方が失敗の場合はエラーをマージした失敗を返す()
    {
        var merged = Result.Success().Merge(Result.Failure(Error.Validation("ng")));

        Assert.True(merged.IsFailure);
        Assert.Single(merged.Errors);
    }
}

public class EntityBaseTests
{
    private sealed class TestEntity : EntityBase
    {
        public TestEntity(DateTimeOffset createdAt) : base(createdAt)
        {
        }
    }

    [Fact]
    public void Equals_同じIDのエンティティは等しい()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new TestEntity(now);

        Assert.Equal(entity, entity);
    }

    [Fact]
    public void Equals_異なるIDのエンティティは等しくない()
    {
        var now = DateTimeOffset.UtcNow;
        var entity1 = new TestEntity(now);
        var entity2 = new TestEntity(now);

        Assert.NotEqual(entity1, entity2);
    }
}

public class ValueObjectBaseTests
{
    private sealed class PositiveNumber : ValueObjectBase<PositiveNumber, int>, IValueObject<PositiveNumber, int>
    {
        private PositiveNumber(int value) : base(value)
        {
        }

        public static PositiveNumber Create(int value) => new(value);

        public static bool ValidateValue(int value, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = value > 0 ? null : "must be positive";
            return value > 0;
        }
    }

    [Fact]
    public void TryCreate_不正な値は失敗結果を返す()
    {
        var result = PositiveNumber.TryCreate(-1);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Equals_同じ値のValueObjectは等しい()
    {
        var a = PositiveNumber.Create(1);
        var b = PositiveNumber.Create(1);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}

public class PagerTests
{
    [Fact]
    public void Empty_データ0件でもTotalPagesは1になる()
    {
        var pager = Pager<int>.Empty(10);

        Assert.Equal(1, pager.TotalPages);
        Assert.False(pager.HasNextPage);
    }

    [Fact]
    public void TotalPages_端数切り上げで計算される()
    {
        var pager = new Pager<int>([1, 2, 3], pageNo: 1, pageSize: 2, totalCount: 3);

        Assert.Equal(2, pager.TotalPages);
        Assert.True(pager.HasNextPage);
    }
}
