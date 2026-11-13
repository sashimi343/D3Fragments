using System.Globalization;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">A convention that fails model building with a clear message when a value object is mapped as an entity type, which happens when <see cref="ValueObjectComplexTypeExtensions.ComplexValueObject{TEntity, TValueObject}"/> registration is missing.</para>
///     <para xml:lang="ja">値オブジェクトがエンティティ型としてマッピングされている場合（<see cref="ValueObjectComplexTypeExtensions.ComplexValueObject{TEntity, TValueObject}"/>での登録漏れ）に、原因の分かるメッセージでモデル構築を失敗させる規約。</para>
/// </summary>
internal sealed class UnmappedValueObjectConvention : IModelFinalizingConvention
{
    /// <inheritdoc />
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (!entityType.ClrType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IValueObject<,>)))
            {
                continue;
            }

            // 値オブジェクトを指すナビゲーションを集める。単一の参照では参照元が外部キーを持ち（値オブジェクトが主側）、
            // コレクションでは値オブジェクトが外部キーを持つ（値オブジェクトが依存側）
            var properties = entityType.GetReferencingForeignKeys().Select(x => x.DependentToPrincipal)
                .Concat(entityType.GetForeignKeys().Select(x => x.PrincipalToDependent))
                .OfType<IConventionNavigation>()
                .Select(x => $"{x.DeclaringEntityType.ClrType.Name}.{x.Name}");
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.InvalidOperationException_ValueObjectMappedAsEntity,
                entityType.ClrType,
                string.Join(", ", properties)));
        }
    }
}
