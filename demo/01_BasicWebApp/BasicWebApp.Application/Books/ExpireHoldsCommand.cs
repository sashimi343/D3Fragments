using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;

using D3Fragments.Application;

namespace BasicWebApp.Application.Books;

// Processes all books whose hold has expired and returns the number of processed books (intended as a daily batch)
public class ExpireHoldsCommand : ICommand<int>
{
}

public class ExpireHoldsCommandHandler : IRequestHandler<ExpireHoldsCommand, int>
{
    private readonly IBookRepository _bookRepository;
    private readonly TimeProvider _timeProvider;

    public ExpireHoldsCommandHandler(IBookRepository bookRepository, TimeProvider timeProvider)
    {
        _bookRepository = bookRepository;
        _timeProvider = timeProvider;
    }

    // ponytail: all books are processed in one transaction, so a single conflict fails the whole batch. Split into one Command per book if the count grows
    public async Task<int> HandleAsync(ExpireHoldsCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetLocalNow();
        List<Book> books = await _bookRepository.FindExpiredHoldsAsync(DateOnly.FromDateTime(now.DateTime), cancellationToken);
        foreach (Book book in books)
        {
            book.ExpireHold(now);
        }
        return books.Count;
    }
}
