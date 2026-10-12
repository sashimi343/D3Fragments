using Microsoft.AspNetCore.OpenApi;

namespace D3Fragments.AspNetCore.OpenApi;

/// <summary>
///     <para xml:lang="en">Provides extension methods that add the D3Fragments transformers to <see cref="OpenApiOptions"/>.</para>
///     <para xml:lang="ja"><see cref="OpenApiOptions"/>にD3Fragmentsのトランスフォーマーを追加する拡張メソッドを提供する。</para>
/// </summary>
public static class D3FragmentsOpenApiOptionsExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Adds transformers that make the OpenAPI document match the JSON representation of the D3Fragments types.</para>
    ///     <para xml:lang="ja">OpenAPIドキュメントをD3Fragmentsの型のJSON表現に合わせるトランスフォーマーを追加する。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">The following schemas are always transformed. <see cref="UuidV7"/> becomes a string in the <c>uuid</c> format. A value object (<see cref="ValueObjectBase{TSelf, TPrimitive}"/>) becomes the schema of its primitive type. A <see cref="TypeSafeEnum{TSelf, TKey}"/> becomes the schema of its key or name with the defined values listed, according to the <see cref="TypeSafeEnumJsonConverterFactory"/> registered in the JSON options (unchanged when none is registered). Constraints of value objects (maximum length etc.) are not reflected because they are implemented in code. Other transformations are enabled by <see cref="D3FragmentsOpenApiOptions"/>.</para>
    ///     <para xml:lang="ja">以下のスキーマは常に変換する。<see cref="UuidV7"/>は<c>uuid</c>形式の文字列にする。値オブジェクト（<see cref="ValueObjectBase{TSelf, TPrimitive}"/>）はそのプリミティブ型のスキーマにする。<see cref="TypeSafeEnum{TSelf, TKey}"/>は、JSONオプションに登録した<see cref="TypeSafeEnumJsonConverterFactory"/>に従い、キーまたは名前のスキーマにして定義値を列挙する（登録していない場合は変換しない）。値オブジェクトの制約（最大長等）はコードで実装されているため反映しない。その他の変換は<see cref="D3FragmentsOpenApiOptions"/>で有効にする。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddOpenApi(options => options.AddD3Fragments(o =>
    /// {
    ///     o.DescribeEnumValues = true;
    ///     o.IncludeCsrfProtectionHeader = true;
    /// }));
    /// </code>
    /// </example>
    /// <param name="options">
    ///     <para xml:lang="en">The OpenAPI options of the document.</para>
    ///     <para xml:lang="ja">ドキュメントのOpenAPIオプション。</para>
    /// </param>
    /// <param name="configure">
    ///     <para xml:lang="en">The delegate that configures <see cref="D3FragmentsOpenApiOptions"/>. The defaults are used when <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><see cref="D3FragmentsOpenApiOptions"/>を構成するデリゲート。<see langword="null"/>の場合は既定値を使用する。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same OpenAPI options for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じOpenAPIオプション。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="options"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="options"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static OpenApiOptions AddD3Fragments(this OpenApiOptions options, Action<D3FragmentsOpenApiOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var d3Options = new D3FragmentsOpenApiOptions();
        configure?.Invoke(d3Options);

        options.AddSchemaTransformer((schema, context, cancellationToken) =>
            D3FragmentsSchemaTransformer.TransformAsync(schema, context, d3Options.DescribeEnumValues, cancellationToken));
        if (d3Options.IncludeCsrfProtectionHeader)
        {
            options.AddOperationTransformer(CsrfProtectionHeaderTransformer.TransformAsync);
        }
        return options;
    }
}
