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

    // 予約は受付順（UuidV7は時刻順に採番される）で読み込む
    // ponytail: SQL ServerのuniqueidentifierはUUIDv7の時刻順に並ばないため、DBMSを変える場合は予約日時の列で並べる
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
