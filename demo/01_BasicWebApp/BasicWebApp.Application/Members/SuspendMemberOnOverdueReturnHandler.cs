using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Loans;
using BasicWebApp.Domain.Members;

using D3Fragments;
using D3Fragments.Application;

namespace BasicWebApp.Application.Members;

public class SuspendMemberOnOverdueReturnHandler : IBeforeCommitDomainEventHandler<BookReturnedDomainEvent>
{
    private readonly IMemberRepository _memberRepository;

    public SuspendMemberOnOverdueReturnHandler(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task HandleAsync(BookReturnedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ReturnedOn <= domainEvent.DueDate)
        {
            return;
        }
        Member member = await _memberRepository.FindAsync(domainEvent.MemberId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Member));

        int overdueDays = domainEvent.ReturnedOn.DayNumber - domainEvent.DueDate.DayNumber;
        member.SuspendLoan(domainEvent.ReturnedOn, overdueDays);
    }
}
