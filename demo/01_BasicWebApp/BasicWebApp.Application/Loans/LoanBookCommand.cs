using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Loans;
using BasicWebApp.Domain.Members;

using D3Fragments;
using D3Fragments.Application;
using D3Fragments.Extensions;

namespace BasicWebApp.Application.Loans;

public class LoanBookCommand : ICommand<Loan>
{
    public required UuidV7 MemberId { get; init; }
    public required UuidV7 BookId { get; init; }
}

public class LoanBookCommandHandler : IRequestHandler<LoanBookCommand, Loan>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IBookRepository _bookRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly LoanEligibilityRule _rule = new();
    private readonly TimeProvider _timeProvider;

    public LoanBookCommandHandler(IMemberRepository memberRepository, IBookRepository bookRepository, ILoanRepository loanRepository, TimeProvider timeProvider)
    {
        _memberRepository = memberRepository;
        _bookRepository = bookRepository;
        _loanRepository = loanRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Loan> HandleAsync(LoanBookCommand request, CancellationToken cancellationToken)
    {
        DateOnly today = _timeProvider.GetLocalToday();
        Member member = await _memberRepository.FindAsync(request.MemberId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Member));
        Book book = await _bookRepository.FindAsync(request.BookId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Book));
        List<Loan> activeLoans = await _loanRepository.FindActiveByMemberIdAsync(request.MemberId, cancellationToken);
        LoanEligibilityRuleContext context = new()
        {
            Member = member,
            Book = book,
            ActiveLoans = activeLoans,
            Today = today,
        };

        Result checkResult = _rule.Check(context);
        checkResult.ThrowIfFailure();

        Loan loan = book.LendTo(member, today);
        _loanRepository.Add(loan);

        return loan;
    }
}
