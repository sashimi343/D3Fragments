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

        Assert.IsType<ArgumentException>(exception.InnerException);
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
}
