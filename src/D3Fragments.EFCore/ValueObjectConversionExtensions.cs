using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Extension methods that map a value object implementing <see cref="IValueObject{TSelf, TPrimitive}"/> to a single column with a value converter. Unlike a complex type mapped with <see cref="ValueObjectComplexTypeExtensions.ComplexValueObject"/>, the property can have an index, and its column name follows the naming convention of the context.</para>
///     <para xml:lang="ja"><see cref="IValueObject{TSelf, TPrimitive}"/>を実装する値オブジェクトを、値コンバーターで単一列にマッピングする拡張メソッド。<see cref="ValueObjectComplexTypeExtensions.ComplexValueObject"/>でマッピングするComplex Typeと異なり、プロパティにインデックスを定義でき、列名はコンテキストの命名規約に従う。</para>
/// </summary>
public static class ValueObjectConversionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Configures the property to be stored as the primitive value that the value object wraps. When read from the database, the value object is created with <see cref="IValueObject{TSelf, TPrimitive}.Create"/>, so the value is validated with <see cref="IValueObject{TSelf, TPrimitive}.ValidateValue"/>. The column name is not set.</para>
    ///     <para xml:lang="ja">プロパティを、値オブジェクトがラップしているプリミティブ値として格納するよう構成する。DBからの読み込み時は<see cref="IValueObject{TSelf, TPrimitive}.Create"/>で値オブジェクトを生成するため、<see cref="IValueObject{TSelf, TPrimitive}.ValidateValue"/>で検証される。列名は設定しない。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Queries cannot use the wrapped value (e.g. <c>x.Email.Value.EndsWith("@example.com")</c>). Compare value objects instead (e.g. <c>x.Email == email</c>).</para>
    ///     <para xml:lang="ja">クエリでラップしている値（例: <c>x.Email.Value.EndsWith("@example.com")</c>）は使えない。値オブジェクトどうしで比較すること（例: <c>x.Email == email</c>）。</para>
    /// </remarks>
    /// <typeparam name="TValueObject">
    ///     <para xml:lang="en">The type of the value object. Must implement <see cref="IValueObject{TSelf, TPrimitive}"/> with itself as <c>TSelf</c>.</para>
    ///     <para xml:lang="ja">値オブジェクトの型。自身を<c>TSelf</c>として<see cref="IValueObject{TSelf, TPrimitive}"/>を実装している必要がある。</para>
    /// </typeparam>
    /// <param name="builder">
    ///     <para xml:lang="en">The builder of the value object property.</para>
    ///     <para xml:lang="ja">値オブジェクトのプロパティのビルダー。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same builder for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じビルダー。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="builder"/> is <c>null</c>.</para>
    ///     <para xml:lang="ja"><paramref name="builder"/>が<c>null</c>の場合にスローされる。</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when <typeparamref name="TValueObject"/> does not implement <see cref="IValueObject{TSelf, TPrimitive}"/> with itself as <c>TSelf</c>.</para>
    ///     <para xml:lang="ja"><typeparamref name="TValueObject"/>が自身を<c>TSelf</c>として<see cref="IValueObject{TSelf, TPrimitive}"/>を実装していない場合にスローされる。</para>
    /// </exception>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Member&gt;(builder =>
    /// {
    ///     builder.Property(x => x.EmailAddress).HasValueObjectConversion().HasMaxLength(254);
    ///     builder.HasIndex(x => x.EmailAddress).IsUnique();
    /// });
    /// </code>
    /// </example>
    public static PropertyBuilder<TValueObject> HasValueObjectConversion<TValueObject>(this PropertyBuilder<TValueObject> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var valueObjectInterface = ValueObjectMapping.GetValueObjectInterface(typeof(TValueObject), nameof(HasValueObjectConversion));
        builder.HasConversion(typeof(ValueObjectConverter<,>).MakeGenericType(valueObjectInterface.GenericTypeArguments));
        return builder;
    }

    /// <summary>
    ///     <para xml:lang="en">Configures the elements of the primitive collection to be stored as the primitive values that the value objects wrap. The collection is stored in a single JSON column as an array of the wrapped values (e.g. <c>["a@example.com"]</c>). When read from the database, each element is created with <see cref="IValueObject{TSelf, TPrimitive}.Create"/>, so the value is validated with <see cref="IValueObject{TSelf, TPrimitive}.ValidateValue"/>.</para>
    ///     <para xml:lang="ja">プリミティブコレクションの要素を、値オブジェクトがラップしているプリミティブ値として格納するよう構成する。コレクションは、ラップしている値の配列（例: <c>["a@example.com"]</c>）として単一のJSON列に格納される。DBからの読み込み時は各要素を<see cref="IValueObject{TSelf, TPrimitive}.Create"/>で生成するため、<see cref="IValueObject{TSelf, TPrimitive}.ValidateValue"/>で検証される。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Queries cannot use the wrapped value of the elements (e.g. <c>x.Aliases.Any(a => a.Value.EndsWith("@example.com"))</c>). Compare value objects instead (e.g. <c>x.Aliases.Contains(email)</c>), or map the collection with <see cref="ValueObjectComplexTypeExtensions.ComplexValueObjectCollection{TEntity, TValueObject}"/>. The property can be exposed as a read-only collection such as <see cref="IReadOnlyList{T}"/>. In that case, the collection is read and written through the backing field, which is found by the EF Core naming convention (e.g. <c>_aliases</c> for a property named <c>Aliases</c>).</para>
    ///     <para xml:lang="ja">クエリで要素のラップしている値（例: <c>x.Aliases.Any(a => a.Value.EndsWith("@example.com"))</c>）は使えない。値オブジェクトどうしで比較する（例: <c>x.Aliases.Contains(email)</c>）か、<see cref="ValueObjectComplexTypeExtensions.ComplexValueObjectCollection{TEntity, TValueObject}"/>でマッピングすること。プロパティは<see cref="IReadOnlyList{T}"/>などの読み取り専用のコレクションとして公開できる。その場合、コレクションはバッキングフィールドを通して読み書きされ、バッキングフィールドはEF Coreの命名規約（例: プロパティ<c>Aliases</c>に対して<c>_aliases</c>）で探される。</para>
    /// </remarks>
    /// <typeparam name="TProperty">
    ///     <para xml:lang="en">The type of the collection property. Its elements must implement <see cref="IValueObject{TSelf, TPrimitive}"/> with themselves as <c>TSelf</c>.</para>
    ///     <para xml:lang="ja">コレクションのプロパティの型。要素は自身を<c>TSelf</c>として<see cref="IValueObject{TSelf, TPrimitive}"/>を実装している必要がある。</para>
    /// </typeparam>
    /// <param name="builder">
    ///     <para xml:lang="en">The builder of the primitive collection property.</para>
    ///     <para xml:lang="ja">プリミティブコレクションのプロパティのビルダー。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same builder for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じビルダー。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="builder"/> is <c>null</c>.</para>
    ///     <para xml:lang="ja"><paramref name="builder"/>が<c>null</c>の場合にスローされる。</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when the element type does not implement <see cref="IValueObject{TSelf, TPrimitive}"/> with itself as <c>TSelf</c>, or when the property has no setter and EF Core finds no backing field for it.</para>
    ///     <para xml:lang="ja">要素の型が自身を<c>TSelf</c>として<see cref="IValueObject{TSelf, TPrimitive}"/>を実装していない場合、またはプロパティにsetterがなくEF Coreがバッキングフィールドを見つけられない場合にスローされる。</para>
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
    /// modelBuilder.Entity&lt;Mailbox&gt;(builder => builder.PrimitiveCollection(x => x.Aliases).HasValueObjectCollectionConversion());
    /// </code>
    /// </example>
    public static PrimitiveCollectionBuilder<TProperty> HasValueObjectCollectionConversion<TProperty>(this PrimitiveCollectionBuilder<TProperty> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // PrimitiveCollection で構成したプロパティには必ず要素型がある
        var elementType = builder.Metadata.GetElementType()!.ClrType;
        var valueObjectInterface = ValueObjectMapping.GetValueObjectInterface(elementType, nameof(HasValueObjectCollectionConversion));
        ValueObjectMapping.EnsureWritable(builder.Metadata);
        builder.ElementType().HasConversion(typeof(ValueObjectConverter<,>).MakeGenericType(valueObjectInterface.GenericTypeArguments));
        return builder;
    }
}
