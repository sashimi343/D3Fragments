using System.Globalization;

using D3Fragments.Validations;

namespace D3Fragments.Tests;

public class StringValidatorTests
{
    [Fact]
    public void Check_必須でnullの場合は必須エラーのみを返す()
    {
        var validator = new StringValidator("Name", target: "name").Required().MinLength(3);

        var result = validator.Check(null);

        var error = Assert.Single(result.Errors);
        Assert.Equal("name", error.Target);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public void Check_必須で空白のみの場合は失敗する()
    {
        Assert.True(new StringValidator("Name").Required().Check("  ").IsFailure);
    }

    [Fact]
    public void Check_任意項目でnullの場合はルールをスキップする()
    {
        Assert.True(new StringValidator("Name").MinLength(3).Check(null).IsSuccess);
    }

    [Fact]
    public void Check_違反したルールのエラーをすべて返す()
    {
        var validator = new StringValidator("Code").MinLength(3).OnlyCharacters(CharacterKinds.AsciiDigit);

        Assert.Equal(2, validator.Check("a").Errors.Count);
    }

    [Theory]
    [InlineData(StringLengthUnit.Utf16CodeUnit, "𠮷", false)]
    [InlineData(StringLengthUnit.CodePoint, "𠮷", true)]
    [InlineData(StringLengthUnit.CodePoint, "👨‍👩‍👧", false)]
    [InlineData(StringLengthUnit.TextElement, "👨‍👩‍👧", true)]
    public void MaxLength_指定した単位で文字数を数える(StringLengthUnit unit, string value, bool expected)
    {
        var validator = new StringValidator("Name", lengthUnit: unit).MaxLength(1);

        Assert.Equal(expected, validator.Check(value).IsSuccess);
    }

    [Theory]
    [InlineData("あいう123", true)]
    [InlineData("らーめん", true)]
    [InlineData("abc", false)]
    [InlineData("カタカナ", false)]
    public void OnlyCharacters_いずれかの文字種に該当すれば成功する(string value, bool expected)
    {
        var validator = new StringValidator("Name").OnlyCharacters(CharacterKinds.Hiragana | CharacterKinds.AsciiDigit);

        Assert.Equal(expected, validator.Check(value).IsSuccess);
    }

    [Theory]
    [InlineData(CharacterKinds.HalfWidthKatakana, "ｶﾞｰﾙ･ﾌﾚﾝﾄﾞ", true)]
    [InlineData(CharacterKinds.HalfWidthKatakana, "ガール", false)]
    public void OnlyCharacters_追加した文字種を判定する(CharacterKinds kinds, string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Name").OnlyCharacters(kinds).Check(value).IsSuccess);
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("user@localhost", false)]
    [InlineData("user example@example.com", false)]
    public void Email_メールアドレス形式を検証する(string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Email").Email().Check(value).IsSuccess);
    }

    [Fact]
    public void Check_カスタムメッセージに表示名と引数を埋め込む()
    {
        var validator = new StringValidator("氏名").MaxLength(2, "{0}は{1}文字まで");

        Assert.Equal("氏名は2文字まで", validator.Check("abc").GetFirstErrorMessage());
    }

    [Fact]
    public void Check_既定メッセージは検証時のUIカルチャで取得する()
    {
        var validator = new StringValidator("Name").Required();
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            Assert.Equal("Nameは必須です。", validator.Check(null).GetFirstErrorMessage());

            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Assert.Equal("Name is required.", validator.Check(null).GetFirstErrorMessage());
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}

public class NumberValidatorTests
{
    [Fact]
    public void Check_必須でnullの場合は失敗する()
    {
        Assert.True(new NumberValidator<int>("Age").Required().Check(null).IsFailure);
    }

    [Fact]
    public void Range_最小値が最大値より大きい場合は例外になる()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumberValidator<int>("Age").Range(10, 1));
    }

    [Fact]
    public void Min_NaNは失敗する()
    {
        Assert.True(new NumberValidator<double>("Rate").Min(0).Check(double.NaN).IsFailure);
    }

    [Theory]
    [InlineData("123.45", true)]
    [InlineData("0.5", true)]
    [InlineData("1.50", true)]
    [InlineData("1234", false)]
    [InlineData("1.234", false)]
    public void Digits_整数部と小数部の桁数を検証する(string value, bool expected)
    {
        var validator = new NumberValidator<decimal>("Price").Digits(3, 2);

        Assert.Equal(expected, validator.Check(decimal.Parse(value, CultureInfo.InvariantCulture)).IsSuccess);
    }

    [Fact]
    public void Digits_decimalに変換できない値は失敗する()
    {
        Assert.True(new NumberValidator<double>("Rate").Digits(3, 2).Check(double.NaN).IsFailure);
    }
}

public class DateValidatorTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(9, true)]
    [InlineData(10, false)]
    public void DateOnlyPast_今日より前であることを検証する(int day, bool expected)
    {
        var validator = new DateOnlyValidator("BirthDate").Past(Clock);

        Assert.Equal(expected, validator.Check(new DateOnly(2026, 1, day)).IsSuccess);
    }

    [Fact]
    public void DateTimeOffsetFuture_現在時刻より後であることを検証する()
    {
        var validator = new DateTimeOffsetValidator("ExpiresAt").Future(Clock);

        Assert.True(validator.Check(Clock.GetUtcNow().AddSeconds(1)).IsSuccess);
        Assert.True(validator.Check(Clock.GetUtcNow()).IsFailure);
    }
}

public class ResultCombineTests
{
    [Fact]
    public void Combine_すべてのエラーをまとめる()
    {
        var result = Result.Combine(
            new StringValidator("Name").Required().Check(null),
            new NumberValidator<int>("Age").Min(0).Check(-1),
            Result.Success());

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void ThrowIfFailure_ErrorのCodeとTargetを例外に引き継ぐ()
    {
        var result = Result.Failure(Error.Validation("invalid", code: "E001", target: "name"));

        var exception = Assert.Throws<DomainException>(() => result.ThrowIfFailure());

        Assert.Equal("E001", exception.ErrorCode);
        Assert.Equal("name", exception.Target);
        Assert.Equal("name", exception.GetDomainError().Target);
    }

    [Fact]
    public void ThrowIfFailure_全エラーを例外に引き継ぎ代表値は先頭エラーとする()
    {
        var result = Result.Failure(
            Error.Validation("first", target: "name"),
            Error.Conflict("second", code: "E002", target: "email"));

        var exception = Assert.Throws<DomainException>(() => result.ThrowIfFailure("E999"));

        Assert.Equal(["name", "email"], exception.Errors.Select(x => x.Target));
        Assert.Equal("E002", exception.Errors[1].Code);
        Assert.Equal("first", exception.Message);
        Assert.Equal(ErrorType.Validation, exception.ErrorType);
        Assert.Equal("E999", exception.ErrorCode);
    }

    [Fact]
    public void Errors_エラー一覧なしで生成した例外は後から設定した値を反映する()
    {
        var exception = new ConflictOccurredException("duplicated") { Target = "email" };

        Assert.Equal("email", Assert.Single(exception.Errors).Target);
    }

    [Fact]
    public void Combine_すべて成功なら成功を返す()
    {
        Assert.True(Result.Combine(Result.Success(), Result.Success()).IsSuccess);
    }
}
