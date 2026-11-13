using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Loans;
using BasicWebApp.Domain.Members;

using D3Fragments;
using D3Fragments.Application;
using D3Fragments.Extensions;

namespace BasicWebApp.Application.Books;

public class ReserveBookCommand : ICommand<Book>
{
    public required UuidV7 BookId { get; init; }
    public required UuidV7 MemberId { get; init; }
}

public class ReserveBookCommandHandler : IRequestHandler<ReserveBookCommand, Book>
{
    private readonly IBookRepository _bookRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly TimeProvider _timeProvider;

    public ReserveBookCommandHandler(IBookRepository bookRepository, IMemberRepository memberRepository, ILoanRepository loanRepository, TimeProvider timeProvider)
    {
        _bookRepository = bookRepository;
        _memberRepository = memberRepository;
        _loanRepository = loanRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Book> HandleAsync(ReserveBookCommand request, CancellationToken cancellationToken)
    {
        Book book = await _bookRepository.FindAsync(request.BookId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Book));
        Member member = await _memberRepository.FindAsync(request.MemberId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Member));
        Loan? currentLoan = await _loanRepository.FindActiveByBookIdAsync(book.Id, cancellationToken);
        book.AcceptReservation(member, _timeProvider.GetLocalToday(), currentLoan?.MemberId);
        return book;
    }
}
