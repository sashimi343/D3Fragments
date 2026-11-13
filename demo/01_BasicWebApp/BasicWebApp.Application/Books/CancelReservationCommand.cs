using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Members;

using D3Fragments;
using D3Fragments.Application;

namespace BasicWebApp.Application.Books;

public class CancelReservationCommand : ICommand<Book>
{
    public required UuidV7 BookId { get; init; }
    public required UuidV7 MemberId { get; init; }
}

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, Book>
{
    private readonly IBookRepository _bookRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly TimeProvider _timeProvider;

    public CancelReservationCommandHandler(IBookRepository bookRepository, IMemberRepository memberRepository, TimeProvider timeProvider)
    {
        _bookRepository = bookRepository;
        _memberRepository = memberRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Book> HandleAsync(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        Book book = await _bookRepository.FindAsync(request.BookId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Book));
        Member member = await _memberRepository.FindAsync(request.MemberId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Member));
        book.CancelReservation(member, _timeProvider.GetLocalNow());
        return book;
    }
}
