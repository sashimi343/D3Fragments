using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;

using D3Fragments;
using D3Fragments.EFCore;

using Microsoft.EntityFrameworkCore;

namespace BasicWebApp.Infrastructure.EFCore;

public class BookRepository : RepositoryBase<BasicWebAppDbContext, Book>, IBookRepository
{
    public BookRepository(BasicWebAppDbContext dbContext) : base(dbContext)
    {
    }

    // Load reservations in the order they were received (UuidV7 values are issued in time order)
    // ponytail: SQL Server's uniqueidentifier does not sort UUIDv7 in time order, so order by a reservation timestamp column if the DBMS changes
    private IQueryable<Book> BooksWithReservations => Set.Include(b => b.Reservations.OrderBy(r => r.Id));

    public override async Task<Book?> FindAsync(UuidV7 id, CancellationToken cancellationToken)
    {
        return await BooksWithReservations.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Pager<Book>> ListAsync(int pageNo, int pageSize, BookStatus? bookStatus = null, CancellationToken cancellationToken = default)
    {
        var query = BooksWithReservations;
        if (bookStatus.HasValue)
        {
            query = query.Where(b => b.Status == bookStatus.Value);
        }
        return await query.OrderBy(b => b.Id).ToPagedListAsync(pageNo, pageSize, cancellationToken);
    }

    public async Task<List<Book>> FindByIdsAsync(IReadOnlyCollection<UuidV7> ids, CancellationToken cancellationToken = default)
    {
        return await BooksWithReservations.Where(b => ids.Contains(b.Id)).ToListAsync(cancellationToken);
    }

    public async Task<List<Book>> FindExpiredHoldsAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        return await BooksWithReservations
            .Where(b => b.Status == BookStatus.OnHold && b.Reservations.Any(r => r.HoldUntil < today))
            .ToListAsync(cancellationToken);
    }
}
