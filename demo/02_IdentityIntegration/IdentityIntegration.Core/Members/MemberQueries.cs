using D3Fragments;
using D3Fragments.Application;

using Microsoft.Extensions.Logging;

namespace IdentityIntegration.Core.Members;

public sealed record MemberProfile(string Nickname, DateOnly JoinedOn);

// Gets the member linked to the signed-in account (Identity user)
public sealed record GetMemberProfileQuery(UuidV7 AccountId) : IQuery<MemberProfile?>;

public sealed class GetMemberProfileQueryHandler(IMemberRepository members) : IRequestHandler<GetMemberProfileQuery, MemberProfile?>
{
    public async Task<MemberProfile?> HandleAsync(GetMemberProfileQuery request, CancellationToken cancellationToken) =>
        await members.FindByAccountIdAsync(request.AccountId, cancellationToken) is { } member
            ? new MemberProfile(member.Nickname.Value, member.JoinedOn)
            : null;
}

// Called only once after the member registration is committed. Because the stores that leave saving to the UnitOfWork (AddD3FragmentsStores) are used,
// SaveChanges is not called for each UserManager operation, so domain events are never dispatched midway (the demo only writes a log)
public sealed class LogMemberRegisteredHandler(ILogger<LogMemberRegisteredHandler> logger) : IAfterCommitDomainEventHandler<MemberRegisteredDomainEvent>
{
    public Task HandleAsync(MemberRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("The member has been registered. MemberId = '{MemberId}', AccountId = '{AccountId}'", domainEvent.MemberId, domainEvent.AccountId);
        return Task.CompletedTask;
    }
}
