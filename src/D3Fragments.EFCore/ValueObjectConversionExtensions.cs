using System.Globalization;

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

        var type = typeof(TValueObject);
        var valueObjectInterface = type.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IValueObject<,>) && x.GenericTypeArguments[0] == type)
            ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidOperationException_NotValueObject, type));
        builder.HasConversion(typeof(ValueObjectConverter<,>).MakeGenericType(valueObjectInterface.GenericTypeArguments));
        return builder;
    }
}
