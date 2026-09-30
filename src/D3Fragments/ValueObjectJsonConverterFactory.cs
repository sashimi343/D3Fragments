using System.Text.Json;
using System.Text.Json.Serialization;

namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">A <see cref="JsonConverterFactory"/> that converts every <see cref="ValueObjectBase{TSelf, TPrimitive}"/>-derived type to and from its primitive value in JSON. Reading a value that fails validation throws a <see cref="JsonException"/> whose message is the validation error message.</para>
///     <para xml:lang="ja"><see cref="ValueObjectBase{TSelf, TPrimitive}"/>の派生型すべてを、プリミティブ値としてJSONと相互変換する<see cref="JsonConverterFactory"/>。バリデーションに失敗する値を読み取った場合は、バリデーションエラーのメッセージを持つ<see cref="JsonException"/>をスローする。</para>
/// </summary>
/// <example>
/// <code>
/// options.Converters.Add(new ValueObjectJsonConverterFactory()); // {"email":"foo@example.com"}
/// </code>
/// </example>
public sealed class ValueObjectJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => FindPrimitiveType(typeToConvert) is not null;

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var primitiveType = FindPrimitiveType(typeToConvert) ?? throw new ArgumentException(null, nameof(typeToConvert));
        var converterType = typeof(ValueObjectJsonConverter<,>).MakeGenericType(typeToConvert, primitiveType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    // ValueObjectBase<TSelf, TPrimitive>の派生型で、かつ変換対象がTSelf自身の場合のみ対象とする
    private static Type? FindPrimitiveType(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ValueObjectBase<,>))
            {
                var arguments = baseType.GetGenericArguments();
                return arguments[0] == type ? arguments[1] : null;
            }
        }
        return null;
    }

    private sealed class ValueObjectJsonConverter<TSelf, TPrimitive> : JsonConverter<TSelf>
        where TSelf : ValueObjectBase<TSelf, TPrimitive>, IValueObject<TSelf, TPrimitive>
    {
        public override TSelf Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var primitive = JsonSerializer.Deserialize<TPrimitive>(ref reader, options) ?? throw new JsonException();
            var result = ValueObjectBase<TSelf, TPrimitive>.TryCreate(primitive);
            return result.IsSuccess ? result.Value : throw new JsonException(result.GetFirstErrorMessage());
        }

        public override void Write(Utf8JsonWriter writer, TSelf value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Value, options);
    }
}
