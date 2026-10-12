namespace D3Fragments.AspNetCore.OpenApi;

/// <summary>
///     <para xml:lang="en">Provides options for the OpenAPI document transformers added by <see cref="D3FragmentsOpenApiOptionsExtensions.AddD3Fragments"/>.</para>
///     <para xml:lang="ja"><see cref="D3FragmentsOpenApiOptionsExtensions.AddD3Fragments"/>で追加するOpenAPIドキュメントのトランスフォーマーのオプションを提供する。</para>
/// </summary>
public sealed class D3FragmentsOpenApiOptions
{
    /// <summary>
    ///     <para xml:lang="en">Gets or sets a value indicating whether to append the list of defined values (numeric value, name and display name) to the description of enum schemas.</para>
    ///     <para xml:lang="ja">列挙型のスキーマの説明に、定義値の一覧（数値・名前・表示名）を追記するかどうかを取得または設定する。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en"><see langword="true"/> to append the list. The default is <see langword="false"/>.</para>
    ///     <para xml:lang="ja">追記する場合は<see langword="true"/>。既定値は<see langword="false"/>。</para>
    /// </value>
    public bool DescribeEnumValues { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets a value indicating whether to add the header required by the CSRF protection middleware (<see cref="CsrfProtectionApplicationBuilderExtensions.UseCsrfProtection"/>) to the operations it checks.</para>
    ///     <para xml:lang="ja">CSRF対策ミドルウェア（<see cref="CsrfProtectionApplicationBuilderExtensions.UseCsrfProtection"/>）が必須とするヘッダを、検証対象の操作へ追加するかどうかを取得または設定する。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en"><see langword="true"/> to add the header. The default is <see langword="false"/>. The header name, safe methods and bypass paths follow <see cref="CsrfProtectionOptions"/>.</para>
    ///     <para xml:lang="ja">追加する場合は<see langword="true"/>。既定値は<see langword="false"/>。ヘッダ名・検証しないメソッド・除外パスは<see cref="CsrfProtectionOptions"/>に従う。</para>
    /// </value>
    public bool IncludeCsrfProtectionHeader { get; set; }
}
