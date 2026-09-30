using System.Text.Json;
using System.Text.Json.Serialization;

namespace D3Fragments;

// UuidV7に[JsonConverter]属性で適用するため、利用者によるOptionsへの登録は不要
internal sealed class UuidV7JsonConverter : JsonConverter<UuidV7>
{
    public override UuidV7 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(ExceptionMessages.JsonException_InvalidUuidV7);
        }

        return UuidV7.TryParse(reader.GetString(), null, out var value)
            ? value
            : throw new JsonException(ExceptionMessages.JsonException_InvalidUuidV7);
    }

    public override void Write(Utf8JsonWriter writer, UuidV7 value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
