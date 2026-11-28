using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Members;

using D3Fragments.Application;

namespace BasicWebApp.Application.Members;

// For sign-in and cookie validation. Returns null if not found
public class FindMemberByEmailAddressQuery : IQuery<Member?>
{
    public required string EmailAddress { get; init; }
}

public class FindMemberByEmailAddressQueryHandler : IRequestHandler<FindMemberByEmailAddressQuery, Member?>
{
    private readonly IMemberRepository _memberRepository;

    public FindMemberByEmailAddressQueryHandler(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<Member?> HandleAsync(FindMemberByEmailAddressQuery request, CancellationToken cancellationToken)
    {
        return await _memberRepository.FindByEmailAddressAsync(request.EmailAddress, cancellationToken);
    }
}
