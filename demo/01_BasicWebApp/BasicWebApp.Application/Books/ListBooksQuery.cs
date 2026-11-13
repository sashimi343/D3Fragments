using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;

using D3Fragments;
using D3Fragments.Application;

namespace BasicWebApp.Application.Books;

public class ListBooksQuery : IQuery<Pager<Book>>
{
    public const int DefaultPageSize = 20;

    public BookStatus? BookStatus { get; init; }
    public int PageNo { get; init; } = IPager.StartPageNo;
    public int PageSize { get; init; } = DefaultPageSize;
}

public class ListBooksQueryHandler : IRequestHandler<ListBooksQuery, Pager<Book>>
{
    private readonly IBookRepository _bookRepository;

    public ListBooksQueryHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<Pager<Book>> HandleAsync(ListBooksQuery request, CancellationToken cancellationToken = default)
    {
        return await _bookRepository.ListAsync(request.PageNo, request.PageSize, request.BookStatus, cancellationToken);
    }
}
