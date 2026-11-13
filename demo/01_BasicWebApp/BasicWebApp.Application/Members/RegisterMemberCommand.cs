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

        // ponytail: 同時登録はすり抜ける。EF Core 10はComplex Typeのプロパティにインデックスを張れないため、必要になったらマイグレーションで一意制約を追加する
        if (await _memberRepository.ExistsByEmailAddressAsync(member.EmailAddress.Value, cancellationToken))
        {
            throw new DomainException(Error.Conflict("指定されたメールアドレスは既に登録されています。"));
        }

        _memberRepository.Add(member);
        return member;
    }
}
