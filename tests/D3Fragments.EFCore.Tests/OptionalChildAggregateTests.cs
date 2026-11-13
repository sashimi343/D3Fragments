using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore.Tests;

internal sealed class TeamMember : Entity
{
    public int Number { get; private set; }

    public TeamMember(int number)
    {
        Number = number;
    }
}

internal sealed class Team : AggregateRoot
{
    private readonly List<TeamMember> _members = [];

    public IReadOnlyList<TeamMember> Members => _members;

    public void AddMember(int number) => _members.Add(new TeamMember(number));

    public void RemoveMember(TeamMember member) => _members.Remove(member);

    public void AcceptMember(TeamMember member) => _members.Add(member);
}

// 子エンティティの外部キーが任意（コレクションから外すと null になる）集約
internal sealed class TeamDbContext(DbContextOptions<TeamDbContext> options) : D3DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>().HasMany(x => x.Members).WithOne().HasForeignKey("TeamId").IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
    }
}

public sealed class OptionalChildAggregateTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public OptionalChildAggregateTests()
    {
        _connection.Open();
        using var dbContext = CreateContext();
        dbContext.Database.EnsureCreated();
        var team = new Team();
        team.AddMember(1);
        dbContext.Teams.Add(team);
        dbContext.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private TeamDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TeamDbContext>().UseSqlite(_connection).Options);

    [Fact]
    public async Task コレクションから外して外部キーがnullになった子_変更前の集約ルートのVersionが更新される()
    {
        await using (var dbContext = CreateContext())
        {
            var team = await dbContext.Teams.Include(x => x.Members).SingleAsync(TestContext.Current.CancellationToken);
            var member = team.Members[0];

            team.RemoveMember(member);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(EntityState.Unchanged, dbContext.Entry(member).State);
            Assert.Null(dbContext.Entry(member).Property("TeamId").CurrentValue);
        }

        await using var readContext = CreateContext();
        Assert.Equal(1, (await readContext.Teams.SingleAsync(TestContext.Current.CancellationToken)).Version);
    }

    [Fact]
    public async Task 別の集約へ移した子_移動元と移動先の両方の集約ルートのVersionが更新される()
    {
        var destinationId = await AddTeamAsync();
        await using (var dbContext = CreateContext())
        {
            var teams = await dbContext.Teams.Include(x => x.Members).ToListAsync(TestContext.Current.CancellationToken);
            var source = teams.Single(x => x.Id != destinationId);
            var member = source.Members[0];

            source.RemoveMember(member);
            teams.Single(x => x.Id == destinationId).AcceptMember(member);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = CreateContext();
        Assert.All(await readContext.Teams.ToListAsync(TestContext.Current.CancellationToken), x => Assert.Equal(1, x.Version));
    }

    private async Task<UuidV7> AddTeamAsync()
    {
        await using var dbContext = CreateContext();
        var team = new Team();
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return team.Id;
    }
}
