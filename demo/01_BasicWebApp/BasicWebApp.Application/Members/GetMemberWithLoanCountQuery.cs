using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Loans;
using BasicWebApp.Domain.Members;

using D3Fragments;
using D3Fragments.Application;
using D3Fragments.Extensions;

namespace BasicWebApp.Application.Members;

public class GetMemberWithLoanCountQuery : IQuery<MemberWithLoanCountDto>
{
    public required string EmailAddress { get; init; }
}

public class GetMemberWithLoanCountQueryHandler  : IRequestHandler<GetMemberWithLoanCountQuery, MemberWithLoanCountDto>
{
    private readonly IMemberRepository _memberRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly TimeProvider _timeProvider;

    public GetMemberWithLoanCountQueryHandler(IMemberRepository memberRepository, ILoanRepository loanRepository, TimeProvider timeProvider)
    {
        _memberRepository = memberRepository;
        _loanRepository = loanRepository;
        _timeProvider = timeProvider;
    }

    public async Task<MemberWithLoanCountDto> HandleAsync(GetMemberWithLoanCountQuery request, CancellationToken cancellationToken = default)
    {
        Member member = await _memberRepository.FindByEmailAddressAsync(request.EmailAddress, cancellationToken) ?? throw new EntityNotFoundException(nameof(Member));
        int loanCount = await _loanRepository.CountActiveByMemberIdAsync(member.Id, cancellationToken);
        DateOnly today = _timeProvider.GetLocalToday();
        return new MemberWithLoanCountDto(member, member.IsLoanSuspended(today), member.LoanSuspendedFrom > today, loanCount);
    }
}
