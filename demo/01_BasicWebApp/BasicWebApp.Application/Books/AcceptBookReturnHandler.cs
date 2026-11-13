using System;
using System.Collections.Generic;
using System.Xml;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Loans;

using D3Fragments;
using D3Fragments.Application;

namespace BasicWebApp.Application.Books;

public class AcceptBookReturnHandler : IBeforeCommitDomainEventHandler<BookReturnedDomainEvent>
{
    private readonly IBookRepository _bookRepository;
    private readonly TimeProvider _timeProvider;

    public AcceptBookReturnHandler(IBookRepository bookRepository, TimeProvider timeProvider)
    {
        _bookRepository = bookRepository;
        _timeProvider = timeProvider;
    }

    public async Task HandleAsync(BookReturnedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        Book book = await _bookRepository.FindAsync(domainEvent.BookId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Book));
        book.AcceptReturn(domainEvent.ReturnedOn, _timeProvider.GetLocalNow());
    }
}
