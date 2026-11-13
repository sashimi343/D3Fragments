# ASP.NET Core 連携

`Sashimi343.D3Fragments.AspNetCore` パッケージはプレゼンテーション層で D3Fragments を使うための機能を提供します。
OpenAPI ドキュメントを生成する場合は、`Sashimi343.D3Fragments.AspNetCore.OpenApi` パッケージも使います。

| ページ | 内容 |
|---|---|
| [エラー応答](01-error-responses.md) | `DomainException` と失敗した `Result` を Problem Details 応答に変換する |
| [Razor Pages](02-razor-pages.md) | ドメインのエラーを `ModelState` に反映する |
| [CSRF 対策](03-csrf.md) | カスタムヘッダーを必須にして、Cookie 認証の API を守る |
| [OpenAPI](04-openapi.md) | OpenAPI ドキュメントのスキーマを D3Fragments の型に合わせる |

## サービスの登録

```csharp
builder.Services.AddD3FragmentsAspNetCore();

var app = builder.Build();

app.UseExceptionHandler();
```

`AddD3FragmentsAspNetCore` は、`DomainException` を Problem Details 応答に変換する例外ハンドラと、値オブジェクトの JSON コンバーターなどを登録します。
例外ハンドラを有効にするには、`app.UseExceptionHandler()` を呼んでください。
`DomainException` 以外の例外は ASP.NET Core の標準の例外処理に任せます。
登録されるサービスの一覧は [サービスの登録](../02-service-registration.md) を参照してください。
