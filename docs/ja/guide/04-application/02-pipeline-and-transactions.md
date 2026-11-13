# Pipeline とトランザクション

`IRequestExecutor` は、Handler を Pipeline Behavior で包んで実行します。
Pipeline Behavior は、変更の保存やトランザクションなど、すべてのリクエストに共通する処理を受け持ちます。

## 標準の Pipeline Behavior

`AddD3FragmentsApplication` は、次の2つの Pipeline Behavior を登録します。

| Behavior | `Order` | 処理 |
|---|---:|---|
| `TransactionBehavior` | 200 | `ITransactionalCommand` の Command について、Handler の実行全体を明示的な DB トランザクションで囲む |
| `UnitOfWorkBehavior` | 300 | Query 以外のリクエストについて、Handler が正常に終わったら変更を保存する。例外がスローされたら変更を破棄する |

`Order` が小さい Behavior ほど外側で動きます。
そのため、`TransactionBehavior` を使う Command では、`UnitOfWorkBehavior` の保存もトランザクションに含まれます。

## 変更の保存

`UnitOfWorkBehavior` は、Handler が正常に終わった後に `IUnitOfWork.SaveChangesAsync` を1回呼びます。
Handler が例外をスローした場合や処理がキャンセルされた場合は、保存せずに変更を破棄し、例外をそのまま再スローします。

多くの Command はこの1回の保存で十分です。
`SaveChangesAsync` は、それだけで1つのトランザクションとして実行されるためです。

## 明示的なトランザクション

Handler の実行前からトランザクションを開始したい場合（行ロックを取る場合など）は、Command に `ITransactionalCommand` を追加します。

```csharp
// 座席を行ロックしてから予約する
public sealed record ReserveSeatCommand(UuidV7 SeatId, UuidV7 MemberId)
    : ICommand<UuidV7>, ITransactionalCommand;
```

`TransactionBehavior` は、Pipeline の残りの処理を `IUnitOfWork.ExecuteInTransactionAsync` の中で実行します。
例外がスローされなければ Commit し、スローされれば Rollback します。

## 独自の Pipeline Behavior を追加する

ロギングなどの共通処理は `IPipelineBehavior<TRequest, TResponse>` を実装して追加します。

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public int Order => 100;

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing {Request}", typeof(TRequest).Name);
        var response = await next(cancellationToken);
        logger.LogInformation("Executed {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

```csharp
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

実行順序は DI の登録順ではなく、`Order` の値で決まります。
上の例では `Order` が 100 なので、標準の Behavior より外側で動きます。

`next` を呼ぶと、Pipeline の次の処理（次の Behavior か Handler）が実行されます。

## 関連する API

- <xref:D3Fragments.Application.IPipelineBehavior`2>
- <xref:D3Fragments.Application.UnitOfWorkBehavior`2>
- <xref:D3Fragments.Application.TransactionBehavior`2>
- @D3Fragments.Application.ITransactionalCommand
- @D3Fragments.Application.IUnitOfWork
