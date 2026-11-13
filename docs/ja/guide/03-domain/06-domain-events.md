# ドメインイベント

ドメインイベントはドメインで起きた出来事（「資料が取り置きされた」など）を表すオブジェクトです。
集約の操作の結果として発行し、ほかの集約の更新や通知などの後続の処理につなげます。

## イベントを定義する

`IDomainEvent` を実装します。
`OccurredAt` には、出来事が起きた日時を設定します。

```csharp
using D3Fragments;

public sealed record BookPlacedOnHoldDomainEvent(DateTimeOffset OccurredAt, UuidV7 BookId, UuidV7 MemberId)
    : IDomainEvent;
```

## イベントを発行する

ドメインイベントを発行できるのは集約ルートだけです。
集約ルートのメソッドの中で `AddDomainEvent` を呼びます。

```csharp
public sealed class Book : AggregateRoot
{
    private void HoldForNextReservation(DateOnly today, DateTimeOffset now)
    {
        // ...
        Status = BookStatus.OnHold;
        AddDomainEvent(new BookPlacedOnHoldDomainEvent(now, Id, nextReservation.MemberId));
    }
}
```

`AddDomainEvent` は、イベントを集約の中に記録するだけです。
この時点では、どのハンドラも呼ばれません。

## イベントが処理されるタイミング

記録したイベントは、Command の完了時に Unit of Work が変更を保存するときに処理します。

1. 変更の保存の前に、Commit 前のハンドラ（`IBeforeCommitDomainEventHandler<TEvent>`）を呼ぶ。ハンドラの変更は業務データと同じトランザクションで保存される
2. 変更を保存し、Commit する
3. Commit の後に、Commit 後のハンドラ（`IAfterCommitDomainEventHandler<TEvent>`）を呼ぶ
4. 集約に記録されたイベントを消す

Handler が例外をスローして変更が破棄された場合、イベントも破棄され、どのハンドラも呼ばれません。

ハンドラの実装方法は [ドメインイベントハンドラ](../04-application/03-domain-event-handlers.md) を参照してください。

## 関連する API

- @D3Fragments.IDomainEvent
- @D3Fragments.IHasDomainEvents
- <xref:D3Fragments.AggregateRoot`1>
