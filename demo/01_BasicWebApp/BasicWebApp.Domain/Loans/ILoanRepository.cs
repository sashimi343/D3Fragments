using System;
using System.Collections.Generic;

using D3Fragments;

namespace BasicWebApp.Domain.Loans;

public interface ILoanRepository : IRepository<Loan, UuidV7>
{
    // Active = 未返却の貸出
    public Task<int> CountActiveByMemberIdAsync(UuidV7 memberId, CancellationToken cancellationToken = default);
    public Task<List<Loan>> FindActiveByMemberIdAsync(UuidV7 memberId, CancellationToken cancellationToken = default);
    public Task<Loan?> FindActiveByBookIdAsync(UuidV7 bookId, CancellationToken cancellationToken = default);
}
