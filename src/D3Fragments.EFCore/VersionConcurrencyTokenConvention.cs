using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">A convention that configures <see cref="IUpdateTrackable.Version"/> of every entity type implementing <see cref="IUpdateTrackable"/> as a concurrency token.</para>
///     <para xml:lang="ja"><see cref="IUpdateTrackable"/>を実装するすべてのエンティティ型の<see cref="IUpdateTrackable.Version"/>を同時実行トークンに設定する規約。</para>
/// </summary>
internal sealed class VersionConcurrencyTokenConvention : IModelFinalizingConvention
{
    /// <inheritdoc />
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (typeof(IUpdateTrackable).IsAssignableFrom(entityType.ClrType))
            {
                entityType.FindProperty(nameof(IUpdateTrackable.Version))?.Builder.IsConcurrencyToken(true);
            }
        }
    }
}
