using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">The <see cref="DbContextOptions"/> extension that holds the D3Fragments settings read by <see cref="D3DbContext"/>. Add it with <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments(DbContextOptionsBuilder, IEnumerable{Assembly}, TimeProvider?)"/>.</para>
///     <para xml:lang="ja"><see cref="D3DbContext"/>が参照するD3Fragmentsの設定を保持する<see cref="DbContextOptions"/>拡張。<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments(DbContextOptionsBuilder, IEnumerable{Assembly}, TimeProvider?)"/>で追加する。</para>
/// </summary>
/// <param name="domainAssemblies">
///     <para xml:lang="en">The assemblies scanned for <see cref="TypeSafeEnum{TSelf, TKey}"/> types.</para>
///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>型を走査するアセンブリ。</para>
/// </param>
/// <param name="timeProvider">
///     <para xml:lang="en">The time source used to update <see cref="IUpdateTrackable.UpdatedAt"/>.</para>
///     <para xml:lang="ja"><see cref="IUpdateTrackable.UpdatedAt"/>の更新に使用する時刻ソース。</para>
/// </param>
public sealed class D3FragmentsOptionsExtension(IReadOnlyList<Assembly> domainAssemblies, TimeProvider timeProvider) : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    /// <summary>
    ///     <para xml:lang="en">Gets the assemblies scanned for <see cref="TypeSafeEnum{TSelf, TKey}"/> types.</para>
    ///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>型を走査するアセンブリを取得する。</para>
    /// </summary>
    public IReadOnlyList<Assembly> DomainAssemblies { get; } = domainAssemblies;

    /// <summary>
    ///     <para xml:lang="en">Gets the time source used to update <see cref="IUpdateTrackable.UpdatedAt"/>.</para>
    ///     <para xml:lang="ja"><see cref="IUpdateTrackable.UpdatedAt"/>の更新に使用する時刻ソースを取得する。</para>
    /// </summary>
    public TimeProvider TimeProvider { get; } = timeProvider;

    /// <inheritdoc />
    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        // 設定値を保持するだけで、EF Coreの内部サービスは追加しない
    }

    /// <inheritdoc />
    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using D3Fragments ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }
    }
}
