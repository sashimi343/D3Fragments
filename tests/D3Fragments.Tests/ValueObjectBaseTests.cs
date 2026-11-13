using System.Diagnostics.CodeAnalysis;

namespace D3Fragments.Tests;

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

    [Fact]
    public void Create_不正な値はバリデーションのメッセージを持つ例外になる()
    {
        var exception = Assert.Throws<DomainValidationException>(() => PositiveNumber.Create(0));

        Assert.Equal("must be positive", exception.Message);
        Assert.Equal(ErrorType.Validation, exception.ErrorType);
    }

    [Fact]
    public void TryCreate_正しい値は成功結果を返す()
    {
        var result = PositiveNumber.TryCreate(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Value);
    }

    [Fact]
    public void Equals_異なる値は等しくない()
    {
        var a = PositiveNumber.Create(1);
        var b = PositiveNumber.Create(2);

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_object版とハッシュコードは値で判定する()
    {
        var a = PositiveNumber.Create(1);

        Assert.True(a.Equals((object)PositiveNumber.Create(1)));
        Assert.False(a.Equals((object)1));
        Assert.Equal(PositiveNumber.Create(1).GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void Equals_nullとは等しくなくnull同士は等しい()
    {
        var a = PositiveNumber.Create(1);
        PositiveNumber? none = null;

        Assert.False(a.Equals(none));
        Assert.False(a == none);
        Assert.False(none == a);
        Assert.True(a != none);
        Assert.True(none == null);
    }

    [Fact]
    public void ToString_値の文字列表現を返す()
    {
        Assert.Equal("42", PositiveNumber.Create(42).ToString());
    }
}
