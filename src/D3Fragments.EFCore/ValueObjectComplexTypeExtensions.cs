using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Extension methods that map a value object implementing <see cref="IValueObject{TSelf, TPrimitive}"/> as an EF Core complex type. Unlike a value converter, a complex type lets queries use the primitive value directly (e.g. <c>x.Email.Value.Contains("@example.com")</c>).</para>
///     <para xml:lang="ja"><see cref="IValueObject{TSelf, TPrimitive}"/>を実装する値オブジェクトをEF CoreのComplex Typeとしてマッピングする拡張メソッド。値コンバーターと異なり、クエリでプリミティブ値を直接扱える（例: <c>x.Email.Value.Contains("@example.com")</c>）。</para>
/// </summary>
public static class ValueObjectComplexTypeExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Maps a value object property as a complex type stored in a single column named after the property (e.g. <c>Email</c> instead of EF Core's default <c>Email_Value</c>). Use the returned builder to override the column name, length, and so on.</para>
    ///     <para xml:lang="ja">値オブジェクトのプロパティを、プロパティ名の単一列（EF Core既定の<c>Email_Value</c>ではなく<c>Email</c>）に格納するComplex Typeとしてマッピングする。列名・長さ等は戻り値のビルダーで上書きできる。</para>
    /// </summary>
    /// <typeparam name="TEntity">
    ///     <para xml:lang="en">The type of the entity.</para>
    ///     <para xml:lang="ja">エンティティの型。</para>
    /// </typeparam>
    /// <typeparam name="TValueObject">
    ///     <para xml:lang="en">The type of the value object. Must implement <see cref="IValueObject{TSelf, TPrimitive}"/>.</para>
    ///     <para xml:lang="ja">値オブジェクトの型。<see cref="IValueObject{TSelf, TPrimitive}"/>を実装している必要がある。</para>
    /// </typeparam>
    /// <param name="builder">
    ///     <para xml:lang="en">The entity type builder.</para>
    ///     <para xml:lang="ja">エンティティ型ビルダー。</para>
    /// </param>
    /// <param name="propertyExpression">
    ///     <para xml:lang="en">An expression that selects the value object property.</para>
    ///     <para xml:lang="ja">値オブジェクトのプロパティを選択する式。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The builder of the complex property.</para>
    ///     <para xml:lang="ja">Complexプロパティのビルダー。</para>
    /// </returns>
    /// <example>
    /// <code>
    /// builder.ComplexValueObject(x => x.Email)
    ///     .Property(x => x.Value).HasMaxLength(256);
    /// </code>
    /// </example>
    public static ComplexPropertyBuilder<TValueObject> ComplexValueObject<TEntity, TValueObject>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TValueObject?>> propertyExpression)
        where TEntity : class
        where TValueObject : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        var complexProperty = builder.ComplexProperty(propertyExpression);
        // TPrimitive は型推論できないため、IValueObject<,>.Value を名前で指定する
        complexProperty.Property(nameof(IValueObject<,>.Value)).HasColumnName(complexProperty.Metadata.Name);
        return complexProperty;
    }
}
