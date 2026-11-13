# CSRF 対策

`UseCsrfProtection` は、指定したカスタムリクエストヘッダーがないリクエストを拒否するミドルウェアです。
Cookie で認証する Web API を、クロスサイトリクエストフォージェリ（CSRF）から守ります。

## 仕組み

ブラウザはほかのオリジンへのリクエストにカスタムヘッダーを付ける前に、CORS のプリフライトリクエストで送信先の許可を確認します。
CORS ポリシーで信頼できないオリジンを許可していなければ、ほかのサイトのページからはヘッダー付きのリクエストを送れません。
そのため、ヘッダーのないリクエストを拒否すれば、ほかのサイトから送られたリクエストを受け付けずに済みます。

ミドルウェアは次のリクエストだけを検査します。

- メソッドが GET・HEAD・OPTIONS・TRACE 以外
- パスが `BypassPaths` に含まれない

ヘッダーがないリクエストには、400 Bad Request を Problem Details 応答で返します。
ヘッダーの値は検査しません。

## 使い方

```csharp
var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// /api 配下だけに適用する
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api => api.UseCsrfProtection());
```

クライアントは安全でないメソッドのリクエストにヘッダーを付けます。

```javascript
await fetch("/api/holds/expire", {
  method: "POST",
  headers: { "X-Requested-By": "my-app" },
});
```

Razor Pages のフォームは ASP.NET Core 標準の偽造防止トークンで守られています。
フォームのページにはこのミドルウェアを適用しないでください。適用すると、ヘッダーを付けられないフォームの送信が拒否されます。

## 設定

`CsrfProtectionOptions` で設定します。

```csharp
builder.Services.Configure<CsrfProtectionOptions>(options =>
{
    options.RequiredHeaderName = "X-CSRF";
    options.BypassPaths.Add("/api/webhooks");
});
```

| プロパティ | 内容 | 既定値 |
|---|---|---|
| `RequiredHeaderName` | 必須にするヘッダーの名前 | `X-Requested-By` |
| `BypassPaths` | 検査しないパスのプレフィックス（Webhook のエンドポイントなど）。パスのセグメント単位で照合する | なし |

OpenAPI ドキュメントにこのヘッダーを載せる方法は、[OpenAPI](04-openapi.md) を参照してください。

## 関連する API

- @D3Fragments.AspNetCore.CsrfProtectionApplicationBuilderExtensions
- @D3Fragments.AspNetCore.CsrfProtectionOptions
