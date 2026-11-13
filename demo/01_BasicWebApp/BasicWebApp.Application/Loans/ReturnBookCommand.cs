using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Loans;

using D3Fragments;
using D3Fragments.Application;

namespace BasicWebApp.Application.Loans;

public class ReturnBookCommand : ICommand<Loan>
{
    public required UuidV7 LoanId { get; init; }
    public required UuidV7 MemberId { get; init; }
}

public class ReturnBookCommandHandler : IRequestHandler<ReturnBookCommand, Loan>
{
    private readonly ILoanRepository _loanRepository;
    private readonly TimeProvider _timeProvider;

    public ReturnBookCommandHandler(ILoanRepository loanRepository, TimeProvider timeProvider)
    {
        _loanRepository = loanRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Loan> HandleAsync(ReturnBookCommand request, CancellationToken cancellationToken)
    {
        Loan loan = await _loanRepository.FindAsync(request.LoanId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Loan));
        if (loan.MemberId != request.MemberId)
        {
            throw new DomainException(Error.Forbidden("他の会員の貸出は返却できません。"));
        }
        loan.Return(_timeProvider.GetLocalNow());
        return loan;
    }
}
