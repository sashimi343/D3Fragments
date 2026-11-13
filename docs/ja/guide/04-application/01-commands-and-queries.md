# Command と Query

ユースケースはリクエストと Handler の組で実装します。

- Command（`ICommand<TResponse>`）：状態を変更するリクエスト。Handler が正常に終わると、変更が保存されます。
- Query（`IQuery<TResponse>`）：読み取りだけのリクエスト。変更は保存されません。

## リクエストを定義する

リクエストは `ICommand<TResponse>` か `IQuery<TResponse>` のどちらか一方を実装します。
`TResponse` は、成功したときに返す値の型です。

```csharp
public sealed class RegisterBookCommand : ICommand<Book>
{
    public required string Isbn { get; init; }

    public required string Title { get; init; }

    public required string Author { get; init; }
}

public sealed record GetBookQuery(UuidV7 BookId) : IQuery<BookDto>;
```

`IRequest<TResponse>` を直接実装しないでください。
D3Fragments は、リクエスト型が `IQuery` かどうかで変更を保存するかを決めます。
どちらとも判断できないリクエストは、Handler の登録時に例外になり、アナライザも警告します（[DFR0016](../../analyzers/DFR0016.md)）。

## Handler を実装する

`IRequestHandler<TRequest, TResponse>` を実装します。
Handler はリポジトリから集約を取得し、集約の操作を呼び出します。

```csharp
public sealed class DeleteBookCommandHandler(IBookRepository books)
    : IRequestHandler<DeleteBookCommand, Book>
{
    public async Task<Book> HandleAsync(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        Book book = await books.FindAsync(request.BookId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Book));
        books.Delete(book.IssueDeletionToken());
        return book;
    }
}
```

Handler の書き方には、次のルールがあります。

- 保存しない：Handler の中で `SaveChangesAsync` を呼んだり、トランザクションを操作したりしません。保存とトランザクションは Pipeline Behavior がまとめて行います（[DFR0012](../../analyzers/DFR0012.md)）。
- 失敗は例外で表す：失敗した場合は `DomainException` をスローします。応答の型に失敗を含める（`Result` を返すなど）必要はありません。Pipeline は、例外がスローされたかどうかだけで変更を保存するかを決めます。

## リクエストを実行する

リクエストは `IRequestExecutor<TRequest, TResponse>` を通して実行します。
Handler を直接呼ぶと Pipeline Behavior が動かず、変更が保存されません。

```csharp
[ApiController]
[Route("api/holds")]
public sealed class HoldsController(IRequestExecutor<ExpireHoldsCommand, int> expireHolds) : ControllerBase
{
    [HttpPost("expire")]
    public async Task<IActionResult> ExpireAsync(CancellationToken cancellationToken)
    {
        int processedCount = await expireHolds.ExecuteAsync(new ExpireHoldsCommand(), cancellationToken);
        return Ok(new { processedCount });
    }
}
```

Controller などのプレゼンテーション層では、ドメイン層のクラスをコンストラクタで受け取らず、`IRequestExecutor` を受け取ります（[DFR0011](../../analyzers/DFR0011.md)）。

### 失敗を Result で受け取る

`TryExecuteAsync` 拡張メソッドを使うと、Handler がスローした `DomainException` を失敗の `Result<TResponse>` として受け取れます。
`try-catch` を書かずに済み、`DomainException` 以外の例外を誤って握りつぶすこともありません。

```csharp
Result<Book> result = await registerBook.TryExecuteAsync(command, cancellationToken);
if (result.IsFailure)
{
    ModelState.AddDomainErrors(result.Errors);
    return Page();
}
```

ASP.NET Core 連携を使う場合は、`DomainException` をそのままスローしてエラー応答に変換させる方法もあります（[エラー応答](../06-aspnetcore/01-error-responses.md) を参照）。

## 関連する API

- <xref:D3Fragments.Application.ICommand`1>
- <xref:D3Fragments.Application.IQuery`1>
- <xref:D3Fragments.Application.IRequestHandler`2>
- <xref:D3Fragments.Application.IRequestExecutor`2>
- @D3Fragments.Application.RequestExecutorExtensions
