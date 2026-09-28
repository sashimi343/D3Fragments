using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">An EF Core value converter that persists a <see cref="TypeSafeEnum{TSelf, TKey}"/> value as its key.</para>
///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>の値をキーとして永続化するEF Core用の値コンバーター。</para>
/// </summary>
/// <typeparam name="TEnum">
///     <para xml:lang="en">The <see cref="TypeSafeEnum{TSelf, TKey}"/>-derived type.</para>
///     <para xml:lang="ja"><see cref="TypeSafeEnum{TSelf, TKey}"/>の派生型。</para>
/// </typeparam>
/// <typeparam name="TKey">
///     <para xml:lang="en">The type of the key.</para>
///     <para xml:lang="ja">キーの型。</para>
/// </typeparam>
/// <example>
/// <code>
/// builder.Property(x => x.Currency).HasConversion&lt;TypeSafeEnumConverter&lt;Currency, string&gt;&gt;();
/// </code>
/// </example>
public sealed class TypeSafeEnumConverter<TEnum, TKey> : ValueConverter<TEnum, TKey>
    where TEnum : TypeSafeEnum<TEnum, TKey>
    where TKey : notnull
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new converter. Reading an undefined key from the database throws an <see cref="ArgumentException"/>.</para>
    ///     <para xml:lang="ja">新しいコンバーターを初期化する。DBから未定義のキーを読み取った場合は<see cref="ArgumentException"/>をスローする。</para>
    /// </summary>
    public TypeSafeEnumConverter() : base(value => value.Key, key => TypeSafeEnum<TEnum, TKey>.Parse(key))
    {
    }
}
