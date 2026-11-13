# OpenAPI

`Sashimi343.D3Fragments.AspNetCore.OpenApi` パッケージは、`Microsoft.AspNetCore.OpenApi` が生成する OpenAPI ドキュメントを、D3Fragments の型の JSON 表現に合わせます。

## 使い方

`AddOpenApi` の設定で `AddD3Fragments` を呼びます。

```csharp
builder.Services.AddOpenApi(options => options.AddD3Fragments());
```

次のスキーマは常に変換します。

| 型 | 変換後のスキーマ |
|---|---|
| `UuidV7` | `uuid` 形式の文字列 |
| 値オブジェクト | ラップしている型（`string` など）のスキーマ |
| 型安全な列挙型 | JSON オプションに登録した `TypeSafeEnumJsonConverterFactory` に合わせて、キーまたは名前のスキーマにし、定義された値を列挙する。コンバーターを登録していなければ変換しない |

値オブジェクトの制約（最大長など）は `ValidateValue` のコードで実装されているため、スキーマには反映されません。

## オプション

```csharp
builder.Services.AddOpenApi(options => options.AddD3Fragments(o =>
{
    o.DescribeEnumValues = true;
    o.IncludeCsrfProtectionHeader = true;
}));
```

| プロパティ | 内容 | 既定値 |
|---|---|---|
| `DescribeEnumValues` | 列挙型のスキーマの説明に、定義された値の一覧（数値・名前・表示名）を追記する | `false` |
| `IncludeCsrfProtectionHeader` | [CSRF 対策](03-csrf.md) のミドルウェアが必須にするヘッダーを、検査の対象になる操作に追加する。ヘッダー名・検査しないメソッド・除外するパスは `CsrfProtectionOptions` に従う | `false` |

## 関連する API

- @D3Fragments.AspNetCore.OpenApi.D3FragmentsOpenApiOptionsExtensions
- @D3Fragments.AspNetCore.OpenApi.D3FragmentsOpenApiOptions
