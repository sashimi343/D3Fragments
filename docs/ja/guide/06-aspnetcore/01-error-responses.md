# エラー応答

D3Fragments はドメインのエラーを [Problem Details](https://www.rfc-editor.org/rfc/rfc9457)（RFC 9457）形式の応答に変換します。
変換するのは、次の2つです。

- Handler などがスローした `DomainException`
- 失敗した `Result` / `Result<T>`

## DomainException を変換する

`AddD3FragmentsAspNetCore` を呼び、`app.UseExceptionHandler()` を有効にすると、エンドポイントの外へスローされた `DomainException` がエラー応答に変換されます。
エンドポイントでは、例外を捕まえる必要はありません。

```csharp
app.MapGet("/books/{id}", (UuidV7 id, IRequestExecutor<GetBookQuery, BookDto> executor, CancellationToken cancellationToken) =>
    executor.ExecuteAsync(new GetBookQuery(id), cancellationToken));
```

Handler が `EntityNotFoundException` をスローすると、次のような応答になります。

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "NotFound",
  "status": 404,
  "detail": "指定されたBookが見つかりません。",
  "traceId": "00-..."
}
```

| 項目 | 内容 |
|---|---|
| `status` | 先頭のエラーの `ErrorType` に対応する HTTP ステータスコード |
| `title` | 先頭のエラーの `ErrorType` の名前 |
| `detail` | すべてのエラーのメッセージを改行でつないだもの |
| `errorCode` | エラーコード（`DomainException.ErrorCode`）。エラーコードがない場合は出力しない |

`IProblemDetailsService` を通して書き出すため、`AddProblemDetails` の `CustomizeProblemDetails` で項目を追加することもできます。

## Result を変換する

Minimal API では `ToHttpResult` で `Result` を `IResult` に変換できます。

| メソッド | 成功時の応答 |
|---|---|
| `Result.ToHttpResult()` | 204 No Content |
| `Result<T>.ToHttpResult()` | 200 OK（値を本文に含める） |
| `Result<T>.ToHttpResult(onSuccess)` | `onSuccess` が返した応答 |

失敗時は `DomainException` と同じ形式のエラー応答になります。
`errorCode` には、先頭のエラーの `Error.Code` が入ります。

```csharp
app.MapPost("/books", async (RegisterBookRequest body, IRequestExecutor<RegisterBookCommand, Book> executor, CancellationToken cancellationToken) =>
{
    Result<Book> result = await executor.TryExecuteAsync(body.ToCommand(), cancellationToken);
    return result.ToHttpResult(book => TypedResults.Created($"/books/{book.Id}", BookDto.From(book)));
});
```

## ステータスコードとエラーコードを変える

`AddD3FragmentsAspNetCore` に渡すデリゲートで、`DomainProblemDetailsOptions` を設定します。

```csharp
builder.Services.AddD3FragmentsAspNetCore(options =>
{
    // InvalidState を 409 ではなく 422 にする
    options.StatusCodeMap[ErrorType.InvalidState] = StatusCodes.Status422UnprocessableEntity;

    // エラーコードを出力する項目名を変える
    options.ErrorCodeKey = "code";
});
```

| プロパティ | 内容 | 既定値 |
|---|---|---|
| `StatusCodeMap` | `ErrorType` から HTTP ステータスコードへの対応表。対応表にない `ErrorType` は 400 になる | [Result とエラー](../03-domain/04-result-and-errors.md) の表を参照 |
| `ErrorCodeKey` | エラーコードを出力する項目名 | `errorCode` |

`GetStatusCode(errorType)` で、対応表から HTTP ステータスコードを取得できます。
Razor Pages などで、自分で応答を組み立てる場合に使います。

## 関連する API

- @D3Fragments.AspNetCore.D3FragmentsServiceCollectionExtensions
- @D3Fragments.AspNetCore.ResultHttpExtensions
- @D3Fragments.AspNetCore.DomainProblemDetailsOptions
