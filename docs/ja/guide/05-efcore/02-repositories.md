# リポジトリとページング

## リポジトリを実装する

リポジトリの実装は `RepositoryBase<TContext, TAggregate>` を継承し、ドメイン層で定義したインターフェースを実装します。
ID の型が `UuidV7` でない場合は、`RepositoryBase<TContext, TAggregate, TId>` を使います。

```csharp
using D3Fragments;
using D3Fragments.EFCore;
using Microsoft.EntityFrameworkCore;

public sealed class BookRepository(LibraryDbContext dbContext)
    : RepositoryBase<LibraryDbContext, Book>(dbContext), IBookRepository
{
    private IQueryable<Book> BooksWithReservations => Set.Include(b => b.Reservations.OrderBy(r => r.Id));

    public override Task<Book?> FindAsync(UuidV7 id, CancellationToken cancellationToken) =>
        BooksWithReservations.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<List<Book>> FindExpiredHoldsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
        BooksWithReservations
            .Where(b => b.Status == BookStatus.OnHold && b.Reservations.Any(r => r.HoldUntil < today))
            .ToListAsync(cancellationToken);
}
```

基底クラスは `IRepository` の基本操作（`FindAsync`・`Add`・`Delete`・`CountAsync`・`ExistsAsync`）を実装しています。
派生クラスでは `DbContext` プロパティと `Set` プロパティ（集約ルートの `DbSet`）を使えます。

各メソッドは `DbContext` の追跡状態を変えるだけで、保存は行いません。
保存は Unit of Work が行います。
例外は次に説明する `ExecuteImmediateAsync` だけで、未保存の変更を保存しますが、Commit はしません（リポジトリは変更を確定しません）。

### 即時の操作を実行する

`ExecuteUpdateAsync`・`ExecuteDeleteAsync`・`Database.ExecuteSqlAsync` など、データベースに即時に実行される操作は、`ExecuteImmediateAsync` に渡すラムダの中で呼び出します（[DFR0020](../../analyzers/DFR0020.md)）。

```csharp
public Task<int> DeleteExpiredAsync(DateOnly today, CancellationToken cancellationToken) =>
    ExecuteImmediateAsync(ct => Set.Where(x => x.HoldUntil < today).ExecuteDeleteAsync(ct), cancellationToken);
```

`ExecuteImmediateAsync` は次のように動きます。

- トランザクションの中でのみ実行できます。トランザクションの外で呼び出すと `InvalidOperationException` をスローします。この操作を使う Command には `ITransactionalCommand` を実装してください（[明示的なトランザクション](../04-application/02-pipeline-and-transactions.md#明示的なトランザクション)）。
- 操作の前に、追跡中の未保存の変更を保存します。`UpdatedAt`・`Version` の更新と、楽観ロックの競合の `ConflictOccurredException` への変換は、通常の保存と同じです。保存した変更は、Command が失敗した場合にトランザクションとともにロールバックされます。
- この保存では Commit 前のドメインイベントハンドラは呼ばれません。記録されたイベントは、Command の終了時の保存で配信されます。

以下の点に注意してください。

- 操作の結果は、追跡中のエンティティには反映されません。
- 集約のテーブルに対する `ExecuteUpdate` では、`UpdatedAt`・`Version` は更新されません。必要であれば、`SetProperty` で自分で設定してください。
- 途中の保存の後で同じ集約をもう一度変更すると、1つの Command で `Version` が2回加算されます。
- `TransactionalTestBase` の中ではテスト用のトランザクションが開始済みのため、`ExecuteImmediateAsync` を呼び出せます。ただし、Command の失敗によるロールバックは確認できません（テスト用のトランザクションはテストの終了時にロールバックされます）。

### 実装するときの注意

- 子エンティティを読み込む：基底クラスの `FindAsync` は子エンティティを読み込みません。集約に子エンティティがある場合は、`FindAsync` をオーバーライドして `Include` で読み込んでください（[DFR0015](../../analyzers/DFR0015.md)）。
- 変更追跡を無効にしない：`AsNoTracking()` で取得した集約は変更が追跡されないため、変更してもエラーにならずに失われます（[DFR0014](../../analyzers/DFR0014.md)）。
- `Update`・`Attach` を呼ばない：ID が採番済みのエンティティが既存の行として扱われ、子エンティティが正しく保存されないことがあります（[DFR0013](../../analyzers/DFR0013.md)）。
- 即時の操作は `ExecuteImmediateAsync` の中で実行する：そうしないと未保存の変更より先に実行され、トランザクションの外ではその場で確定します（[DFR0020](../../analyzers/DFR0020.md)）。

## ページング

`ToPagedListAsync` は、クエリの1ページ分を取得して `Pager<T>` で返します。

```csharp
public Task<Pager<Book>> ListAsync(int pageNo, int pageSize, BookStatus? bookStatus = null, CancellationToken cancellationToken = default)
{
    var query = BooksWithReservations;
    if (bookStatus.HasValue)
    {
        query = query.Where(b => b.Status == bookStatus.Value);
    }

    return query.OrderBy(b => b.Id).ToPagedListAsync(pageNo, pageSize, cancellationToken);
}
```

- ページ番号は1から始まります（`IPager.StartPageNo`）。
- DB に問い合わせる前に、ページ番号とページサイズを検証します。不正な場合は `DomainValidationException` をスローします。
- ページサイズの上限は既定で 200（`PagingExtensions.DefaultMaxPageSize`）です。`maxPageSize` 引数で変えられます。
- 最終ページより後のページ番号を指定すると、エラーではなく空のページを返します。データが0件の場合も同じです。返す `Pager<T>` の `PageNo` は指定したページ番号です。
- 並び順は呼び出し側で `OrderBy` を指定してください。ページ間で並びが変わらないように、一意なキーを並び順に含めます。

`Pager<T>` は、そのページのデータ（`Items`）と、全件数・全ページ数・前後のページ番号などを持ちます。

```csharp
Pager<Book> page = await books.ListAsync(pageNo: 2, pageSize: 20, cancellationToken: cancellationToken);
// page.Items、page.TotalCount、page.TotalPages、page.HasNextPage など
```

## 関連する API

- <xref:D3Fragments.EFCore.RepositoryBase`2>
- <xref:D3Fragments.EFCore.RepositoryBase`3>
- @D3Fragments.EFCore.PagingExtensions
- <xref:D3Fragments.Pager`1>
- @D3Fragments.IPager
