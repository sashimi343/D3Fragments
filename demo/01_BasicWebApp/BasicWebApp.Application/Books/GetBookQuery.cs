using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;

using D3Fragments;
using D3Fragments.Application;

namespace BasicWebApp.Application.Books;

public class GetBookQuery : IQuery<Book>
{
    public required UuidV7 BookId { get; init; }
}

public class GetBookQueryHandler : IRequestHandler<GetBookQuery, Book>
{
    private readonly IBookRepository _bookRepository;

    public GetBookQueryHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<Book> HandleAsync(GetBookQuery request, CancellationToken cancellationToken)
    {
        return await _bookRepository.FindAsync(request.BookId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Book));
    }
}
