# ドメインルール

ドメインルールは複数の集約や値オブジェクトにまたがる判定を表すクラスです。
たとえば「この会員がこの資料を借りられるか」は、会員・資料・貸出中の資料の一覧を見て判定します。
このような判定は、どれか1つの集約のメソッドとしては書きにくいことがあります。

1つの集約のメソッドとして自然に書ける判定には、ドメインルールを使う必要はありません。

## ルールを定義する

`IDomainRule<TContext>` を実装します。
`TContext` には、判定に使う値を名前付きでまとめた record 型を指定します。
`Check` は、ルールを満たせば成功の `Result` を、満たさなければ理由を持つ失敗の `Result` を返します。

```csharp
using D3Fragments;

public readonly record struct LoanEligibilityRuleContext
{
    public Member Member { get; init; }

    public Book Book { get; init; }

    public IReadOnlyCollection<Loan> ActiveLoans { get; init; }

    public DateOnly Today { get; init; }
}

public sealed class LoanEligibilityRule : IDomainRule<LoanEligibilityRuleContext>
{
    public Result Check(LoanEligibilityRuleContext context)
    {
        if (context.Member.IsLoanSuspended(context.Today))
        {
            return Result.Failure(Error.PreconditionFailed("貸出停止中のため貸出できません。"));
        }

        if (context.ActiveLoans.Count >= context.Member.MemberType.LoanLimit)
        {
            return Result.Failure(Error.PreconditionFailed("貸出冊数の上限に達しているため貸出できません。"));
        }

        // ...
        return Result.Success();
    }
}
```

ドメインルールは純粋な判定だけを行います。
リポジトリを呼んだり、集約の状態を変えたりしません。
判定に必要なデータは、呼び出し側が集めて `TContext` で渡します。

## ルールを使う

Handler で必要なデータを集めてルールを呼び、失敗なら `ThrowIfFailure` で `DomainException` に変換します。

```csharp
public async Task<Loan> HandleAsync(LoanBookCommand request, CancellationToken cancellationToken)
{
    DateOnly today = _timeProvider.GetLocalToday();
    Member member = await _memberRepository.FindAsync(request.MemberId, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(Member));
    Book book = await _bookRepository.FindAsync(request.BookId, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(Book));
    List<Loan> activeLoans = await _loanRepository.FindActiveByMemberIdAsync(request.MemberId, cancellationToken);

    _rule.Check(new LoanEligibilityRuleContext
    {
        Member = member,
        Book = book,
        ActiveLoans = activeLoans,
        Today = today,
    }).ThrowIfFailure();

    Loan loan = book.LendTo(member, today);
    _loanRepository.Add(loan);
    return loan;
}
```

`GetLocalToday` は、`TimeProvider` から現地の今日の日付を `DateOnly` で取得する拡張メソッドです（`D3Fragments.Extensions` 名前空間）。

## 関連する API

- <xref:D3Fragments.IDomainRule`1>
- @D3Fragments.Extensions.TimeProviderExtensions
