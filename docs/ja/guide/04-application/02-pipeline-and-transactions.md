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

リポジトリの `ExecuteImmediateAsync`（`ExecuteUpdate`・`ExecuteDelete`・`ExecuteSql*` などの即時の操作）を使う Command にも、`ITransactionalCommand` が必要です（[即時の操作を実行する](../05-efcore/02-repositories.md#即時の操作を実行する)）。

## 別のスコープで Command を実行する

1回の `IRequestExecutor` の呼び出しが、1つの保存の単位です。
Handler が例外をスローすると、その Command の変更はすべて破棄されます。
次のような処理は、`IIsolatedRequestExecutor<TRequest, TResponse>` を使って、呼び出し元とは別のスコープで実行します。

- 本体より前に、別のトランザクションで確定させたい前処理
- 本体が失敗しても残したい記録（外部 API の呼び出しの試行記録など）

`IIsolatedRequestExecutor` は、新しい DI スコープを作り、そのスコープの `IRequestExecutor` でリクエストを実行します。
`DbContext`・`IUnitOfWork` が呼び出し元とは別のインスタンスになるため、呼び出し元とは独立して保存・Commit されます。
Pipeline Behavior とドメインイベントの配信は、通常の実行と同じに動きます。
`IRequestExecutor` を継承しているため、`TryExecuteAsync` もそのまま使えます。

```csharp
public sealed class ChangePlanCommandHandler(
    IIsolatedRequestExecutor<StartPaymentAttemptCommand, UuidV7> startAttempt,
    IPaymentAttemptRepository attempts,
    IPaymentGateway paymentGateway)
    : IRequestHandler<ChangePlanCommand, Result>
{
    public async Task<Result> HandleAsync(ChangePlanCommand request, CancellationToken cancellationToken)
    {
        // 外部 API を呼ぶ前に、試行記録を確定させる。この後で本体が失敗しても残る
        UuidV7 attemptId = await startAttempt.ExecuteAsync(new StartPaymentAttemptCommand(request.SubscriptionId), cancellationToken);

        await paymentGateway.ChangePlanAsync(request.SubscriptionId, request.Plan, cancellationToken);

        // 本体の変更と同じトランザクションで、試行を完了にする
        PaymentAttempt attempt = await attempts.FindAsync(attemptId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PaymentAttempt));
        attempt.Complete();
        // 本体の変更
        return Result.Success();
    }
}
```

この例では、本体の保存や Commit が失敗すると、試行記録は未完了のまま残ります。
未完了のまま残った試行記録を検知すれば、外部 API と DB の不整合に気付けます。

別のスコープで実行するときは、次の点に注意してください。

- Command の Handler・Commit 前のドメインイベントハンドラから、Command を `IRequestExecutor` で実行しないでください（[DFR0018](../../analyzers/DFR0018.md)）。`DbContext` を共有するため、内側の Command の保存が、呼び出し元のまだ保存していない変更まで保存してしまいます。Commit 後のハンドラは Commit が済んでいるため、`IRequestExecutor` で実行できます
- Query の Handler からは、Command を実行しないでください（[DFR0019](../../analyzers/DFR0019.md)）
- 内側の Command からは、呼び出し元がまだ Commit していない行は見えません。それらの行への外部キーを持つ行を登録すると、制約違反になるか、ロックの解放を待ち続けます
- 呼び出し元のトランザクションがロックしている行を内側の Command が更新すると、内側の Command は待ち続けます（呼び出し元は内側の完了を待つため、処理が進みません）。ライブラリでは検出できません
- Scoped のサービスが持つ状態（キャッシュなど）は、新しいスコープに引き継がれません。`IHttpContextAccessor` は新しいスコープでも同じ `HttpContext` を返します
- 1回のリクエストで DB の接続を2本使います。接続プールの上限に注意してください
- Handler の単体テストでは、`IIsolatedRequestExecutor` をスパイに差し替えて、呼ばれたこととリクエストの内容を確認します。`TransactionalTestBase` の `DbContext` とは別の接続で実行され、テストのロールバックの対象にならないためです

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
- <xref:D3Fragments.Application.IIsolatedRequestExecutor`2>
