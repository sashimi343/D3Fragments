using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace D3Fragments.EFCore;

/// <summary>
/// ComplexValueObjectでマッピングした値オブジェクトの列名を、EF Core既定の<c>Email_Value</c>からプロパティ名の<c>Email</c>にする規約。
/// 命名規約（EFCore.NamingConventionsなど）が<c>Email_Value</c>を書き換えた結果（<c>email_value</c>など）からも末尾を取り除くため、
/// HasValueObjectConversionでマッピングした場合と同じ列名になる
/// </summary>
internal sealed class ValueObjectColumnNameConvention : IModelFinalizingConvention
{
    // ComplexValueObjectで登録したComplexプロパティに付ける目印
    internal const string ComplexValueObjectAnnotation = "D3Fragments:ComplexValueObject";

    private const string ValueSuffix = "_" + nameof(IValueObject<,>.Value);

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        // 命名規約より後に実行される必要がある。ConfigureConventionsで追加した規約は、拡張機能が追加した規約より後に実行される
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var complexProperty in entityType.GetDeclaredComplexProperties().Where(x => x.FindAnnotation(ComplexValueObjectAnnotation) is not null))
            {
                var property = complexProperty.ComplexType.FindProperty(nameof(IValueObject<,>.Value))!;
                if (property.GetColumnNameConfigurationSource() is ConfigurationSource.Explicit or ConfigurationSource.DataAnnotation)
                {
                    continue;
                }

                // ponytail: 命名規約が「_Value」を大文字小文字の違う「_value」にする前提。区切り文字を変える独自の命名規約では書き換えた名前のまま残る
                var columnName = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) is { } table
                    ? property.GetColumnName(table)
                    : property.GetColumnName();
                if (columnName is not null && columnName.EndsWith(ValueSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    property.Builder.HasColumnName(columnName[..^ValueSuffix.Length]);
                }
            }
        }
    }
}
