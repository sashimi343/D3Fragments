using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace D3Fragments.Tests;

public class UuidV7JsonConverterTests
{
    [Fact]
    public void UuidV7_属性指定だけでUUID文字列として相互変換できる()
    {
        var id = UuidV7.NewUuidV7();

        var json = JsonSerializer.Serialize(new { Id = id });

        Assert.Equal($"{{\"Id\":\"{id}\"}}", json);
        Assert.Equal(id, JsonSerializer.Deserialize<UuidV7>($"\"{id}\""));
    }

    [Theory]
    [InlineData("\"not-a-uuid\"")]
    [InlineData("\"00000000-0000-4000-8000-000000000000\"")] // version 4
    [InlineData("123")]
    public void UuidV7_不正な値はJsonExceptionになる(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UuidV7>(json));
    }
}

public class ValueObjectJsonConverterFactoryTests
{
    public sealed class PositiveNumber : ValueObjectBase<PositiveNumber, int>, IValueObject<PositiveNumber, int>
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

    private static readonly JsonSerializerOptions Options = new() { Converters = { new ValueObjectJsonConverterFactory() } };

    [Fact]
    public void プリミティブ値として相互変換できる()
    {
        Assert.Equal("{\"Number\":3}", JsonSerializer.Serialize(new { Number = PositiveNumber.Create(3) }, Options));
        Assert.Equal(PositiveNumber.Create(3), JsonSerializer.Deserialize<PositiveNumber>("3", Options));
        Assert.Null(JsonSerializer.Deserialize<PositiveNumber>("null", Options));
    }

    [Fact]
    public void バリデーション失敗時はそのメッセージを持つJsonExceptionになる()
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PositiveNumber>("-1", Options));

        Assert.Contains("must be positive", ex.Message);
    }
}
