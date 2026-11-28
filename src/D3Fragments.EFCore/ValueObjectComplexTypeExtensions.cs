using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="builder"/> is <c>null</c>.</para>
    ///     <para xml:lang="ja"><paramref name="builder"/>が<c>null</c>の場合にスローされる。</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when <typeparamref name="TValueObject"/> does not implement <see cref="IValueObject{TSelf, TPrimitive}"/> with itself as <c>TSelf</c>. For a collection of value objects, use <see cref="ComplexValueObjectCollection{TEntity, TValueObject}"/>.</para>
    ///     <para xml:lang="ja"><typeparamref name="TValueObject"/>が自身を<c>TSelf</c>として<see cref="IValueObject{TSelf, TPrimitive}"/>を実装していない場合にスローされる。値オブジェクトのコレクションには<see cref="ComplexValueObjectCollection{TEntity, TValueObject}"/>を使用する。</para>
    /// </exception>
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
        // コレクション型も型制約を満たすため、値オブジェクトかを検査する（しないと EF Core の分かりにくい例外になる）
        ValueObjectMapping.GetValueObjectInterface(typeof(TValueObject), nameof(ComplexValueObject));

        var complexProperty = builder.ComplexProperty(propertyExpression);
        // TPrimitive は型推論できないため、IValueObject<,>.Value を名前で指定する
        complexProperty.Property(nameof(IValueObject<,>.Value)).HasColumnName(complexProperty.Metadata.Name);
        return complexProperty;
    }

    /// <summary>
    ///     <para xml:lang="en">Maps a collection of value objects as a complex collection stored in a single JSON column named after the property. Each element is stored as a JSON object that holds the wrapped value (e.g. <c>[{"Value":"a@example.com"}]</c>), so queries can use the wrapped value of the elements (e.g. <c>x.Aliases.Any(a => a.Value.EndsWith("@example.com"))</c>).</para>
    ///     <para xml:lang="ja">値オブジェクトのコレクションを、プロパティ名の単一のJSON列に格納するComplex Collectionとしてマッピングする。各要素はラップしている値を持つJSONオブジェクト（例: <c>[{"Value":"a@example.com"}]</c>）として格納されるため、クエリで要素のラップしている値を扱える（例: <c>x.Aliases.Any(a => a.Value.EndsWith("@example.com"))</c>）。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">The property can be exposed as a read-only collection such as <see cref="IReadOnlyList{T}"/>. In that case, the collection is read and written through the backing field, which must be a <see cref="List{T}"/>. The backing field is found by the EF Core naming convention (e.g. <c>_aliases</c> for a property named <c>Aliases</c>).</para>
    ///     <para xml:lang="ja">プロパティは<see cref="IReadOnlyList{T}"/>などの読み取り専用のコレクションとして公開できる。その場合、コレクションはバッキングフィールドを通して読み書きされ、バッキングフィールドは<see cref="List{T}"/>である必要がある。バッキングフィールドはEF Coreの命名規約（例: プロパティ<c>Aliases</c>に対して<c>_aliases</c>）で探される。</para>
    /// </remarks>
    /// <typeparam name="TEntity">
    ///     <para xml:lang="en">The type of the entity.</para>
    ///     <para xml:lang="ja">エンティティの型。</para>
    /// </typeparam>
    /// <typeparam name="TValueObject">
    ///     <para xml:lang="en">The type of the elements. Must implement <see cref="IValueObject{TSelf, TPrimitive}"/> with itself as <c>TSelf</c>.</para>
    ///     <para xml:lang="ja">要素の型。自身を<c>TSelf</c>として<see cref="IValueObject{TSelf, TPrimitive}"/>を実装している必要がある。</para>
    /// </typeparam>
    /// <param name="builder">
    ///     <para xml:lang="en">The entity type builder.</para>
    ///     <para xml:lang="ja">エンティティ型ビルダー。</para>
    /// </param>
    /// <param name="propertyExpression">
    ///     <para xml:lang="en">An expression that selects the collection property (e.g. <c>x => x.Aliases</c>).</para>
    ///     <para xml:lang="ja">コレクションのプロパティを選択する式（例: <c>x => x.Aliases</c>）。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The builder of the complex collection.</para>
    ///     <para xml:lang="ja">Complex Collectionのビルダー。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="builder"/> or <paramref name="propertyExpression"/> is <c>null</c>.</para>
    ///     <para xml:lang="ja"><paramref name="builder"/>または<paramref name="propertyExpression"/>が<c>null</c>の場合にスローされる。</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="propertyExpression"/> is not a simple property access.</para>
    ///     <para xml:lang="ja"><paramref name="propertyExpression"/>が単純なプロパティアクセスでない場合にスローされる。</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when <typeparamref name="TValueObject"/> does not implement <see cref="IValueObject{TSelf, TPrimitive}"/> with itself as <c>TSelf</c>, or when the property has no setter and EF Core finds no backing field for it.</para>
    ///     <para xml:lang="ja"><typeparamref name="TValueObject"/>が自身を<c>TSelf</c>として<see cref="IValueObject{TSelf, TPrimitive}"/>を実装していない場合、またはプロパティにsetterがなくEF Coreがバッキングフィールドを見つけられない場合にスローされる。</para>
    /// </exception>
    /// <example>
    /// <code>
    /// public sealed class Mailbox : AggregateRoot
    /// {
    ///     private readonly List&lt;Email&gt; _aliases = [];
    ///
    ///     public IReadOnlyList&lt;Email&gt; Aliases => _aliases;
    /// }
    ///
    /// modelBuilder.Entity&lt;Mailbox&gt;(builder => builder.ComplexValueObjectCollection(x => x.Aliases));
    /// </code>
    /// </example>
    public static ComplexCollectionBuilder<TValueObject> ComplexValueObjectCollection<TEntity, TValueObject>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IEnumerable<TValueObject>?>> propertyExpression)
        where TEntity : class
        where TValueObject : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);
        ValueObjectMapping.GetValueObjectInterface(typeof(TValueObject), nameof(ComplexValueObjectCollection));

        // EF Core は Complex Collection のプロパティ型に IList<T> を要求し、IReadOnlyList<T> で公開したプロパティは式で登録できない。
        // そのため名前で List<T> として登録し、読み書きは EF Core の規約で見つけたバッキングフィールドに任せる
        var complexCollection = builder.ComplexCollection<List<TValueObject>, TValueObject>(propertyExpression.GetMemberAccess().Name);
        ValueObjectMapping.EnsureWritable(complexCollection.Metadata);
        complexCollection.ToJson();
        complexCollection.Property(nameof(IValueObject<,>.Value));
        return complexCollection;
    }
}
