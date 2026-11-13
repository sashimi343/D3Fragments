# Domain rules

A domain rule is a class that represents a decision spanning multiple aggregates or value objects.
For example, "can this member borrow this book?" is decided by looking at the member, the book, and the list of books the member currently has on loan.
Such a decision can be hard to write as a method of any single aggregate.

You do not need a domain rule for a decision that can be written naturally as a method of one aggregate.

## Defining a rule

Implement `IDomainRule<TContext>`.
Specify as `TContext` a record type that groups the values used for the decision under names.
`Check` returns a successful `Result` if the rule is satisfied, or a failed `Result` with the reason if it is not.

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
            return Result.Failure(Error.PreconditionFailed("Loans are suspended for this member."));
        }

        if (context.ActiveLoans.Count >= context.Member.MemberType.LoanLimit)
        {
            return Result.Failure(Error.PreconditionFailed("The member has reached the loan limit."));
        }

        // ...
        return Result.Success();
    }
}
```

A domain rule only makes a pure decision.
It does not call repositories or change the state of aggregates.
The caller collects the data needed for the decision and passes it as `TContext`.

## Using a rule

In a handler, collect the necessary data, call the rule, and convert a failure into a `DomainException` with `ThrowIfFailure`.

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

`GetLocalToday` is an extension method that gets today's local date from a `TimeProvider` as a `DateOnly` (in the `D3Fragments.Extensions` namespace).

## Related APIs

- <xref:D3Fragments.IDomainRule`1>
- @D3Fragments.Extensions.TimeProviderExtensions
