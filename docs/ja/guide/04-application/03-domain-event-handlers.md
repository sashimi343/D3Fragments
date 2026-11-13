# ドメインイベントハンドラ

ドメインイベントハンドラは、集約が発行した [ドメインイベント](../03-domain/06-domain-events.md) を受け取って、後続の処理を行います。
ハンドラには、呼ばれるタイミングが違う2種類があります。

| インターフェース | 呼ばれるタイミング | 用途 |
|---|---|---|
| `IBeforeCommitDomainEventHandler<TEvent>` | 変更の保存の前（同じトランザクション内） | ほかの集約の更新など、DB の更新を伴う処理 |
| `IAfterCommitDomainEventHandler<TEvent>` | Commit の成功後 | メールの送信や外部 API の呼び出しなど、DB の外への入出力 |

ハンドラは `AddD3FragmentsApplication` に渡したアセンブリから自動で登録されます。

## Commit 前のハンドラ

ハンドラの中での変更は業務データと一緒に保存されます。
ハンドラか保存のどちらかが失敗すると、まとめてロールバックされます。
ハンドラがスローした例外は Command の呼び出し元に伝わります。

```csharp
public sealed class SuspendMemberOnOverdueReturnHandler(IMemberRepository members)
    : IBeforeCommitDomainEventHandler<BookReturnedDomainEvent>
{
    public async Task HandleAsync(BookReturnedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ReturnedOn <= domainEvent.DueDate)
        {
            return;
        }

        Member member = await members.FindAsync(domainEvent.MemberId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Member));
        int overdueDays = domainEvent.ReturnedOn.DayNumber - domainEvent.DueDate.DayNumber;
        member.SuspendLoan(domainEvent.ReturnedOn, overdueDays);
    }
}
```

ハンドラが変更した集約が、さらに新しいイベントを発行することもあります。
その場合、新しいイベントの Commit 前のハンドラも、続けて呼ばれます。
ハンドラ同士がイベントを循環して発行し続けないように、繰り返しは最大10回までです。
それを超えると `InvalidOperationException` がスローされます。

## Commit 後のハンドラ

業務データはすでに確定しているため、ハンドラがスローした例外は Command の呼び出し元に伝わりません。
例外はエラーログに出力され、残りのハンドラは続けて呼ばれます。

```csharp
public sealed class NotifyBookPlacedOnHoldHandler(ILogger<NotifyBookPlacedOnHoldHandler> logger)
    : IAfterCommitDomainEventHandler<BookPlacedOnHoldDomainEvent>
{
    public Task HandleAsync(BookPlacedOnHoldDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // 会員に取り置きの完了を通知する
        logger.LogInformation("BookId '{BookId}' の取り置きが完了しました。", domainEvent.BookId);
        return Task.CompletedTask;
    }
}
```

Commit 後のハンドラではエンティティを直接変更しないでください。
変更しても保存されず、破棄されたことがエラーログに出力されます。
DB を更新したい場合は、ハンドラから別の Command を実行します。

既定のディスパッチャーは `cancellationToken` に `CancellationToken.None` を渡します。
確定した変更の後続処理を、呼び出し元のキャンセルで中断させないためです。

## ハンドラが呼ばれる条件

- イベントの実行時の型と、ハンドラの型引数 `TEvent` が一致するハンドラだけが呼ばれます。イベントの基底型やインターフェースを `TEvent` にしたハンドラは呼ばれません。
- 同じイベントに複数のハンドラがある場合は、登録順に1つずつ呼ばれます。

## ログのイベント ID

Commit 後の処理で出力するエラーログのイベント ID は、`D3FragmentsApplicationOptions` で変えられます。

| プロパティ | 既定値 | 出力する場面 |
|---|---|---|
| `AfterCommitHandlerFailedEventId` | 1001 | Commit 後のハンドラが例外をスローした |
| `AfterCommitChangesDiscardedEventId` | 1002 | Commit 後のハンドラによる変更を破棄した |

```csharp
builder.Services.AddD3FragmentsApplication(
    [typeof(RegisterBookCommand).Assembly],
    options => options.AfterCommitHandlerFailedEventId = new EventId(5001, "AfterCommitHandlerFailed"));
```

## 関連する API

- <xref:D3Fragments.Application.IBeforeCommitDomainEventHandler`1>
- <xref:D3Fragments.Application.IAfterCommitDomainEventHandler`1>
- @D3Fragments.Application.D3FragmentsApplicationOptions
