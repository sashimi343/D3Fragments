using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Loans;

using D3Fragments;
using D3Fragments.Application;
using D3Fragments.Extensions;

namespace BasicWebApp.Application.Loans;

public class ListMemberLoansQuery : IQuery<List<LoanWithOverdueDto>>
{
    public required UuidV7 MemberId { get; init; }
}

public readonly record struct LoanWithOverdueDto(Loan Loan, BookTitle BookTitle, bool IsOverdue);

public class ListMemberLoansQueryHandler : IRequestHandler<ListMemberLoansQuery, List<LoanWithOverdueDto>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;
    private readonly TimeProvider _timeProvider;

    public ListMemberLoansQueryHandler(ILoanRepository loanRepository, IBookRepository bookRepository, TimeProvider timeProvider)
    {
        _loanRepository = loanRepository;
        _bookRepository = bookRepository;
        _timeProvider = timeProvider;
    }

    public async Task<List<LoanWithOverdueDto>> HandleAsync(ListMemberLoansQuery request, CancellationToken cancellationToken)
    {
        DateOnly today = _timeProvider.GetLocalToday();
        List<Loan> loans = await _loanRepository.FindActiveByMemberIdAsync(request.MemberId, cancellationToken);
        // 貸出中の資料は除籍できない（B9）ため、必ず見つかる
        Dictionary<UuidV7, BookTitle> titles = (await _bookRepository.FindByIdsAsync([.. loans.Select(l => l.BookId)], cancellationToken))
            .ToDictionary(b => b.Id, b => b.Title);
        return [.. loans.Select(l => new LoanWithOverdueDto(l, titles[l.BookId], l.GetOverdueDays(today) > 0))];
    }
}
