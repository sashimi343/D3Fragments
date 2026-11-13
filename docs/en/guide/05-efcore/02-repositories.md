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

### Things to note when implementing

- Load child entities: the base class `FindAsync` does not load child entities. If the aggregate has child entities, override `FindAsync` and load them with `Include` ([DFR0015](../../analyzers/DFR0015.md)).
- Do not disable change tracking: changes to an aggregate retrieved with `AsNoTracking()` are not tracked, so they are lost without any error ([DFR0014](../../analyzers/DFR0014.md)).
- Do not call `Update` or `Attach`: entities whose ID is already assigned are treated as existing rows, and child entities may not be saved correctly ([DFR0013](../../analyzers/DFR0013.md)).

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
