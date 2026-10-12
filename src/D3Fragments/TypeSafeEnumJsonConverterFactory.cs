using System.Text.Json;
using System.Text.Json.Serialization;

namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Specifies how a <see cref="TypeSafeEnum{TSelf, TKey}"/> value is represented in JSON.</para>
///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>の値をJSONでどう表現するかを指定する。</para>
/// </summary>
public enum TypeSafeEnumJsonFormat
{
    /// <summary>
    ///     <para xml:lang="en">The key (<see cref="TypeSafeEnum{TSelf, TKey}.Key"/>), serialized as its own JSON type (e.g. a number for an <see cref="int"/> key).</para>
    ///     <para xml:lang="ja">キー（<see cref="TypeSafeEnum{TSelf, TKey}.Key"/>）。キーの型に応じたJSON型（<see cref="int"/>キーなら数値等）で表現する。</para>
    /// </summary>
    Key,

    /// <summary>
    ///     <para xml:lang="en">The name (<see cref="TypeSafeEnum{TSelf, TKey}.Name"/>), as a JSON string. Matching on read is case-sensitive.</para>
    ///     <para xml:lang="ja">名前（<see cref="TypeSafeEnum{TSelf, TKey}.Name"/>）。JSON文字列で表現する。読み取り時の照合は大文字小文字を区別する。</para>
    /// </summary>
    Name,
}

/// <summary>
///     <para xml:lang="en">A <see cref="JsonConverterFactory"/> that converts every <see cref="TypeSafeEnum{TSelf, TKey}"/>-derived type to and from JSON. Reading an undefined key or name throws a <see cref="JsonException"/>.</para>
///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>の派生型すべてをJSONと相互変換する<see cref="JsonConverterFactory"/>。未定義のキー・名前を読み取った場合は<see cref="JsonException"/>をスローする。</para>
/// </summary>
/// <example>
/// <code>
/// options.Converters.Add(new TypeSafeEnumJsonConverterFactory());                              // {"currency":"JPY"}（キー）
/// options.Converters.Add(new TypeSafeEnumJsonConverterFactory(TypeSafeEnumJsonFormat.Name));  // {"currency":"Japanese Yen"}（名前）
/// </code>
/// </example>
public sealed class TypeSafeEnumJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new factory that represents values by their key.</para>
    ///     <para xml:lang="ja">値をキーで表現するファクトリを初期化する。</para>
    /// </summary>
    public TypeSafeEnumJsonConverterFactory() : this(TypeSafeEnumJsonFormat.Key)
    {
    }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new factory with the specified representation.</para>
    ///     <para xml:lang="ja">表現方法を指定してファクトリを初期化する。</para>
    /// </summary>
    /// <param name="format">
    ///     <para xml:lang="en">How values are represented in JSON.</para>
    ///     <para xml:lang="ja">JSONでの値の表現方法。</para>
    /// </param>
    public TypeSafeEnumJsonConverterFactory(TypeSafeEnumJsonFormat format)
    {
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }
        Format = format;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the JSON representation of values.</para>
    ///     <para xml:lang="ja">値のJSON表現を取得する。</para>
    /// </summary>
    public TypeSafeEnumJsonFormat Format { get; }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => FindKeyType(typeToConvert) is not null;

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var keyType = FindKeyType(typeToConvert) ?? throw new ArgumentException(null, nameof(typeToConvert));
        var converterType = typeof(TypeSafeEnumJsonConverter<,>).MakeGenericType(typeToConvert, keyType);
        return (JsonConverter?)Activator.CreateInstance(converterType, Format);
    }

    // TypeSafeEnumBase<TSelf, TKey>の派生型で、かつ変換対象がTSelf自身の場合のみ対象とする
    // （値ごとの入れ子サブクラス等、TSelf以外の実行時型はJsonConverter<TSelf>で扱えないため）
    private static Type? FindKeyType(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(TypeSafeEnum<,>))
            {
                var arguments = baseType.GetGenericArguments();
                return arguments[0] == type ? arguments[1] : null;
            }
        }
        return null;
    }

    private sealed class TypeSafeEnumJsonConverter<TEnum, TKey>(TypeSafeEnumJsonFormat format) : JsonConverter<TEnum>
        where TEnum : TypeSafeEnum<TEnum, TKey>
        where TKey : notnull
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (format == TypeSafeEnumJsonFormat.Name)
            {
                var name = reader.TokenType == JsonTokenType.String ? reader.GetString()! : throw new JsonException();
                return TypeSafeEnum<TEnum, TKey>.TryParseByName(name, out var byName)
                    ? byName
                    : throw new JsonException(TypeSafeEnum<TEnum, TKey>.UndefinedNameMessage(name));
            }

            var key = JsonSerializer.Deserialize<TKey>(ref reader, options) ?? throw new JsonException();
            return TypeSafeEnum<TEnum, TKey>.TryParse(key, out var byKey)
                ? byKey
                : throw new JsonException(TypeSafeEnum<TEnum, TKey>.UndefinedKeyMessage(key));
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            if (format == TypeSafeEnumJsonFormat.Name)
            {
                writer.WriteStringValue(value.Name);
                return;
            }

            JsonSerializer.Serialize(writer, value.Key, options);
        }
    }
}
