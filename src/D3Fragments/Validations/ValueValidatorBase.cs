namespace D3Fragments.Validations;

/// <summary>
///     <para xml:lang="en">Base class for validators of value types. <see langword="null"/> is treated as a missing value.</para>
///     <para xml:lang="ja">値型のバリデータの基底クラス。<see langword="null"/>を値が存在しないものとして扱う。</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The value type to validate.</para>
///     <para xml:lang="ja">検証対象の値型。</para>
/// </typeparam>
public abstract class ValueValidatorBase<T> : ValidatorBase<T>
    where T : struct
{
    /// <inheritdoc cref="ValidatorBase{T}(string, string?)" />
    protected ValueValidatorBase(string displayName, string? target = null) : base(displayName, target)
    {
    }

    /// <summary>
    ///     <para xml:lang="en">Validates the value.</para>
    ///     <para xml:lang="ja">値を検証する。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The value to validate.</para>
    ///     <para xml:lang="ja">検証対象の値。</para>
    /// </param>
    public Result Check(T? value) => Check(value, DisplayName);

    /// <summary>
    ///     <para xml:lang="en">Validates the value, overriding the display name used in error messages (e.g. with a localized name).</para>
    ///     <para xml:lang="ja">エラーメッセージに使用する表示名を上書き（多言語化した項目名等）して値を検証する。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The value to validate.</para>
    ///     <para xml:lang="ja">検証対象の値。</para>
    /// </param>
    /// <param name="displayName">
    ///     <para xml:lang="en">The display name used in error messages.</para>
    ///     <para xml:lang="ja">エラーメッセージに使用する表示名。</para>
    /// </param>
    public Result Check(T? value, string displayName) => Validate(value.HasValue, value.GetValueOrDefault(), displayName);
}
