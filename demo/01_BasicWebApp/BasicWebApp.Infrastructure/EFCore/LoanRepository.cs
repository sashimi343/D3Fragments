using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Loans;

using D3Fragments;
using D3Fragments.EFCore;

using Microsoft.EntityFrameworkCore;

namespace BasicWebApp.Infrastructure.EFCore;

public class LoanRepository : RepositoryBase<BasicWebAppDbContext, Loan>, ILoanRepository
{
    public LoanRepository(BasicWebAppDbContext dbContext) : base(dbContext)
    {
    }

    private IQueryable<Loan> ActiveLoans => DbContext.Loans.Where(l => l.ReturnedOn == null);

    public async Task<int> CountActiveByMemberIdAsync(UuidV7 memberId, CancellationToken cancellationToken = default)
    {
        return await ActiveLoans.Where(l => l.MemberId == memberId).CountAsync(cancellationToken);
    }

    public async Task<List<Loan>> FindActiveByMemberIdAsync(UuidV7 memberId, CancellationToken cancellationToken = default)
    {
        return await ActiveLoans.Where(l => l.MemberId == memberId).OrderBy(l => l.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<Loan?> FindActiveByBookIdAsync(UuidV7 bookId, CancellationToken cancellationToken = default)
    {
        return await ActiveLoans.SingleOrDefaultAsync(l => l.BookId == bookId, cancellationToken);
    }
}
