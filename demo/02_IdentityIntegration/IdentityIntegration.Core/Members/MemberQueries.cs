using D3Fragments;
using D3Fragments.Application;

using Microsoft.Extensions.Logging;

namespace IdentityIntegration.Core.Members;

public sealed record MemberProfile(string Nickname, DateOnly JoinedOn);

// ログイン中のアカウント（Identity のユーザー）に紐づく会員を取得する
public sealed record GetMemberProfileQuery(UuidV7 AccountId) : IQuery<MemberProfile?>;

public sealed class GetMemberProfileQueryHandler(IMemberRepository members) : IRequestHandler<GetMemberProfileQuery, MemberProfile?>
{
    public async Task<MemberProfile?> HandleAsync(GetMemberProfileQuery request, CancellationToken cancellationToken) =>
        await members.FindByAccountIdAsync(request.AccountId, cancellationToken) is { } member
            ? new MemberProfile(member.Nickname.Value, member.JoinedOn)
            : null;
}

// 会員登録の Commit 後に1回だけ呼ばれる。保存を UnitOfWork に任せるストア（AddD3FragmentsStores）を使うため、
// UserManager の操作ごとに SaveChanges が呼ばれてドメインイベントが途中で発行されることはない（デモではログ出力だけ）
public sealed class LogMemberRegisteredHandler(ILogger<LogMemberRegisteredHandler> logger) : IAfterCommitDomainEventHandler<MemberRegisteredDomainEvent>
{
    public Task HandleAsync(MemberRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("会員を登録しました。MemberId = '{MemberId}', AccountId = '{AccountId}'", domainEvent.MemberId, domainEvent.AccountId);
        return Task.CompletedTask;
    }
}
