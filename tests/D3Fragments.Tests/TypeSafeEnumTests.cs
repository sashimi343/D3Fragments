using System.Text.Json;

namespace D3Fragments.Tests;

public sealed class Currency : TypeSafeEnum<Currency, string>
{
    public static readonly Currency Jpy = new("JPY", "Japanese Yen");
    public static readonly Currency Usd = new("USD", "US Dollar");

    private Currency(string key, string name) : base(key, name)
    {
    }
}

// 値ごとに振る舞いを変えるパターン（抽象クラス＋privateな入れ子クラス）
public abstract class Shipping : TypeSafeEnum<Shipping, int>
{
    public static readonly Shipping Standard = new StandardShipping();
    public static readonly Shipping Express = new ExpressShipping();

    private Shipping(int key) : base(key)
    {
    }

    public abstract int Fee(int weight);

    private sealed class StandardShipping() : Shipping(1)
    {
        public override int Fee(int weight) => 500;
    }

    private sealed class ExpressShipping() : Shipping(2)
    {
        public override int Fee(int weight) => 500 + (weight * 10);
    }
}

// 値をprivateフィールドにしてプロパティで公開するパターン
public sealed class Priority : TypeSafeEnum<Priority, int>
{
    private static readonly Priority LowValue = new(1);
    private static readonly Priority HighValue = new(2);

    private Priority(int key) : base(key)
    {
    }

    public static Priority Low => LowValue;

    public static Priority High => HighValue;
}

public sealed class DuplicatedKey : TypeSafeEnum<DuplicatedKey, int>
{
    public static readonly DuplicatedKey A = new(1, "A");
    public static readonly DuplicatedKey B = new(1, "B");

    private DuplicatedKey(int key, string name) : base(key, name)
    {
    }
}

public sealed class DuplicatedName : TypeSafeEnum<DuplicatedName, int>
{
    public static readonly DuplicatedName A = new(1, "A");
    public static readonly DuplicatedName B = new(2, "A");

    private DuplicatedName(int key, string name) : base(key, name)
    {
    }
}

public sealed class EmptyName : TypeSafeEnum<EmptyName, int>
{
    public static readonly EmptyName A = new(1, "");

    private EmptyName(int key, string name) : base(key, name)
    {
    }
}

public sealed record Order(Currency Currency, Shipping Shipping);

public class TypeSafeEnumBaseTests
{
    [Fact]
    public void Values_定義順ですべての値を返す()
    {
        Assert.Equal([Currency.Jpy, Currency.Usd], Currency.Values);
        Assert.Equal(["JPY", "USD"], Currency.Keys);
    }

    [Fact]
    public void Values_値をprivateフィールドで定義していても初期化される()
    {
        // 値へ一度もアクセスしていない状態でも取得できること
        Assert.Equal(2, Priority.Count);
        Assert.Same(Priority.High, Priority.Parse(2));
    }

    [Fact]
    public void Values_読み取り専用で変更できない()
    {
        Assert.False(Currency.Values is List<Currency>);
    }

    [Fact]
    public void Parse_キーに対応する同一インスタンスを返す()
    {
        Assert.Same(Currency.Usd, Currency.Parse("USD"));
        Assert.True(Currency.Parse("JPY") == Currency.Jpy);
    }

    [Fact]
    public void Parse_未定義のキーは例外になる()
    {
        Assert.Throws<ArgumentException>(() => Currency.Parse("EUR"));
    }

    [Fact]
    public void ParseAsResult_未定義のキーはバリデーションエラーを返す()
    {
        var result = Currency.ParseAsResult("EUR");

        Assert.Equal(ErrorType.Validation, Assert.Single(result.Errors).Type);
    }

    [Fact]
    public void 値ごとに異なる振る舞いを持てる()
    {
        Assert.Equal(500, Shipping.Parse(1).Fee(10));
        Assert.Equal(600, Shipping.Express.Fee(10));
        Assert.Equal("2", Shipping.Express.Name);
    }

    [Fact]
    public void キーが重複する定義は初期化時に例外になる()
    {
        var exception = Assert.Throws<TypeInitializationException>(() => DuplicatedKey.Values);

        Assert.Equal("key", Assert.IsType<ArgumentException>(exception.InnerException).ParamName);
    }

    [Fact]
    public void 名前が重複する定義は初期化時に例外になる()
    {
        var exception = Assert.Throws<TypeInitializationException>(() => DuplicatedName.Values);

        Assert.Equal("name", Assert.IsType<ArgumentException>(exception.InnerException).ParamName);
    }

    [Fact]
    public void 空の名前の定義は初期化時に例外になる()
    {
        var exception = Assert.Throws<TypeInitializationException>(() => EmptyName.Values);

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void ParseAsResult_定義済みのキーは成功結果を返す()
    {
        var result = Currency.ParseAsResult("JPY");

        Assert.True(result.IsSuccess);
        Assert.Same(Currency.Jpy, result.Value);
    }

    [Fact]
    public void TryParse_nullのキーは例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => Currency.TryParse(null!, out _));
    }

    [Fact]
    public void ToString_名前を返す()
    {
        Assert.Equal("US Dollar", Currency.Usd.ToString());
        Assert.Equal("1", Shipping.Standard.ToString());
    }
}

public class TypeSafeEnumJsonConverterFactoryTests
{
    private static JsonSerializerOptions Options(TypeSafeEnumJsonFormat format) =>
        new() { Converters = { new TypeSafeEnumJsonConverterFactory(format) } };

    [Fact]
    public void 既定ではキーで相互変換する()
    {
        var options = Options(TypeSafeEnumJsonFormat.Key);

        var json = JsonSerializer.Serialize(new Order(Currency.Usd, Shipping.Express), options);

        Assert.Equal("""{"Currency":"USD","Shipping":2}""", json);
        Assert.Equal(new Order(Currency.Usd, Shipping.Express), JsonSerializer.Deserialize<Order>(json, options));
    }

    [Fact]
    public void 名前指定時は名前で相互変換する()
    {
        var options = Options(TypeSafeEnumJsonFormat.Name);

        var json = JsonSerializer.Serialize(new Order(Currency.Jpy, Shipping.Standard), options);

        Assert.Equal("""{"Currency":"Japanese Yen","Shipping":"1"}""", json);
        Assert.Equal(new Order(Currency.Jpy, Shipping.Standard), JsonSerializer.Deserialize<Order>(json, options));
    }

    [Theory]
    [InlineData(TypeSafeEnumJsonFormat.Key, """{"Currency":"EUR","Shipping":1}""")]
    [InlineData(TypeSafeEnumJsonFormat.Name, """{"Currency":"Euro","Shipping":"1"}""")]
    public void 未定義の値はJsonExceptionになる(TypeSafeEnumJsonFormat format, string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Order>(json, Options(format)));
    }

    [Fact]
    public void nullはnullとして変換する()
    {
        var options = Options(TypeSafeEnumJsonFormat.Key);

        Assert.Null(JsonSerializer.Deserialize<Currency>("null", options));
    }

    [Fact]
    public void Constructor_既定はキー形式で未定義の形式は例外になる()
    {
        Assert.Equal(TypeSafeEnumJsonFormat.Key, new TypeSafeEnumJsonConverterFactory().Format);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TypeSafeEnumJsonConverterFactory((TypeSafeEnumJsonFormat)99));
    }

    [Fact]
    public void CanConvert_TypeSafeEnumの定義型のみ変換対象とする()
    {
        var factory = new TypeSafeEnumJsonConverterFactory();

        Assert.True(factory.CanConvert(typeof(Currency)));
        Assert.True(factory.CanConvert(typeof(Shipping)));
        Assert.False(factory.CanConvert(Shipping.Express.GetType()));
        Assert.False(factory.CanConvert(typeof(string)));
    }

    [Fact]
    public void CreateConverter_変換対象外の型は例外になる()
    {
        Assert.Throws<ArgumentException>(() => new TypeSafeEnumJsonConverterFactory().CreateConverter(typeof(string), JsonSerializerOptions.Default));
    }

    [Fact]
    public void 名前指定時は文字列以外のトークンはJsonExceptionになる()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Currency>("1", Options(TypeSafeEnumJsonFormat.Name)));
    }
}
