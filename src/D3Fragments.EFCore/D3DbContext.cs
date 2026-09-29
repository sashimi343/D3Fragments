using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Base class for a <see cref="DbContext"/> that persists D3Fragments domain models. It registers value converters for <see cref="UuidV7"/> and <see cref="TypeSafeEnum{TSelf, TKey}"/> properties, and updates <see cref="IUpdateTrackable.UpdatedAt"/> of modified entities on save. Configure it with <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>.</para>
///     <para xml:lang="ja">D3Fragmentsのドメインモデルを永続化する<see cref="DbContext"/>の基底クラス。<see cref="UuidV7"/>・<see cref="TypeSafeEnum{TSelf, TKey}"/>プロパティへの値コンバーター登録と、保存時の変更済みエンティティの<see cref="IUpdateTrackable.UpdatedAt"/>更新を行う。設定は<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>で行う。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Value objects are not converted automatically. Map each value object property as a complex type with <see cref="ValueObjectComplexTypeExtensions.ComplexValueObject"/>. Domain events are not dispatched by this class; save through <c>IUnitOfWork</c> (<see cref="EfUnitOfWork{TContext}"/>) so that they are dispatched after commit. <see cref="IUpdateTrackable.UpdatedAt"/> is updated only when the entity itself is in the <see cref="EntityState.Modified"/> state; changing only a child entity does not update the aggregate root's timestamp.</para>
///     <para xml:lang="ja">値オブジェクトは自動変換しない。値オブジェクトのプロパティは<see cref="ValueObjectComplexTypeExtensions.ComplexValueObject"/>でComplex Typeとしてマッピングすること。ドメインイベントはこのクラスではディスパッチしない。Commit後にディスパッチされるよう、<c>IUnitOfWork</c>（<see cref="EfUnitOfWork{TContext}"/>）経由で保存すること。<see cref="IUpdateTrackable.UpdatedAt"/>はエンティティ自身が<see cref="EntityState.Modified"/>状態の場合のみ更新され、子エンティティのみを変更しても集約ルートのタイムスタンプは更新されない。</para>
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
        UpdateTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<UuidV7>().HaveConversion<UuidV7Converter>();

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

    private void UpdateTimestamps()
    {
        var now = (_extension?.TimeProvider ?? TimeProvider.System).GetUtcNow();

        // Entries() は変更検出を行うため、プロパティ変更がこの時点で Modified に反映される
        foreach (var entry in ChangeTracker.Entries<IUpdateTrackable>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdateTimestamp(now);
            }
        }
    }
}
