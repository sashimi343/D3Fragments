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

    [Fact]
    public void Constructor_未定義の長さ単位は例外になる()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StringValidator("Name", lengthUnit: (StringLengthUnit)99));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    public void NotNull_nullのみを未入力として扱う(string? value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Name").NotNull().Check(value).IsSuccess);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("a", true)]
    public void Required_空文字は未入力として扱う(string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Name").Required().Check(value).IsSuccess);
    }

    [Fact]
    public void Required_カスタムメッセージと検証時の表示名を使う()
    {
        var validator = new StringValidator("Name", target: "name").Required("{0}がありません");

        var error = Assert.Single(validator.Check(null, "氏名").Errors);

        Assert.Equal("氏名がありません", error.Message);
        Assert.Equal("name", error.Target);
    }

    [Theory]
    [InlineData("ab", false)]
    [InlineData("abc", true)]
    public void MinLength_最小文字数以上であることを検証する(string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Name").MinLength(3).Check(value).IsSuccess);
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("abcd", false)]
    public void MaxLength_最大文字数以下であることを検証する(string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Name").MaxLength(3).Check(value).IsSuccess);
    }

    [Theory]
    [InlineData("ab", false)]
    [InlineData("abc", true)]
    [InlineData("abcd", false)]
    public void Length_文字数が一致することを検証する(string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Name").Length(3).Check(value).IsSuccess);
    }

    [Fact]
    public void 文字数ルール_負の文字数は例外になり0は許可する()
    {
        var validator = new StringValidator("Name");

        Assert.Throws<ArgumentOutOfRangeException>(() => validator.MinLength(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => validator.MaxLength(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => validator.Length(-1));
        Assert.True(validator.MinLength(0).MaxLength(0).Length(0).Check(string.Empty).IsSuccess);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("12a", false)]
    public void Matches_パターン文字列に一致することを検証する(string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Code").Matches("^[0-9]+$").Check(value).IsSuccess);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("12a", false)]
    public void Matches_正規表現に一致することを検証する(string value, bool expected)
    {
        var regex = new System.Text.RegularExpressions.Regex("^[0-9]+$");

        Assert.Equal(expected, new StringValidator("Code").Matches(regex).Check(value).IsSuccess);
    }

    [Fact]
    public void Matches_タイムアウトした場合は失敗として扱う()
    {
        // 指数的なバックトラックを起こすパターンで、1ms のタイムアウトを確実に超えさせる
        var regex = new System.Text.RegularExpressions.Regex("^(a+)+$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(1));

        Assert.True(new StringValidator("Code").Matches(regex).Check(new string('a', 40) + "!").IsFailure);
    }

    [Fact]
    public void Matches_nullの正規表現は例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => new StringValidator("Code").Matches((System.Text.RegularExpressions.Regex)null!));
    }

    [Theory]
    [InlineData("a@b.c", true)]
    [InlineData("first.last@sub.example.co.jp", true)]
    [InlineData("@example.com", false)]
    [InlineData("user@@example.com", false)]
    [InlineData("user@example.", false)]
    [InlineData("user@.example.com", false)]
    [InlineData("user@example..com", false)]
    public void Email_ドメイン部の構成を検証する(string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Email").Email().Check(value).IsSuccess);
    }

    [Theory]
    [InlineData(CharacterKinds.AsciiDigit, "0", true)]
    [InlineData(CharacterKinds.AsciiDigit, "9", true)]
    [InlineData(CharacterKinds.AsciiDigit, "/", false)]
    [InlineData(CharacterKinds.AsciiDigit, ":", false)]
    [InlineData(CharacterKinds.AsciiUppercase, "A", true)]
    [InlineData(CharacterKinds.AsciiUppercase, "Z", true)]
    [InlineData(CharacterKinds.AsciiUppercase, "@", false)]
    [InlineData(CharacterKinds.AsciiUppercase, "[", false)]
    [InlineData(CharacterKinds.AsciiLowercase, "a", true)]
    [InlineData(CharacterKinds.AsciiLowercase, "z", true)]
    [InlineData(CharacterKinds.AsciiLowercase, "`", false)]
    [InlineData(CharacterKinds.AsciiLowercase, "{", false)]
    [InlineData(CharacterKinds.AsciiSymbol, "!", true)]
    [InlineData(CharacterKinds.AsciiSymbol, "/", true)]
    [InlineData(CharacterKinds.AsciiSymbol, ":", true)]
    [InlineData(CharacterKinds.AsciiSymbol, "@", true)]
    [InlineData(CharacterKinds.AsciiSymbol, "[", true)]
    [InlineData(CharacterKinds.AsciiSymbol, "`", true)]
    [InlineData(CharacterKinds.AsciiSymbol, "{", true)]
    [InlineData(CharacterKinds.AsciiSymbol, "~", true)]
    [InlineData(CharacterKinds.AsciiSymbol, " ", false)]
    [InlineData(CharacterKinds.AsciiSymbol, "0", false)]
    [InlineData(CharacterKinds.AsciiSymbol, "A", false)]
    [InlineData(CharacterKinds.AsciiSymbol, "a", false)]
    [InlineData(CharacterKinds.AsciiSymbol, "\u007F", false)]
    [InlineData(CharacterKinds.AsciiSpace, " ", true)]
    [InlineData(CharacterKinds.AsciiSpace, "\t", false)]
    [InlineData(CharacterKinds.AsciiSpace, "　", false)]
    [InlineData(CharacterKinds.Hiragana, "ぁ", true)]
    [InlineData(CharacterKinds.Hiragana, "ゖ", true)]
    [InlineData(CharacterKinds.Hiragana, "ゝ", true)]
    [InlineData(CharacterKinds.Hiragana, "ゟ", true)]
    [InlineData(CharacterKinds.Hiragana, "ー", true)]
    [InlineData(CharacterKinds.Hiragana, "぀", false)]
    [InlineData(CharacterKinds.Hiragana, "゗", false)]
    [InlineData(CharacterKinds.Hiragana, "゜", false)]
    [InlineData(CharacterKinds.Hiragana, "゠", false)]
    [InlineData(CharacterKinds.FullWidthKatakana, "ァ", true)]
    [InlineData(CharacterKinds.FullWidthKatakana, "ヿ", true)]
    [InlineData(CharacterKinds.FullWidthKatakana, "゠", false)]
    [InlineData(CharacterKinds.FullWidthKatakana, "ゖ", false)]
    [InlineData(CharacterKinds.HalfWidthKatakana, "･", true)]
    [InlineData(CharacterKinds.HalfWidthKatakana, "ﾟ", true)]
    [InlineData(CharacterKinds.HalfWidthKatakana, "､", false)]
    [InlineData(CharacterKinds.HalfWidthKatakana, "ﾠ", false)]
    [InlineData(CharacterKinds.AsciiPrintable, "Hello, World!", true)]
    [InlineData(CharacterKinds.AsciiPrintable, "", true)]
    public void OnlyCharacters_文字種の範囲の境界を判定する(CharacterKinds kinds, string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Name").OnlyCharacters(kinds).Check(value).IsSuccess);
    }

    [Theory]
    [InlineData("\uD842")]
    [InlineData("a\uDFB7")]
    public void OnlyCharacters_ペアになっていないサロゲートは失敗する(string value)
    {
        var all = CharacterKinds.AsciiPrintable | CharacterKinds.Hiragana | CharacterKinds.FullWidthKatakana | CharacterKinds.HalfWidthKatakana;

        Assert.True(new StringValidator("Name").OnlyCharacters(all).Check(value).IsFailure);
    }

    [Fact]
    public void OnlyCharacters_Noneは例外になる()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StringValidator("Name").OnlyCharacters(CharacterKinds.None));
    }

    [Theory]
    [InlineData("ok", true)]
    [InlineData("ng", false)]
    public void Must_述語の結果で検証する(string value, bool expected)
    {
        Assert.Equal(expected, new StringValidator("Name").Must(x => x == "ok").Check(value).IsSuccess);
    }

    [Fact]
    public void Must_nullの述語は例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => new StringValidator("Name").Must(null!));
    }

    [Fact]
    public void Check_ルール違反はValidationエラーとしてTargetを設定する()
    {
        var error = Assert.Single(new StringValidator("Name", target: "name").MaxLength(1).Check("ab").Errors);

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("name", error.Target);
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

    [Fact]
    public void Check_任意項目でnullの場合はルールをスキップする()
    {
        Assert.True(new NumberValidator<int>("Age").Min(0).Check(null).IsSuccess);
    }

    [Fact]
    public void Required_カスタムメッセージと検証時の表示名を使う()
    {
        var validator = new NumberValidator<int>("Age", target: "age").Required("{0}がありません");

        var error = Assert.Single(validator.Check(null, "年齢").Errors);

        Assert.Equal("年齢がありません", error.Message);
        Assert.Equal("age", error.Target);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public void Min_最小値以上であることを検証する(int value, bool expected)
    {
        Assert.Equal(expected, new NumberValidator<int>("Age").Min(10).Check(value).IsSuccess);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Max_最大値以下であることを検証する(int value, bool expected)
    {
        Assert.Equal(expected, new NumberValidator<int>("Age").Max(10).Check(value).IsSuccess);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Range_範囲内であることを検証する(int value, bool expected)
    {
        Assert.Equal(expected, new NumberValidator<int>("Age").Range(1, 10).Check(value).IsSuccess);
    }

    [Fact]
    public void Range_最小値と最大値が同じ場合はその値のみ許可する()
    {
        var validator = new NumberValidator<int>("Age").Range(5, 5);

        Assert.True(validator.Check(5).IsSuccess);
        Assert.True(validator.Check(4).IsFailure);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Positive_0より大きいことを検証する(int value, bool expected)
    {
        Assert.Equal(expected, new NumberValidator<int>("Age").Positive().Check(value).IsSuccess);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    public void NonNegative_0以上であることを検証する(int value, bool expected)
    {
        Assert.Equal(expected, new NumberValidator<int>("Age").NonNegative().Check(value).IsSuccess);
    }

    [Theory]
    [InlineData("-123.45", true)]
    [InlineData("-1234", false)]
    [InlineData("0", true)]
    public void Digits_負の値は絶対値の桁数で検証する(string value, bool expected)
    {
        var validator = new NumberValidator<decimal>("Price").Digits(3, 2);

        Assert.Equal(expected, validator.Check(decimal.Parse(value, CultureInfo.InvariantCulture)).IsSuccess);
    }

    [Theory]
    [InlineData("0.5", 0, 1, true)]
    [InlineData("1", 0, 1, false)]
    [InlineData("1.0", 1, 0, true)]
    [InlineData("1.5", 1, 0, false)]
    public void Digits_整数部の0と小数部末尾の0は桁数に数えない(string value, int integerDigits, int fractionDigits, bool expected)
    {
        var validator = new NumberValidator<decimal>("Price").Digits(integerDigits, fractionDigits);

        Assert.Equal(expected, validator.Check(decimal.Parse(value, CultureInfo.InvariantCulture)).IsSuccess);
    }

    [Fact]
    public void Digits_decimalの範囲を超える値は失敗する()
    {
        Assert.True(new NumberValidator<double>("Rate").Digits(30, 0).Check(double.MaxValue).IsFailure);
    }

    [Fact]
    public void Digits_負の桁数は例外になる()
    {
        var validator = new NumberValidator<decimal>("Price");

        Assert.Throws<ArgumentOutOfRangeException>(() => validator.Digits(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => validator.Digits(0, -1));
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void Must_述語の結果で検証する(int value, bool expected)
    {
        Assert.Equal(expected, new NumberValidator<int>("Age").Must(x => x % 2 == 0).Check(value).IsSuccess);
    }

    [Fact]
    public void Must_nullの述語は例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => new NumberValidator<int>("Age").Must(null!));
    }
}

public class DateValidatorTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now, TimeZoneInfo? localTimeZone = null) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => localTimeZone ?? TimeZoneInfo.Utc;
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

    private static DateOnly Jan(int day) => new(2026, 1, day);

    private static readonly DateTimeOffset Now = Clock.GetUtcNow();

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void DateOnlyFuture_今日より後であることを検証する(int day, bool expected)
    {
        Assert.Equal(expected, new DateOnlyValidator("StartDate").Future(Clock).Check(Jan(day)).IsSuccess);
    }

    [Fact]
    public void DateOnlyPastFuture_今日はTimeProviderのローカル時刻の日付で判定する()
    {
        // UTC では 1/10 だが、UTC+9 では 1/11 になる時刻
        var jst = TimeZoneInfo.CreateCustomTimeZone("UTC+9", TimeSpan.FromHours(9), "UTC+9", "UTC+9");
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 1, 10, 20, 0, 0, TimeSpan.Zero), jst);

        Assert.True(new DateOnlyValidator("BirthDate").Past(clock).Check(Jan(10)).IsSuccess);
        Assert.True(new DateOnlyValidator("StartDate").Future(clock).Check(Jan(11)).IsFailure);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public void DateOnlyMin_最小日付以降であることを検証する(int day, bool expected)
    {
        Assert.Equal(expected, new DateOnlyValidator("Date").Min(Jan(10)).Check(Jan(day)).IsSuccess);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void DateOnlyMax_最大日付以前であることを検証する(int day, bool expected)
    {
        Assert.Equal(expected, new DateOnlyValidator("Date").Max(Jan(10)).Check(Jan(day)).IsSuccess);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void DateOnlyRange_範囲内であることを検証する(int day, bool expected)
    {
        Assert.Equal(expected, new DateOnlyValidator("Date").Range(Jan(10), Jan(20)).Check(Jan(day)).IsSuccess);
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void DateOnlyMust_述語の結果で検証する(int day, bool expected)
    {
        Assert.Equal(expected, new DateOnlyValidator("Date").Must(x => x.Day % 2 == 0).Check(Jan(day)).IsSuccess);
    }

    [Fact]
    public void DateOnly_必須でnullの場合は失敗し任意項目ならルールをスキップする()
    {
        Assert.True(new DateOnlyValidator("Date").Required().Check(null).IsFailure);
        Assert.True(new DateOnlyValidator("Date").Min(Jan(10)).Check(null).IsSuccess);
    }

    [Fact]
    public void DateOnly_不正な引数は例外になる()
    {
        var validator = new DateOnlyValidator("Date");

        Assert.Throws<ArgumentOutOfRangeException>(() => validator.Range(Jan(20), Jan(10)));
        Assert.Throws<ArgumentNullException>(() => validator.Past(null!));
        Assert.Throws<ArgumentNullException>(() => validator.Future(null!));
        Assert.Throws<ArgumentNullException>(() => validator.Must(null!));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    public void DateTimeOffsetPast_現在時刻より前であることを検証する(int seconds, bool expected)
    {
        Assert.Equal(expected, new DateTimeOffsetValidator("At").Past(Clock).Check(Now.AddSeconds(seconds)).IsSuccess);
    }

    [Fact]
    public void DateTimeOffsetPastFuture_オフセットが異なっても同じ時点として比較する()
    {
        var sameInstant = Now.ToOffset(TimeSpan.FromHours(9));

        Assert.True(new DateTimeOffsetValidator("At").Past(Clock).Check(sameInstant).IsFailure);
        Assert.True(new DateTimeOffsetValidator("At").Future(Clock).Check(sameInstant).IsFailure);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    public void DateTimeOffsetMin_最小日時以降であることを検証する(int ticks, bool expected)
    {
        Assert.Equal(expected, new DateTimeOffsetValidator("At").Min(Now).Check(Now.AddTicks(ticks)).IsSuccess);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void DateTimeOffsetMax_最大日時以前であることを検証する(int ticks, bool expected)
    {
        Assert.Equal(expected, new DateTimeOffsetValidator("At").Max(Now).Check(Now.AddTicks(ticks)).IsSuccess);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void DateTimeOffsetRange_範囲内であることを検証する(int ticks, bool expected)
    {
        var validator = new DateTimeOffsetValidator("At").Range(Now, Now.AddTicks(10));

        Assert.Equal(expected, validator.Check(Now.AddTicks(ticks)).IsSuccess);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void DateTimeOffsetMust_述語の結果で検証する(int seconds, bool expected)
    {
        Assert.Equal(expected, new DateTimeOffsetValidator("At").Must(x => x == Now).Check(Now.AddSeconds(seconds)).IsSuccess);
    }

    [Fact]
    public void DateTimeOffset_必須でnullの場合は失敗し任意項目ならルールをスキップする()
    {
        Assert.True(new DateTimeOffsetValidator("At").Required().Check(null).IsFailure);
        Assert.True(new DateTimeOffsetValidator("At").Min(Now).Check(null).IsSuccess);
    }

    [Fact]
    public void DateTimeOffset_不正な引数は例外になる()
    {
        var validator = new DateTimeOffsetValidator("At");

        Assert.Throws<ArgumentOutOfRangeException>(() => validator.Range(Now, Now.AddTicks(-1)));
        Assert.Throws<ArgumentNullException>(() => validator.Past(null!));
        Assert.Throws<ArgumentNullException>(() => validator.Future(null!));
        Assert.Throws<ArgumentNullException>(() => validator.Must(null!));
    }
}
