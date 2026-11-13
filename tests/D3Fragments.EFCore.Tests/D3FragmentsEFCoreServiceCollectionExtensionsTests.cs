using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;
using D3Fragments.Testing;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.EFCore.Tests;

internal interface ICustomMemberRepository : IRepository<Member>;

internal sealed class CustomMemberRepository(MemberDbContext dbContext)
    : RepositoryBase<MemberDbContext, Member>(dbContext), ICustomMemberRepository;

public class D3FragmentsEFCoreServiceCollectionExtensionsTests
{
    private sealed class NoopDispatchStrategy : IDomainEventDispatchStrategy
    {
        public Task PrepareAsync(DbContext dbContext, IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DispatchAfterCommitAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // テストアセンブリには重複検出用のRepositoryがあるため、公開メソッドの走査対象にはRepositoryを含まないライブラリ本体を渡す
    private static ServiceCollection CreateServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IDomainEventDispatcher, SpyDomainEventDispatcher>();
        configure?.Invoke(services);
        services.AddD3FragmentsEFCore<MemberDbContext>(
            [typeof(D3DbContext).Assembly],
            db => db.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services;
    }

    [Fact]
    public void DbContextとUnitOfWorkと既定のDispatch戦略が登録される()
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<EfUnitOfWork<MemberDbContext>>(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        Assert.IsType<ImmediateDispatchStrategy>(scope.ServiceProvider.GetRequiredService<IDomainEventDispatchStrategy>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DbContextOptions<MemberDbContext>>().FindExtension<D3FragmentsOptionsExtension>());
    }

    [Fact]
    public void 事前に登録したDispatch戦略は置き換えない()
    {
        using var provider = CreateServices(services => services.AddScoped<IDomainEventDispatchStrategy, NoopDispatchStrategy>())
            .BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<NoopDispatchStrategy>(scope.ServiceProvider.GetRequiredService<IDomainEventDispatchStrategy>());
    }

    [Fact]
    public void Repositoryは実装しているIRepositoryとその派生インターフェースで登録される()
    {
        var services = new ServiceCollection();

        D3FragmentsEFCoreServiceCollectionExtensions.AddRepositories(services, [typeof(CustomMemberRepository), typeof(Member)]);

        Assert.Equal(
            new HashSet<Type> { typeof(ICustomMemberRepository), typeof(IRepository<Member>), typeof(IRepository<Member, UuidV7>) },
            services.Where(x => x.ImplementationType == typeof(CustomMemberRepository) && x.Lifetime == ServiceLifetime.Scoped)
                .Select(x => x.ServiceType)
                .ToHashSet());
        Assert.Equal(3, services.Count);
    }

    [Fact]
    public void 同じ型が複数回含まれていても重複とみなさない()
    {
        var services = new ServiceCollection();

        D3FragmentsEFCoreServiceCollectionExtensions.AddRepositories(services, [typeof(MemberRepository), typeof(MemberRepository)]);

        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void 同じRepositoryインターフェースの実装が複数ある場合はInvalidOperationExceptionをスローする()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            D3FragmentsEFCoreServiceCollectionExtensions.AddRepositories(services, [typeof(MemberRepository), typeof(CustomMemberRepository)]));
    }

    [Fact]
    public void DbContextは指定した設定とD3Fragmentsの設定で構成される()
    {
        using var provider = CreateServices().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<MemberDbContext>>();

        // D3Fragments の設定はプロバイダーではないため、プロバイダーは指定したものだけになる
        Assert.Single(options.Extensions, x => x.Info.IsDatabaseProvider);
        Assert.Contains(options.Extensions, x => x is D3FragmentsOptionsExtension);
    }

    [Fact]
    public void 指定したアセンブリのRepositoryを走査して登録する()
    {
        // テストアセンブリには同じインターフェースの実装が複数あるため、走査されていれば重複として検出される
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddD3FragmentsEFCore<MemberDbContext>([typeof(MemberRepository).Assembly], db => db.UseInMemoryDatabase("x")));
    }

    [Fact]
    public void 不正な引数は例外になる()
    {
        var services = new ServiceCollection();
        Action<DbContextOptionsBuilder> configure = db => db.UseInMemoryDatabase("x");

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddD3FragmentsEFCore<MemberDbContext>([typeof(D3DbContext).Assembly], configure));
        Assert.Throws<ArgumentNullException>(() => services.AddD3FragmentsEFCore<MemberDbContext>(null!, configure));
        Assert.Throws<ArgumentNullException>(() => services.AddD3FragmentsEFCore<MemberDbContext>([typeof(D3DbContext).Assembly], null!));
        Assert.Throws<ArgumentNullException>(() => ((DbContextOptionsBuilder)null!).UseD3Fragments([]));
        Assert.Throws<ArgumentNullException>(() => new DbContextOptionsBuilder().UseD3Fragments(null!));
    }

    [Fact]
    public void アセンブリが空の場合はArgumentExceptionをスローする()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>("assemblies", () =>
            services.AddD3FragmentsEFCore<MemberDbContext>([], db => db.UseInMemoryDatabase("x")));
    }
}
