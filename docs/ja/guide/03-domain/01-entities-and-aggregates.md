# エンティティと集約

## 基底クラスの選び方

エンティティは ID で同一性を判定するオブジェクトです。
D3Fragments には、用途に応じて次の3種類の基底クラスがあります。

| 基底クラス | ID | 作成日時・更新日時・バージョン番号 | ドメインイベントの発行 | 用途 |
|---|:-:|:-:|:-:|---|
| `AggregateRoot` | ○ | ○ | ○ | 集約ルート |
| `Entity` | ○ | ○ | × | 集約の中の子エンティティ |
| `EntityBase` | ○ | × | × | 日時やバージョンを記録しないエンティティ |

それぞれ、ID の型を `UuidV7` に固定したクラスです。
ID の型を変えたい場合は、`AggregateRoot<TId>`・`Entity<TId>`・`EntityBase<TId>` を継承し、コンストラクタに ID を渡します。

```csharp
public sealed class Book : AggregateRoot
{
    private readonly List<Reservation> _reservations = [];

    private Book(Isbn isbn, BookTitle title, string author, BookStatus status)
    {
        Isbn = isbn;
        Title = title;
        Author = author;
        Status = status;
    }

    public Isbn Isbn { get; private set; }

    public BookTitle Title { get; private set; }

    public string Author { get; private set; }

    public BookStatus Status { get; private set; }

    public IReadOnlyCollection<Reservation> Reservations => _reservations;

    // ...
}
```

エンティティの状態はメソッド（操作）を通してだけ変更します。
公開セッターを持たせたり（[DFR0003](../../analyzers/DFR0003.md)）、変更可能なコレクションを公開したり（[DFR0009](../../analyzers/DFR0009.md)）すると、アナライザが警告します。

## ID と同一性

ID が同じで実行時の型も同じエンティティは、等しいと判定されます（`Equals` と `==`）。
ID に既定値（`UuidV7.Empty` など）を渡すと、コンストラクタが `ArgumentException` をスローします。
そのため、ID が未確定のエンティティは作れません。

`UuidV7` は、時刻順に並ぶ UUID（バージョン7）を表す構造体です。
`AggregateRoot` などの ID 固定版の基底クラスは、インスタンスの生成時に `UuidV7.NewUuidV7()` で ID を採番します。
DB に保存する前から ID が決まるため、集約を作ったその場で ID を参照できます。

```csharp
var book = Book.Create("9784798121963", "エリック・エヴァンスのドメイン駆動設計", "Eric Evans");
var id = book.Id; // 保存前でも採番済み
```

`UuidV7` は、文字列との相互変換（`Parse`・`TryParse`・`ToString`）と JSON への変換に対応しています。
ASP.NET Core のルートパラメーターにもそのまま使えます。

## 作成日時・更新日時・バージョン番号

`Entity` と `AggregateRoot` は次のプロパティを自動で記録します。

| プロパティ | 内容 |
|---|---|
| `CreatedAt` | インスタンスを生成したシステム日時（UTC） |
| `UpdatedAt` | 最後に更新を保存したシステム日時（UTC） |
| `Version` | 楽観的同時実行制御に使うバージョン番号。0 から始まり、更新を保存するたびに1増える |

これらは監査のための項目です。
業務上意味のある日時（貸出日など）は、別のプロパティで持ってください。

更新日時とバージョン番号は、`D3DbContext` が変更を保存するときに更新します。
子エンティティだけが変更された場合も、集約ルートの更新として扱います。
そのため、集約の一部が同時に変更された場合も競合を検出できます（[EF Core 連携](../05-efcore/01-dbcontext.md) を参照）。

## 集約とリポジトリ

集約は整合性を保つ単位としてまとめて扱うエンティティの集まりです。
集約の外からは集約ルートを通してだけ集約の中のエンティティを操作します。

リポジトリは集約ルートごとにインターフェースを定義し、子エンティティ用のリポジトリは作りません。
`IRepository<TAggregate>` を継承すると、次の操作が定義済みになります。

| メソッド | 内容 |
|---|---|
| `FindAsync(id, cancellationToken)` | ID を指定して集約を取得する。見つからない場合は `null` |
| `Add(aggregate)` | 集約を追加する |
| `Delete(token)` | 集約を削除する（後述の `DeletionToken<T>` を使う） |
| `CountAsync(cancellationToken)` | 集約の件数を取得する |
| `ExistsAsync(id, cancellationToken)` | 集約が存在するかどうかを、集約を読み込まずに判定する |

```csharp
public interface IBookRepository : IRepository<Book>
{
    Task<Pager<Book>> ListAsync(int pageNo, int pageSize, BookStatus? bookStatus = null, CancellationToken cancellationToken = default);

    Task<List<Book>> FindExpiredHoldsAsync(DateOnly today, CancellationToken cancellationToken = default);
}
```

リポジトリには更新や保存のメソッドがありません。
取得した集約への変更は Unit of Work が追跡し、Command の完了時にまとめて保存します。

集約の一部だけを取得・更新するメソッドは定義しないでください。
集約の整合性を壊すおそれがあるため、アナライザが警告します（[DFR0004](../../analyzers/DFR0004.md)、[DFR0005](../../analyzers/DFR0005.md)）。

## 集約の削除と DeletionToken

削除できるかどうかは集約自身が判断するべきです。
D3Fragments では、リポジトリの `Delete` は ID ではなく `DeletionToken<T>` を受け取ります。
`DeletionToken<T>` は、集約ルートの `IssueDeletionToken` からしか発行できません。
そのため、集約の削除ルールを通らずに削除することはできません。

```csharp
public sealed class Book : AggregateRoot
{
    // ...

    public DeletionToken<Book> IssueDeletionToken()
    {
        if (Status != BookStatus.Available)
        {
            throw new DomainException(ErrorType.InvalidState, "在架でない資料は除籍できません。");
        }

        if (Reservations.Any())
        {
            throw new DomainException(ErrorType.InvalidState, "予約がある資料は除籍できません。");
        }

        return base.IssueDeletionToken(this);
    }
}
```

```csharp
// Handler での使い方
var book = await books.FindAsync(request.BookId, cancellationToken)
    ?? throw new EntityNotFoundException(nameof(Book));
books.Delete(book.IssueDeletionToken());
```

## 関連する API

- @D3Fragments.AggregateRoot
- @D3Fragments.Entity
- @D3Fragments.EntityBase
- @D3Fragments.UuidV7
- <xref:D3Fragments.IRepository`1>
- <xref:D3Fragments.DeletionToken`1>
