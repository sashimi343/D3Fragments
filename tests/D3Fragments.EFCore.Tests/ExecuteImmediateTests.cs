using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;
using D3Fragments.Testing;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace D3Fragments.EFCore.Tests;

internal sealed class ImmediateMemberRepository(MemberDbContext dbContext) : RepositoryBase<MemberDbContext, Member>(dbContext)
{
    public Task<int> DeleteAllAsync(CancellationToken cancellationToken) =>
        ExecuteImmediateAsync(ct => Set.ExecuteDeleteAsync(ct), cancellationToken);

    public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        ExecuteImmediateAsync(operation, cancellationToken);
}

public class ExecuteImmediateTests
{
    private static Member NewMember(string email = "a@example.com") => new(Email.Create(email), null, TestColor.Red, null);

    private static async Task<Member> SeedAsync(SqliteTestDatabase database)
    {
        var member = NewMember();
        await using var dbContext = database.CreateContext();
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return member;
    }

    private static EfUnitOfWork<MemberDbContext> CreateUnitOfWork(MemberDbContext dbContext, SpyDomainEventDispatcher dispatcher) =>
        new(dbContext, dispatcher, new ImmediateDispatchStrategy(dispatcher), NullLogger<EfUnitOfWork<MemberDbContext>>.Instance, Options.Create(new D3FragmentsApplicationOptions()));

    [Fact]
    public async Task トランザクションの外ではInvalidOperationExceptionを送出し_操作も保存も行わない()
    {
        using var database = new SqliteTestDatabase();
        await SeedAsync(database);
        await using var dbContext = database.CreateContext();
        var repository = new ImmediateMemberRepository(dbContext);
        repository.Add(NewMember("b@example.com"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAllAsync(TestContext.Current.CancellationToken));

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.Members.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task 操作がnullの場合はArgumentNullExceptionを送出する()
    {
        using var database = new SqliteTestDatabase();
        await using var dbContext = database.CreateContext();
        var repository = new ImmediateMemberRepository(dbContext);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.RunAsync<int>(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task 未保存の変更を操作の前に保存する()
    {
        using var database = new SqliteTestDatabase();
        await SeedAsync(database);
        await using var dbContext = database.CreateContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var repository = new ImmediateMemberRepository(dbContext);
        repository.Add(NewMember("b@example.com"));

        var deleted = await repository.DeleteAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, deleted);
    }

    [Fact]
    public async Task 途中の保存でVersionとUpdatedAtを更新する()
    {
        using var database = new SqliteTestDatabase();
        var seeded = await SeedAsync(database);
        await using var dbContext = database.CreateContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var repository = new ImmediateMemberRepository(dbContext);
        var member = await repository.FindAsync(seeded.Id, TestContext.Current.CancellationToken);
        member!.ChangeEmail(Email.Create("changed@example.com"));

        var saved = await repository.RunAsync(
            ct => dbContext.Members.AsNoTracking().Where(x => x.Id == seeded.Id).Select(x => new { x.Version, x.UpdatedAt }).SingleAsync(ct),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, saved.Version);
        Assert.NotEqual(seeded.UpdatedAt, saved.UpdatedAt);
    }

    [Fact]
    public async Task 途中の保存での競合はConflictOccurredExceptionに変換する()
    {
        using var database = new SqliteTestDatabase();
        await SeedAsync(database);
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var secondMember = await second.Members.SingleAsync(TestContext.Current.CancellationToken);
        (await first.Members.SingleAsync(TestContext.Current.CancellationToken)).ChangeEmail(Email.Create("first@example.com"));
        await first.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var transaction = await second.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        secondMember.ChangeEmail(Email.Create("second@example.com"));
        var repository = new ImmediateMemberRepository(second);

        await Assert.ThrowsAsync<ConflictOccurredException>(() => repository.DeleteAllAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task 途中の保存ではCommit前のハンドラーを呼ばず_最後の保存で配信する()
    {
        using var database = new SqliteTestDatabase();
        await using var dbContext = database.CreateContext();
        var dispatcher = new SpyDomainEventDispatcher();
        var unitOfWork = CreateUnitOfWork(dbContext, dispatcher);
        var repository = new ImmediateMemberRepository(dbContext);
        var domainEvent = new TestDomainEvent(DateTimeOffset.UtcNow);

        await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var member = NewMember();
                member.Raise(domainEvent);
                repository.Add(member);
                await repository.RunAsync(c => dbContext.Members.CountAsync(c), ct);
                Assert.Empty(dispatcher.BeforeCommitEvents);
                return await unitOfWork.SaveChangesAsync(ct);
            },
            TestContext.Current.CancellationToken);

        Assert.Same(domainEvent, Assert.Single(dispatcher.BeforeCommitEvents));
        Assert.Same(domainEvent, Assert.Single(dispatcher.AfterCommitEvents));
    }

    [Fact]
    public async Task Commandが失敗した場合は途中で保存した変更もロールバックされる()
    {
        using var database = new SqliteTestDatabase();
        await using var dbContext = database.CreateContext();
        var unitOfWork = CreateUnitOfWork(dbContext, new SpyDomainEventDispatcher());
        var repository = new ImmediateMemberRepository(dbContext);

        await Assert.ThrowsAsync<DomainException>(() => unitOfWork.ExecuteInTransactionAsync<int>(
            async ct =>
            {
                repository.Add(NewMember());
                await repository.RunAsync(c => dbContext.Members.CountAsync(c), ct);
                throw new DomainException(ErrorType.InvalidState, "ng");
            },
            TestContext.Current.CancellationToken));

        Assert.Empty(dbContext.ChangeTracker.Entries());
        await using var verify = database.CreateContext();
        Assert.Equal(0, await verify.Members.CountAsync(TestContext.Current.CancellationToken));
    }
}
