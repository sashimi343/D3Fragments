using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Loans;
using BasicWebApp.Domain.Members;

using D3Fragments;
using D3Fragments.Application;
using D3Fragments.Extensions;

namespace BasicWebApp.Application.Loans;

public class RenewLoanCommand : ICommand<Loan>
{
    public required UuidV7 LoanId { get; init; }
    public required UuidV7 MemberId { get; init; }
}

public class RenewLoanCommandHandler : IRequestHandler<RenewLoanCommand, Loan>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IBookRepository _bookRepository;
    private readonly TimeProvider _timeProvider;

    public RenewLoanCommandHandler(ILoanRepository loanRepository, IMemberRepository memberRepository, IBookRepository bookRepository, TimeProvider timeProvider)
    {
        _loanRepository = loanRepository;
        _memberRepository = memberRepository;
        _bookRepository = bookRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Loan> HandleAsync(RenewLoanCommand request, CancellationToken cancellationToken)
    {
        Loan loan = await _loanRepository.FindAsync(request.LoanId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Loan));
        if (loan.MemberId != request.MemberId)
        {
            throw new DomainException(Error.Forbidden("他の会員の貸出は延長できません。"));
        }
        Member member = await _memberRepository.FindAsync(loan.MemberId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Member));
        Book book = await _bookRepository.FindAsync(loan.BookId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Book));

        loan.Renew(_timeProvider.GetLocalToday(), member.MemberType.LoanPeriodDays, book.Reservations.Any());
        return loan;
    }
}
