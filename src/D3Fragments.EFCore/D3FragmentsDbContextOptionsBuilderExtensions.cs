using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Extension methods that configure D3Fragments on a <see cref="DbContextOptionsBuilder"/>.</para>
///     <para xml:lang="ja"><see cref="DbContextOptionsBuilder"/>にD3Fragmentsの設定を行う拡張メソッド。</para>
/// </summary>
public static class D3FragmentsDbContextOptionsBuilderExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Configures the settings used by <see cref="D3DbContext"/>. Call this together with the database provider (e.g. <c>UseNpgsql</c>) when registering the <see cref="DbContext"/>.</para>
    ///     <para xml:lang="ja"><see cref="D3DbContext"/>が使用する設定を行う。<see cref="DbContext"/>の登録時に、DBプロバイダの指定（<c>UseNpgsql</c>等）と合わせて呼び出す。</para>
    /// </summary>
    /// <param name="optionsBuilder">
    ///     <para xml:lang="en">The options builder.</para>
    ///     <para xml:lang="ja">オプションビルダー。</para>
    /// </param>
    /// <param name="domainAssemblies">
    ///     <para xml:lang="en">The assemblies that define <see cref="TypeSafeEnum{TSelf, TKey}"/> types. A value converter that persists the key is registered for every type found.</para>
    ///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>型を定義しているアセンブリ。見つかったすべての型に、キーで永続化する値コンバーターを登録する。</para>
    /// </param>
    /// <param name="timeProvider">
    ///     <para xml:lang="en">The time source used to update <see cref="IUpdateTrackable.UpdatedAt"/>. Defaults to <see cref="TimeProvider.System"/> when <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><see cref="IUpdateTrackable.UpdatedAt"/>の更新に使用する時刻ソース。<see langword="null"/>の場合は<see cref="TimeProvider.System"/>を使用する。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same options builder for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じオプションビルダー。</para>
    /// </returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) => options
    ///     .UseNpgsql(connectionString)
    ///     .UseD3Fragments([typeof(Order).Assembly], sp.GetService&lt;TimeProvider&gt;()));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder UseD3Fragments(
        this DbContextOptionsBuilder optionsBuilder,
        IEnumerable<Assembly> domainAssemblies,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(domainAssemblies);

        var extension = new D3FragmentsOptionsExtension([.. domainAssemblies], timeProvider ?? TimeProvider.System);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }

    /// <inheritdoc cref="UseD3Fragments(DbContextOptionsBuilder, IEnumerable{Assembly}, TimeProvider?)"/>
    /// <typeparam name="TContext">
    ///     <para xml:lang="en">The type of the <see cref="DbContext"/>.</para>
    ///     <para xml:lang="ja"><see cref="DbContext"/>の型。</para>
    /// </typeparam>
    public static DbContextOptionsBuilder<TContext> UseD3Fragments<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        IEnumerable<Assembly> domainAssemblies,
        TimeProvider? timeProvider = null)
        where TContext : DbContext =>
        (DbContextOptionsBuilder<TContext>)UseD3Fragments((DbContextOptionsBuilder)optionsBuilder, domainAssemblies, timeProvider);
}
