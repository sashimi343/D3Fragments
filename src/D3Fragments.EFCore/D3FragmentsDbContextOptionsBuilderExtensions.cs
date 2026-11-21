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
    ///     <para xml:lang="en">Configures the settings used by <see cref="D3DbContext"/> and <see cref="D3FragmentsModelConfigurationBuilderExtensions.ApplyD3FragmentsConventions"/>. Call this together with the database provider (e.g. <c>UseNpgsql</c>) when registering the <see cref="DbContext"/>.</para>
    ///     <para xml:lang="ja"><see cref="D3DbContext"/>と<see cref="D3FragmentsModelConfigurationBuilderExtensions.ApplyD3FragmentsConventions"/>が使用する設定を行う。<see cref="DbContext"/>の登録時に、DBプロバイダの指定（<c>UseNpgsql</c>等）と合わせて呼び出す。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">This method also adds an interceptor that updates <see cref="IUpdateTrackable.UpdatedAt"/> and <see cref="IUpdateTrackable.Version"/> on save for a context that does not derive from <see cref="D3DbContext"/>. The interceptor does nothing for <see cref="D3DbContext"/>, which updates them itself, and is added only once even if this method is called more than once.</para>
    ///     <para xml:lang="ja">このメソッドは、<see cref="D3DbContext"/>を継承しないコンテキストで保存時に<see cref="IUpdateTrackable.UpdatedAt"/>・<see cref="IUpdateTrackable.Version"/>を更新するインターセプターも追加する。自身で更新する<see cref="D3DbContext"/>ではインターセプターは何もしない。このメソッドを複数回呼び出しても、インターセプターは1つだけ追加する。</para>
    /// </remarks>
    /// <param name="optionsBuilder">
    ///     <para xml:lang="en">The options builder.</para>
    ///     <para xml:lang="ja">オプションビルダー。</para>
    /// </param>
    /// <param name="domainAssemblies">
    ///     <para xml:lang="en">The assemblies that define <see cref="TypeSafeEnum{TSelf, TKey}"/> types. A value converter that persists the key is registered for every type found.</para>
    ///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>型を定義しているアセンブリ。見つかったすべての型に、キーで永続化する値コンバーターを登録する。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same options builder for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じオプションビルダー。</para>
    /// </returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;(options => options
    ///     .UseNpgsql(connectionString)
    ///     .UseD3Fragments([typeof(Order).Assembly]));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder UseD3Fragments(
        this DbContextOptionsBuilder optionsBuilder,
        IEnumerable<Assembly> domainAssemblies)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(domainAssemblies);

        var extension = new D3FragmentsOptionsExtension([.. domainAssemblies]);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        // 複数回呼ばれても、保存時の更新が二重に行われないよう1つだけ登録する
        var interceptors = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()?.Interceptors ?? [];
        if (!interceptors.Contains(D3SaveChangesInterceptor.Instance))
        {
            optionsBuilder.AddInterceptors(D3SaveChangesInterceptor.Instance);
        }

        return optionsBuilder;
    }

    /// <inheritdoc cref="UseD3Fragments(DbContextOptionsBuilder, IEnumerable{Assembly})"/>
    /// <typeparam name="TContext">
    ///     <para xml:lang="en">The type of the <see cref="DbContext"/>.</para>
    ///     <para xml:lang="ja"><see cref="DbContext"/>の型。</para>
    /// </typeparam>
    public static DbContextOptionsBuilder<TContext> UseD3Fragments<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        IEnumerable<Assembly> domainAssemblies)
        where TContext : DbContext =>
        (DbContextOptionsBuilder<TContext>)UseD3Fragments((DbContextOptionsBuilder)optionsBuilder, domainAssemblies);
}
