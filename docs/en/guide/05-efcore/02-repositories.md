# Repositories and paging

## Implementing a repository

A repository implementation derives from `RepositoryBase<TContext, TAggregate>` and implements the interface defined in the domain layer.
If the ID type is not `UuidV7`, use `RepositoryBase<TContext, TAggregate, TId>`.

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

The base class implements the basic `IRepository` operations (`FindAsync`, `Add`, `Delete`, `CountAsync`, `ExistsAsync`).
Derived classes can use the `DbContext` property and the `Set` property (the `DbSet` of the aggregate root).

Each method only changes the tracking state of the `DbContext`; it does not save anything.
The Unit of Work does the saving.
The only exception is `ExecuteImmediateAsync`, described next, which saves pending changes but does not commit them (repositories do not commit changes).

### Executing immediate operations

Call operations that run against the database immediately, such as `ExecuteUpdateAsync`, `ExecuteDeleteAsync`, and `Database.ExecuteSqlAsync`, inside the lambda passed to `ExecuteImmediateAsync` ([DFR0020](../../analyzers/DFR0020.md)).

```csharp
public Task<int> DeleteExpiredAsync(DateOnly today, CancellationToken cancellationToken) =>
    ExecuteImmediateAsync(ct => Set.Where(x => x.HoldUntil < today).ExecuteDeleteAsync(ct), cancellationToken);
```

`ExecuteImmediateAsync` works as follows.

- It can only run within a transaction. Calling it outside a transaction throws `InvalidOperationException`. Make the commands that use it implement `ITransactionalCommand` ([Explicit transactions](../04-application/02-pipeline-and-transactions.md#explicit-transactions)).
- Before the operation, it saves the pending tracked changes. `UpdatedAt` and `Version` are updated, and optimistic concurrency conflicts are converted to `ConflictOccurredException`, in the same way as a normal save. If the command fails, the saved changes are rolled back with the transaction.
- This save does not call before-commit domain event handlers. The recorded events are dispatched by the save at the end of the command.

Note the following points.

- The result of the operation is not reflected in tracked entities.
- An `ExecuteUpdate` on an aggregate table does not update `UpdatedAt` or `Version`. Set them yourself with `SetProperty` if needed.
- If the same aggregate is changed again after the save, `Version` is incremented twice in one command.
- In `TransactionalTestBase`, the test transaction has already been started, so `ExecuteImmediateAsync` can be called. However, rollback on command failure cannot be verified there (the test transaction is rolled back at the end of the test).

### Things to note when implementing

- Load child entities: the base class `FindAsync` does not load child entities. If the aggregate has child entities, override `FindAsync` and load them with `Include` ([DFR0015](../../analyzers/DFR0015.md)).
- Do not disable change tracking: changes to an aggregate retrieved with `AsNoTracking()` are not tracked, so they are lost without any error ([DFR0014](../../analyzers/DFR0014.md)).
- Do not call `Update` or `Attach`: entities whose ID is already assigned are treated as existing rows, and child entities may not be saved correctly ([DFR0013](../../analyzers/DFR0013.md)).
- Execute immediate operations inside `ExecuteImmediateAsync`: otherwise they run before the pending changes are saved, and outside a transaction they are committed at once ([DFR0020](../../analyzers/DFR0020.md)).

## Paging

`ToPagedListAsync` gets one page of a query and returns it as a `Pager<T>`.

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

- Page numbers start at 1 (`IPager.StartPageNo`).
- The page number and page size are validated before the database is queried. If they are invalid, `DomainValidationException` is thrown.
- The maximum page size is 200 by default (`PagingExtensions.DefaultMaxPageSize`). You can change it with the `maxPageSize` parameter.
- If you specify a page number after the last page, an empty page is returned instead of an error. The same applies when there is no data. The `PageNo` of the returned `Pager<T>` is the specified page number.
- Specify the sort order with `OrderBy` on the caller side. Include a unique key in the sort order so that the order does not change between pages.

`Pager<T>` holds the data of the page (`Items`) as well as the total count, the total number of pages, the previous and next page numbers, and so on.

```csharp
Pager<Book> page = await books.ListAsync(pageNo: 2, pageSize: 20, cancellationToken: cancellationToken);
// page.Items, page.TotalCount, page.TotalPages, page.HasNextPage, and so on
```

## Related APIs

- <xref:D3Fragments.EFCore.RepositoryBase`2>
- <xref:D3Fragments.EFCore.RepositoryBase`3>
- @D3Fragments.EFCore.PagingExtensions
- <xref:D3Fragments.Pager`1>
- @D3Fragments.IPager
