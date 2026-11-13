using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;

using D3Fragments;
using D3Fragments.Application;

namespace BasicWebApp.Application.Books;

public class DeleteBookCommand : ICommand<Book>
{
    public required UuidV7 BookId { get; init; }
}

public class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, Book>
{
    private readonly IBookRepository _bookRepository;

    public DeleteBookCommandHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<Book> HandleAsync(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        Book book = await _bookRepository.FindAsync(request.BookId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Book));
        DeletionToken<Book> deletionToken = book.IssueDeletionToken();
        _bookRepository.Delete(deletionToken);
        return book;
    }
}
