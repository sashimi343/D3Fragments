using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Extension methods that apply the D3Fragments conventions to a <see cref="ModelConfigurationBuilder"/>.</para>
///     <para xml:lang="ja"><see cref="ModelConfigurationBuilder"/>にD3Fragmentsの規約を適用する拡張メソッド。</para>
/// </summary>
public static class D3FragmentsModelConfigurationBuilderExtensions
{
    // 規約を適用したモデルに付ける目印。D3SaveChangesInterceptorが規約の呼び忘れを検出するために使う
    internal const string ConventionsAppliedAnnotation = "D3Fragments:ConventionsApplied";

    /// <summary>
    ///     <para xml:lang="en">Applies the conventions that <see cref="D3DbContext"/> applies, for a <see cref="DbContext"/> that does not derive from <see cref="D3DbContext"/>: value converters for <see cref="UuidV7"/> and <see cref="TypeSafeEnum{TSelf, TKey}"/> properties, <see cref="IUpdateTrackable.Version"/> as the concurrency token, and the check for unmapped value objects. Call this in <see cref="DbContext.ConfigureConventions"/>, and configure the context with <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/> so that <see cref="IUpdateTrackable.UpdatedAt"/> and <see cref="IUpdateTrackable.Version"/> are updated on save.</para>
    ///     <para xml:lang="ja"><see cref="D3DbContext"/>を継承しない<see cref="DbContext"/>に、<see cref="D3DbContext"/>と同じ規約を適用する。<see cref="UuidV7"/>・<see cref="TypeSafeEnum{TSelf, TKey}"/>プロパティへの値コンバーター登録、<see cref="IUpdateTrackable.Version"/>の同時実行トークン設定、マッピングされていない値オブジェクトの検出を行う。<see cref="DbContext.ConfigureConventions"/>で呼び出し、保存時に<see cref="IUpdateTrackable.UpdatedAt"/>・<see cref="IUpdateTrackable.Version"/>が更新されるよう、<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>で構成すること。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">The <see cref="TypeSafeEnum{TSelf, TKey}"/> types are found in the assemblies passed to <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>, read from <paramref name="options"/>. Settings added in <see cref="DbContext.OnConfiguring"/> are not read. When a context configured with <see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/> is saved without these conventions, an <see cref="InvalidOperationException"/> is thrown.</para>
    ///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>型は、<paramref name="options"/>から読み取った<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>のアセンブリから探す。<see cref="DbContext.OnConfiguring"/>内で追加した設定は読み取らない。<see cref="D3FragmentsDbContextOptionsBuilderExtensions.UseD3Fragments"/>で構成したコンテキストを、この規約を適用せずに保存すると<see cref="InvalidOperationException"/>がスローされる。</para>
    /// </remarks>
    /// <param name="configurationBuilder">
    ///     <para xml:lang="en">The model configuration builder passed to <see cref="DbContext.ConfigureConventions"/>.</para>
    ///     <para xml:lang="ja"><see cref="DbContext.ConfigureConventions"/>に渡されたモデル構成ビルダー。</para>
    /// </param>
    /// <param name="options">
    ///     <para xml:lang="en">The options passed to the constructor of the context.</para>
    ///     <para xml:lang="ja">コンテキストのコンストラクタに渡されたオプション。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same model configuration builder for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じモデル構成ビルダー。</para>
    /// </returns>
    /// <example>
    /// <code>
    /// public sealed class AppDbContext(DbContextOptions&lt;AppDbContext&gt; options)
    ///     : IdentityDbContext&lt;Account, IdentityRole&lt;Guid&gt;, Guid&gt;(options)
    /// {
    ///     protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    ///     {
    ///         base.ConfigureConventions(configurationBuilder);
    ///         configurationBuilder.ApplyD3FragmentsConventions(options);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static ModelConfigurationBuilder ApplyD3FragmentsConventions(this ModelConfigurationBuilder configurationBuilder, DbContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(options);

        ApplyConventions(configurationBuilder, options.FindExtension<D3FragmentsOptionsExtension>());
        return configurationBuilder;
    }

    internal static void ApplyConventions(ModelConfigurationBuilder configurationBuilder, D3FragmentsOptionsExtension? extension)
    {
        configurationBuilder.Properties<UuidV7>().HaveConversion<UuidV7Converter>();
        configurationBuilder.Conventions.Add(_ => new VersionConcurrencyTokenConvention());
        configurationBuilder.Conventions.Add(_ => new UnmappedValueObjectConvention());
        configurationBuilder.Conventions.Add(_ => new ConventionsAppliedConvention());

        foreach (var type in extension?.DomainAssemblies.SelectMany(x => x.GetTypes()) ?? [])
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

    internal static bool AreConventionsApplied(IModel model) => model.FindAnnotation(ConventionsAppliedAnnotation)?.Value is true;

    private sealed class ConventionsAppliedConvention : IModelInitializedConvention
    {
        public void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context) =>
            modelBuilder.HasAnnotation(ConventionsAppliedAnnotation, true);
    }
}
