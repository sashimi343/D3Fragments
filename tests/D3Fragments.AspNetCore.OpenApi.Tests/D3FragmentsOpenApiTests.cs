using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.AspNetCore.OpenApi.Tests;

public sealed class Email : ValueObjectBase<Email, string>, IValueObject<Email, string>
{
    private Email(string value) : base(value)
    {
    }

    public static Email Create(string value) => new(value);

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;
        return true;
    }
}

public sealed class Quantity : ValueObjectBase<Quantity, int>, IValueObject<Quantity, int>
{
    private Quantity(int value) : base(value)
    {
    }

    public static Quantity Create(int value) => new(value);

    public static bool ValidateValue(int value, [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;
        return true;
    }
}

public sealed class Currency : TypeSafeEnum<Currency, string>
{
    public static readonly Currency Jpy = new("JPY", "Japanese Yen");
    public static readonly Currency Usd = new("USD", "US Dollar");

    private Currency(string key, string name) : base(key, name)
    {
    }
}

public sealed class Priority : TypeSafeEnum<Priority, int>
{
    public static readonly Priority Low = new(1, "Low");
    public static readonly Priority High = new(2, "High");

    private Priority(int key, string name) : base(key, name)
    {
    }
}

public enum Color
{
    Red = 1,

    [Display(Name = "青")]
    Blue = 2,
}

// 同じ型を複数のプロパティで使い、スキーマの共有時に説明が重複しないことも確認する
public sealed record Order(
    UuidV7 Id, Email Email, Email? SubEmail, Quantity Quantity, Currency Currency, Currency Currency2, Priority Priority, Color Color, Color Color2);

public class D3FragmentsOpenApiTests
{
    private static async Task<JsonElement> GetDocumentAsync(
        Action<D3FragmentsOpenApiOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddD3FragmentsAspNetCore();
        builder.Services.AddOpenApi(options => options.AddD3Fragments(configure));
        configureServices?.Invoke(builder.Services);

        await using var app = builder.Build();
        app.MapOpenApi();
        app.MapPost("/orders", (Order order) => order);
        app.MapGet("/orders", () => Array.Empty<Order>());
        app.MapPut("/webhook/orders", (Order order) => order);
        await app.StartAsync(TestContext.Current.CancellationToken);

        var json = await app.GetTestClient().GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static void AddTypeSafeEnumConverter(IServiceCollection services, TypeSafeEnumJsonFormat format) =>
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new TypeSafeEnumJsonConverterFactory(format)));

    private static JsonElement Schema(JsonElement document, string name) =>
        document.GetProperty("components").GetProperty("schemas").GetProperty(name);

    private static string[] Strings(JsonElement array) => [.. array.EnumerateArray().Select(x => x.ToString())];

    private static JsonElement[] Parameters(JsonElement document, string path, string method) =>
        document.GetProperty("paths").GetProperty(path).GetProperty(method).TryGetProperty("parameters", out var parameters)
            ? [.. parameters.EnumerateArray()]
            : [];

    [Fact]
    public async Task UuidV7はuuid形式の文字列になる()
    {
        var schema = Schema(await GetDocumentAsync(), "UuidV7");

        Assert.Equal("string", schema.GetProperty("type").GetString());
        Assert.Equal("uuid", schema.GetProperty("format").GetString());
    }

    [Fact]
    public async Task 値オブジェクトはプリミティブ型のスキーマになる()
    {
        var document = await GetDocumentAsync();

        var email = Schema(document, "Email");
        Assert.Equal("string", email.GetProperty("type").GetString());
        Assert.False(email.TryGetProperty("properties", out _));
        var quantity = Schema(document, "Quantity");
        // Webの既定のJSONオプションは数値を文字列からも読み取るため、intは["integer", "string"]になる（プリミティブのintと同じ表現）
        Assert.Contains("integer", Strings(quantity.GetProperty("type")));
        Assert.Equal("int32", quantity.GetProperty("format").GetString());
    }

    [Fact]
    public async Task TypeSafeEnum_キー表現ではキーを列挙し説明に対応表を付ける()
    {
        var document = await GetDocumentAsync(configureServices: s => AddTypeSafeEnumConverter(s, TypeSafeEnumJsonFormat.Key));

        var currency = Schema(document, "Currency");
        Assert.Equal("string", currency.GetProperty("type").GetString());
        Assert.Equal(["JPY", "USD"], Strings(currency.GetProperty("enum")));
        Assert.Equal("- JPY = Japanese Yen\n- USD = US Dollar", currency.GetProperty("description").GetString());
        Assert.False(currency.TryGetProperty("properties", out _));
        var priority = Schema(document, "Priority");
        Assert.Contains("integer", Strings(priority.GetProperty("type")));
        Assert.All(priority.GetProperty("enum").EnumerateArray(), x => Assert.Equal(JsonValueKind.Number, x.ValueKind));
        Assert.Equal(["1", "2"], Strings(priority.GetProperty("enum")));
    }

    [Fact]
    public async Task TypeSafeEnum_名前表現では名前を列挙する()
    {
        var document = await GetDocumentAsync(configureServices: s => AddTypeSafeEnumConverter(s, TypeSafeEnumJsonFormat.Name));

        var priority = Schema(document, "Priority");
        Assert.Equal("string", priority.GetProperty("type").GetString());
        Assert.Equal(["Low", "High"], Strings(priority.GetProperty("enum")));
        Assert.False(priority.TryGetProperty("description", out _));
    }

    [Fact]
    public async Task TypeSafeEnum_MVC用のJSONオプションだけに登録したコンバーターも参照する()
    {
        var document = await GetDocumentAsync(configureServices: s => s.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(
            o => o.JsonSerializerOptions.Converters.Add(new TypeSafeEnumJsonConverterFactory(TypeSafeEnumJsonFormat.Name))));

        Assert.Equal(["Japanese Yen", "US Dollar"], Strings(Schema(document, "Currency").GetProperty("enum")));
    }

    [Fact]
    public async Task TypeSafeEnum_コンバーター未登録なら変換しない()
    {
        var currency = Schema(await GetDocumentAsync(), "Currency");

        Assert.False(currency.TryGetProperty("enum", out _));
        Assert.True(currency.TryGetProperty("properties", out _));
    }

    [Fact]
    public async Task 列挙型_既定では説明を追記しない()
    {
        var color = Schema(await GetDocumentAsync(), "Color");

        Assert.False(color.TryGetProperty("description", out _));
    }

    [Fact]
    public async Task 列挙型_オプション有効時は定義値の一覧を説明に追記する()
    {
        var color = Schema(await GetDocumentAsync(o => o.DescribeEnumValues = true), "Color");

        Assert.Equal("- 1 = Red\n- 2 = Blue (青)", color.GetProperty("description").GetString());
    }

    [Fact]
    public async Task CSRFヘッダ_既定では追加しない()
    {
        Assert.Empty(Parameters(await GetDocumentAsync(), "/orders", "post"));
    }

    [Fact]
    public async Task CSRFヘッダ_オプション有効時は検証対象の操作にだけ追加する()
    {
        var document = await GetDocumentAsync(
            o => o.IncludeCsrfProtectionHeader = true,
            s => s.Configure<CsrfProtectionOptions>(o =>
            {
                o.RequiredHeaderName = "X-Custom";
                o.BypassPaths.Add("/webhook");
            }));

        var header = Assert.Single(Parameters(document, "/orders", "post"));
        Assert.Equal("X-Custom", header.GetProperty("name").GetString());
        Assert.Equal("header", header.GetProperty("in").GetString());
        Assert.True(header.GetProperty("required").GetBoolean());
        Assert.Equal("openapi", header.GetProperty("schema").GetProperty("default").GetString());
        Assert.Empty(Parameters(document, "/orders", "get"));
        Assert.Empty(Parameters(document, "/webhook/orders", "put"));
    }
}
