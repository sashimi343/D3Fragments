using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Members;

using D3Fragments;
using D3Fragments.Application;

namespace BasicWebApp.Application.Members;

public class RegisterMemberCommand : ICommand<Member>
{
    public required string MemberName { get; init; }
    public required string EmailAddress { get; init; }
    public required MemberType MemberType { get; init; }
}

public class RegisterMemberCommandHandler : IRequestHandler<RegisterMemberCommand, Member>
{
    private readonly IMemberRepository _memberRepository;

    public RegisterMemberCommandHandler(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<Member> HandleAsync(RegisterMemberCommand request, CancellationToken cancellationToken = default)
    {
        Member member = Member.Create(request.MemberName, request.EmailAddress, request.MemberType);

        // ponytail: concurrent registrations slip through. EF Core 10 cannot index Complex Type properties, so add a unique constraint in a migration when needed
        if (await _memberRepository.ExistsByEmailAddressAsync(member.EmailAddress.Value, cancellationToken))
        {
            throw new DomainException(Error.Conflict("The specified email address is already registered."));
        }

        _memberRepository.Add(member);
        return member;
    }
}
