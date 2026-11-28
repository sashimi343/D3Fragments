using D3Fragments;

namespace IdentityIntegration.Core.Members;

// The business user (member). Authentication information (password, two-factor authentication, etc.) belongs to the Identity user (UserAccount in Infrastructure),
// and the member refers to it by AccountId. It does not depend on Identity types
public sealed class Member : AggregateRoot
{
    private Member(UuidV7 accountId, Nickname nickname, DateOnly joinedOn)
    {
        AccountId = accountId;
        Nickname = nickname;
        JoinedOn = joinedOn;
        AddDomainEvent(new MemberRegisteredDomainEvent(DateTimeOffset.UtcNow, Id, accountId));
    }

    // For EF Core (Complex Types cannot be bound to constructor parameters)
    private Member()
    {
        Nickname = null!;
    }

    // ID of the Identity user
    public UuidV7 AccountId { get; private set; }

    public Nickname Nickname { get; private set; }

    public DateOnly JoinedOn { get; private set; }

    public static Result<Member> TryRegister(UuidV7 accountId, string nickname, DateOnly joinedOn)
    {
        Result<Nickname> nicknameResult = Nickname.TryCreate(nickname);
        return nicknameResult.IsFailure
            ? Result<Member>.Failure(nicknameResult.Errors)
            : Result<Member>.Success(new Member(accountId, nicknameResult.Value!, joinedOn));
    }
}

public sealed record MemberRegisteredDomainEvent(DateTimeOffset OccurredAt, UuidV7 MemberId, UuidV7 AccountId) : IDomainEvent;

public interface IMemberRepository : IRepository<Member>
{
    Task<Member?> FindByAccountIdAsync(UuidV7 accountId, CancellationToken cancellationToken);
}
