using System;
using System.Collections.Generic;

using D3Fragments;

namespace BasicWebApp.Domain.Books;

public interface IBookRepository : IRepository<Book, UuidV7>
{
    public Task<Pager<Book>> ListAsync(int pageNo, int pageSize, BookStatus? bookStatus = null, CancellationToken cancellationToken = default);

    public Task<List<Book>> FindByIdsAsync(IReadOnlyCollection<UuidV7> ids, CancellationToken cancellationToken = default);

    public Task<List<Book>> FindExpiredHoldsAsync(DateOnly today, CancellationToken cancellationToken = default);
}
