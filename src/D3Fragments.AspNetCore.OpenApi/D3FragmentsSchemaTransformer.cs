using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace D3Fragments.AspNetCore.OpenApi;

internal static class D3FragmentsSchemaTransformer
{
    public static async Task TransformAsync(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context, bool describeEnumValues, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (type == typeof(UuidV7))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "uuid";
        }
        else if (FindSelfTypedBase(type, typeof(ValueObjectBase<,>)) is { } valueObjectBase)
        {
            await ReplaceWithSchemaOfAsync(schema, valueObjectBase.GetGenericArguments()[1], context, cancellationToken).ConfigureAwait(false);
        }
        else if (FindSelfTypedBase(type, typeof(TypeSafeEnum<,>)) is { } typeSafeEnumBase)
        {
            await TransformTypeSafeEnumAsync(schema, typeSafeEnumBase, context, cancellationToken).ConfigureAwait(false);
        }
        else if (describeEnumValues && type.IsEnum)
        {
            AppendDescription(schema, DescribeEnumValues(type));
        }
    }

    private static async Task TransformTypeSafeEnumAsync(
        OpenApiSchema schema, Type typeSafeEnumBase, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        // スキーマ生成はHTTP用のJSONオプションを使うが、コントローラーにだけコンバーターを登録している場合に備えてMVC用も参照する
        var factory = FindFactory(context.JsonTypeInfo.Options)
            ?? FindFactory(context.ApplicationServices.GetService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()?.Value.JsonSerializerOptions);
        if (factory is null)
        {
            return;
        }

        var keyProperty = typeSafeEnumBase.GetProperty(nameof(TypeSafeEnum<,>.Key))!;
        var nameProperty = typeSafeEnumBase.GetProperty(nameof(TypeSafeEnum<,>.Name))!;
        var values = ((IEnumerable)typeSafeEnumBase.GetProperty(nameof(TypeSafeEnum<,>.Values))!.GetValue(null)!)
            .Cast<object>()
            .Select(value => (Key: keyProperty.GetValue(value)!, Name: (string)nameProperty.GetValue(value)!))
            .ToList();

        if (factory.Format == TypeSafeEnumJsonFormat.Name)
        {
            ResetSchema(schema);
            schema.Type = JsonSchemaType.String;
            schema.Enum = [.. values.Select(value => (JsonNode)JsonValue.Create(value.Name))];
            return;
        }

        var keyType = typeSafeEnumBase.GetGenericArguments()[1];
        await ReplaceWithSchemaOfAsync(schema, keyType, context, cancellationToken).ConfigureAwait(false);
        schema.Enum = [.. values.Select(value => JsonSerializer.SerializeToNode(value.Key, keyType, context.JsonTypeInfo.Options)!)];
        AppendDescription(schema, values.Select(value => string.Create(CultureInfo.InvariantCulture, $"- {value.Key} = {value.Name}")));
    }

    private static IEnumerable<string> DescribeEnumValues(Type enumType)
    {
        var underlyingType = Enum.GetUnderlyingType(enumType);
        foreach (Enum value in Enum.GetValues(enumType))
        {
            var number = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            var name = value.ToString();
            var displayName = D3Fragments.Extensions.EnumExtensions.GetDisplayName(value);
            yield return displayName == name
                ? string.Create(CultureInfo.InvariantCulture, $"- {number} = {name}")
                : string.Create(CultureInfo.InvariantCulture, $"- {number} = {name} ({displayName})");
        }
    }

    private static TypeSafeEnumJsonConverterFactory? FindFactory(JsonSerializerOptions? options) =>
        options?.Converters.OfType<TypeSafeEnumJsonConverterFactory>().FirstOrDefault();

    private static async Task ReplaceWithSchemaOfAsync(
        OpenApiSchema schema, Type type, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var source = await context.GetOrCreateSchemaAsync(type, null, cancellationToken).ConfigureAwait(false);
        ResetSchema(schema);
        // 参照型のプリミティブ（string等）はnull許容で生成されるが、値オブジェクトの値・TypeSafeEnumのキーはnullにならない。
        // null許容かどうかは参照元のプロパティ側で表現される
        schema.Type = source.Type & ~JsonSchemaType.Null;
        schema.Format = source.Format;
        schema.Pattern = source.Pattern;
    }

    // カスタムコンバーターの型はオブジェクトとして解釈されている場合があるため、オブジェクト用の情報を消す
    private static void ResetSchema(OpenApiSchema schema)
    {
        schema.Properties = null;
        schema.Required = null;
        schema.AdditionalProperties = null;
    }

    private static void AppendDescription(OpenApiSchema schema, IEnumerable<string> lines)
    {
        var list = string.Join('\n', lines);
        schema.Description = string.IsNullOrEmpty(schema.Description) ? list : schema.Description + "\n\n" + list;
    }

    // ValueObjectBase<TSelf, ...>・TypeSafeEnum<TSelf, ...>の派生型で、かつTSelfが自身の場合のみ、その基底型を返す
    // （各JsonConverterFactoryの変換対象と揃える）
    private static Type? FindSelfTypedBase(Type type, Type openGenericBase)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == openGenericBase)
            {
                return baseType.GetGenericArguments()[0] == type ? baseType : null;
            }
        }
        return null;
    }
}
