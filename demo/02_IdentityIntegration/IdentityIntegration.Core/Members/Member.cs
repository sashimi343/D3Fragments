using D3Fragments;

namespace IdentityIntegration.Core.Members;

// 業務上の利用者（会員）。認証の情報（パスワード・二要素認証など）は Identity のユーザー（Infrastructure の UserAccount）が持ち、
// 会員は AccountId でそれを参照する。Identity の型には依存しない
public sealed class Member : AggregateRoot
{
    private Member(UuidV7 accountId, Nickname nickname, DateOnly joinedOn)
    {
        AccountId = accountId;
        Nickname = nickname;
        JoinedOn = joinedOn;
        AddDomainEvent(new MemberRegisteredDomainEvent(DateTimeOffset.UtcNow, Id, accountId));
    }

    // EF Core用（Complex Typeはコンストラクタ引数にバインドできない）
    private Member()
    {
        Nickname = null!;
    }

    // Identity のユーザーの ID
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
