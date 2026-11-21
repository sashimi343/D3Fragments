using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Base class for a <see cref="DbContext"/> that persists D3Fragments domain models. It registers value converters for <see cref="UuidV7"/> and <see cref="TypeSafeEnum{TSelf, TKey}"/> properties, configures <see cref="IUpdateTrackable.Version"/> as the concurrency token, and updates <see cref="IUpdateTrackable.UpdatedAt"/> and <see cref="IUpdateTrackable.Version"/> of modified entities on save. Configure it with <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>.</para>
///     <para xml:lang="ja">D3Fragmentsのドメインモデルを永続化する<see cref="DbContext"/>の基底クラス。<see cref="UuidV7"/>・<see cref="TypeSafeEnum{TSelf, TKey}"/>プロパティへの値コンバーター登録、<see cref="IUpdateTrackable.Version"/>の同時実行トークン設定、保存時の変更済みエンティティの<see cref="IUpdateTrackable.UpdatedAt"/>・<see cref="IUpdateTrackable.Version"/>更新を行う。設定は<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>で行う。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Value objects are not converted automatically. Map each value object property as a complex type with <see cref="ValueObjectComplexTypeExtensions.ComplexValueObject"/>. Domain events are not dispatched by this class; save through <c>IUnitOfWork</c> (<see cref="EfUnitOfWork{TContext}"/>) so that they are dispatched after commit. <see cref="IUpdateTrackable.UpdatedAt"/> and <see cref="IUpdateTrackable.Version"/> are updated for each entity in the <see cref="EntityState.Modified"/> state, and also for the aggregate root of each added, modified or deleted child entity, so that concurrency is checked per aggregate. The aggregate root is found by following the foreign keys of the child entity whose principal has a navigation to the dependent (or which are ownerships), up to the first <see cref="AggregateRoot{TId}"/>; foreign keys that reference other aggregates by ID only are not followed. When the aggregate root is not tracked, it is not updated. A concurrency conflict throws <see cref="DbUpdateConcurrencyException"/>, which <see cref="EfUnitOfWork{TContext}"/> converts to <see cref="ConflictOccurredException"/>.</para>
///     <para xml:lang="ja">値オブジェクトは自動変換しない。値オブジェクトのプロパティは<see cref="ValueObjectComplexTypeExtensions.ComplexValueObject"/>でComplex Typeとしてマッピングすること。ドメインイベントはこのクラスではディスパッチしない。Commit後にディスパッチされるよう、<c>IUnitOfWork</c>（<see cref="EfUnitOfWork{TContext}"/>）経由で保存すること。<see cref="IUpdateTrackable.UpdatedAt"/>・<see cref="IUpdateTrackable.Version"/>は、<see cref="EntityState.Modified"/>状態の各エンティティに加え、追加・変更・削除された子エンティティが属する集約ルートについても更新する。これにより同時実行チェックは集約単位となる。集約ルートは、子エンティティの外部キーのうち親側に子へのナビゲーションがあるもの（または所有関係）を最初の<see cref="AggregateRoot{TId}"/>までたどって特定する。他の集約をIDのみで参照する外部キーはたどらない。集約ルートが追跡されていない場合は更新しない。競合時は<see cref="DbUpdateConcurrencyException"/>がスローされ、<see cref="EfUnitOfWork{TContext}"/>が<see cref="ConflictOccurredException"/>に変換する。</para>
///     <para xml:lang="en">To derive from another base class (such as <c>IdentityDbContext</c>), call <see cref="D3FragmentsModelConfigurationBuilderExtensions.ApplyD3FragmentsConventions"/> in <see cref="DbContext.ConfigureConventions"/> and configure the context with <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/> instead of deriving from this class. The same behavior is then provided by an interceptor.</para>
///     <para xml:lang="ja">別の基底クラス（<c>IdentityDbContext</c>など）から派生する場合は、このクラスを継承する代わりに、<see cref="DbContext.ConfigureConventions"/>で<see cref="D3FragmentsModelConfigurationBuilderExtensions.ApplyD3FragmentsConventions"/>を呼び出し、<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>で構成する。同じ動作をインターセプターが提供する。</para>
/// </remarks>
public abstract class D3DbContext : DbContext
{
    private readonly D3FragmentsOptionsExtension? _extension;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance with the specified options. The settings added by <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/> are read from <paramref name="options"/>; settings added in <see cref="DbContext.OnConfiguring"/> are not.</para>
    ///     <para xml:lang="ja">指定したオプションで新しいインスタンスを初期化する。<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>で追加した設定は<paramref name="options"/>から読み取る。<see cref="DbContext.OnConfiguring"/>内で追加した設定は読み取らない。</para>
    /// </summary>
    /// <param name="options">
    ///     <para xml:lang="en">The options for this context.</para>
    ///     <para xml:lang="ja">このコンテキストのオプション。</para>
    /// </param>
    protected D3DbContext(DbContextOptions options) : base(options)
    {
        _extension = options.FindExtension<D3FragmentsOptionsExtension>();
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AggregateUpdateTracker.MarkModifiedEntitiesAsUpdated(this);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AggregateUpdateTracker.MarkModifiedEntitiesAsUpdated(this);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);

        D3FragmentsModelConfigurationBuilderExtensions.ApplyConventions(configurationBuilder, _extension);
    }
}
