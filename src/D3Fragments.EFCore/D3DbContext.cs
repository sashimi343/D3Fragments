using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Base class for a <see cref="DbContext"/> that persists D3Fragments domain models. It registers value converters for <see cref="UuidV7"/> and <see cref="TypeSafeEnum{TSelf, TKey}"/> properties, configures <see cref="IUpdateTrackable.Version"/> as the concurrency token, and updates <see cref="IUpdateTrackable.UpdatedAt"/> and <see cref="IUpdateTrackable.Version"/> of modified entities on save. Configure it with <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>.</para>
///     <para xml:lang="ja">D3Fragmentsのドメインモデルを永続化する<see cref="DbContext"/>の基底クラス。<see cref="UuidV7"/>・<see cref="TypeSafeEnum{TSelf, TKey}"/>プロパティへの値コンバーター登録、<see cref="IUpdateTrackable.Version"/>の同時実行トークン設定、保存時の変更済みエンティティの<see cref="IUpdateTrackable.UpdatedAt"/>・<see cref="IUpdateTrackable.Version"/>更新を行う。設定は<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>で行う。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Value objects are not converted automatically. Map each value object property as a complex type with <see cref="ValueObjectComplexTypeExtensions.ComplexValueObject"/>. Domain events are not dispatched by this class; save through <c>IUnitOfWork</c> (<see cref="EfUnitOfWork{TContext}"/>) so that they are dispatched after commit. <see cref="IUpdateTrackable.UpdatedAt"/> and <see cref="IUpdateTrackable.Version"/> are updated for each entity in the <see cref="EntityState.Modified"/> state, and also for the aggregate root of each added, modified or deleted child entity, so that concurrency is checked per aggregate. The aggregate root is found by following the foreign keys of the child entity whose principal has a navigation to the dependent (or which are ownerships), up to the first <see cref="AggregateRoot{TId}"/>; foreign keys that reference other aggregates by ID only are not followed. When the aggregate root is not tracked, it is not updated. A concurrency conflict throws <see cref="DbUpdateConcurrencyException"/>, which <see cref="EfUnitOfWork{TContext}"/> converts to <see cref="ConflictOccurredException"/>.</para>
///     <para xml:lang="ja">値オブジェクトは自動変換しない。値オブジェクトのプロパティは<see cref="ValueObjectComplexTypeExtensions.ComplexValueObject"/>でComplex Typeとしてマッピングすること。ドメインイベントはこのクラスではディスパッチしない。Commit後にディスパッチされるよう、<c>IUnitOfWork</c>（<see cref="EfUnitOfWork{TContext}"/>）経由で保存すること。<see cref="IUpdateTrackable.UpdatedAt"/>・<see cref="IUpdateTrackable.Version"/>は、<see cref="EntityState.Modified"/>状態の各エンティティに加え、追加・変更・削除された子エンティティが属する集約ルートについても更新する。これにより同時実行チェックは集約単位となる。集約ルートは、子エンティティの外部キーのうち親側に子へのナビゲーションがあるもの（または所有関係）を最初の<see cref="AggregateRoot{TId}"/>までたどって特定する。他の集約をIDのみで参照する外部キーはたどらない。集約ルートが追跡されていない場合は更新しない。競合時は<see cref="DbUpdateConcurrencyException"/>がスローされ、<see cref="EfUnitOfWork{TContext}"/>が<see cref="ConflictOccurredException"/>に変換する。</para>
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
        MarkModifiedEntitiesAsUpdated();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        MarkModifiedEntitiesAsUpdated();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<UuidV7>().HaveConversion<UuidV7Converter>();
        configurationBuilder.Conventions.Add(_ => new VersionConcurrencyTokenConvention());

        foreach (var type in _extension?.DomainAssemblies.SelectMany(x => x.GetTypes()) ?? [])
        {
            if (type.BaseType is { IsGenericType: true } baseType
                && baseType.GetGenericTypeDefinition() == typeof(TypeSafeEnum<,>)
                && baseType.GenericTypeArguments[0] == type)
            {
                var converterType = typeof(TypeSafeEnumConverter<,>).MakeGenericType(baseType.GenericTypeArguments);
                configurationBuilder.Properties(type).HaveConversion(converterType);
            }
        }
    }

    private void MarkModifiedEntitiesAsUpdated()
    {
        // Entries() は変更検出を行うため、プロパティ変更・子エンティティの追加削除がこの時点で状態に反映される
        var entries = ChangeTracker.Entries().ToList();
        var updated = new HashSet<IUpdateTrackable>(ReferenceEqualityComparer.Instance);
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified && entry.Entity is IUpdateTrackable trackable)
            {
                updated.Add(trackable);
            }

            // 子エンティティの変更は、集約単位で競合を検知するため集約ルートの更新として扱う
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                && !IsAggregateRoot(entry.Entity.GetType())
                && FindAggregateRoot(entry, entries) is { State: EntityState.Unchanged or EntityState.Modified, Entity: IUpdateTrackable root })
            {
                updated.Add(root);
            }
        }

        // 集約ルート自身と子エンティティの両方が変更されていても、更新は1回だけ行う
        foreach (var trackable in updated)
        {
            trackable.MarkUpdated();
        }
    }

    private static EntityEntry? FindAggregateRoot(EntityEntry entry, List<EntityEntry> entries)
    {
        var visited = new HashSet<EntityEntry>();
        for (var current = FindParent(entry, entries); current is not null && visited.Add(current); current = FindParent(current, entries))
        {
            if (IsAggregateRoot(current.Entity.GetType()))
            {
                return current;
            }
        }
        return null;
    }

    // 親側に子へのナビゲーションがある外部キー（または所有関係）を集約の構成とみなし、親のエントリを探す。
    // 他の集約をIDのみで参照する外部キー（親側にナビゲーションがない）はたどらない
    // ponytail: 追跡中の全エントリを線形探索する。1回の保存で扱うエンティティ数が多くなり性能が問題になれば、キーで索引化する
    private static EntityEntry? FindParent(EntityEntry entry, List<EntityEntry> entries)
    {
        foreach (var foreignKey in entry.Metadata.GetForeignKeys().Where(x => x.IsOwnership || x.PrincipalToDependent is not null))
        {
            // 削除された子や、コレクションから外されて外部キーがnullになった子は、変更前の値で親を特定する
            var keyValues = foreignKey.Properties.Select(p => entry.State == EntityState.Deleted ? entry.OriginalValues[p] : entry.CurrentValues[p] ?? entry.OriginalValues[p]).ToArray();
            if (keyValues.Any(x => x is null))
            {
                continue;
            }

            var parent = entries.FirstOrDefault(candidate =>
                foreignKey.PrincipalEntityType.IsAssignableFrom(candidate.Metadata)
                && foreignKey.PrincipalKey.Properties.Select(p => candidate.CurrentValues[p]).SequenceEqual(keyValues));
            if (parent is not null)
            {
                return parent;
            }
        }
        return null;
    }

    private static bool IsAggregateRoot(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
        }
        return false;
    }
}
