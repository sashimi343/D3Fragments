using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;

using D3Fragments.Application;

namespace BasicWebApp.Application.Books;

public class RegisterBookCommand : ICommand<Book>
{
    public required string Isbn { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
}

public class RegisterBookCommandHandler : IRequestHandler<RegisterBookCommand, Book>
{
    private readonly IBookRepository _bookRepository;
    public RegisterBookCommandHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }
    public async Task<Book> HandleAsync(RegisterBookCommand request, CancellationToken cancellationToken = default)
    {
        Book book = Book.Create(request.Isbn, request.Title, request.Author);
        _bookRepository.Add(book);
        return book;
    }
}
