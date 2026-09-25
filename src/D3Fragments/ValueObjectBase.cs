using System.Diagnostics.CodeAnalysis;

namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Base class for a value object that wraps a primitive value with validation.</para>
///     <para xml:lang="ja">プリミティブ型をラップし、バリデーション付きで値オブジェクトを定義する基底クラス。</para>
/// </summary>
/// <typeparam name="TSelf">
///     <para xml:lang="en">The derived type itself.</para>
///     <para xml:lang="ja">派生クラス自身の型。</para>
/// </typeparam>
/// <typeparam name="TPrimitive">
///     <para xml:lang="en">The wrapped primitive type.</para>
///     <para xml:lang="ja">ラップするプリミティブ型。</para>
/// </typeparam>
public abstract class ValueObjectBase<TSelf, TPrimitive> : IEquatable<TSelf>
    where TSelf : ValueObjectBase<TSelf, TPrimitive>, IValueObject<TSelf, TPrimitive>
{
    /// <summary>
    ///     <para xml:lang="en">Gets the wrapped primitive value.</para>
    ///     <para xml:lang="ja">ラップされたプリミティブ値を取得する。</para>
    /// </summary>
    [NotNull]
    public TPrimitive Value { get; private init; }

    /// <summary>
    ///     <para xml:lang="en">Initializes the value object after validating the primitive value. Throws <see cref="DomainValidationException"/> when validation fails.</para>
    ///     <para xml:lang="ja">バリデーションを実行してプリミティブ値を設定する値オブジェクトを初期化する。バリデーション失敗時は<see cref="DomainValidationException"/>をスローする。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The primitive value to set.</para>
    ///     <para xml:lang="ja">設定するプリミティブ値。</para>
    /// </param>
    protected ValueObjectBase([DisallowNull] TPrimitive value)
    {
        if (!TSelf.ValidateValue(value, out var errorMessage))
        {
            throw new DomainValidationException(errorMessage);
        }
        Value = value;
    }

    /// <summary>
    ///     <para xml:lang="en">Attempts to create the value object, validating the primitive value. Returns a failure result when validation fails.</para>
    ///     <para xml:lang="ja">バリデーションを実行して値オブジェクトの生成を試みる。バリデーション失敗時は失敗結果を返す。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The primitive value to set.</para>
    ///     <para xml:lang="ja">設定するプリミティブ値。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A success result holding the value object, or a failure result holding the validation error.</para>
    ///     <para xml:lang="ja">成功時は値オブジェクトを保持する結果、失敗時はバリデーションエラーを保持する結果。</para>
    /// </returns>
    public static Result<TSelf> TryCreate(TPrimitive value) =>
        TSelf.ValidateValue(value, out var errorMessage)
            ? Result<TSelf>.Success(TSelf.Create(value))
            : Result<TSelf>.Failure(Error.Validation(errorMessage));

    /// <summary>
    ///     <para xml:lang="en">Determines whether this instance and <paramref name="other"/> wrap an equal value.</para>
    ///     <para xml:lang="ja">このインスタンスと<paramref name="other"/>が等しい値をラップしているかどうかを判定する。</para>
    /// </summary>
    public bool Equals(TSelf? other) =>
        other is not null && EqualityComparer<TPrimitive>.Default.Equals(Value, other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TSelf);

    /// <inheritdoc />
    public override int GetHashCode() => EqualityComparer<TPrimitive>.Default.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString() ?? string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Determines whether two value objects wrap an equal value.</para>
    ///     <para xml:lang="ja">2つの値オブジェクトが等しい値をラップしているかどうかを判定する。</para>
    /// </summary>
    public static bool operator ==(ValueObjectBase<TSelf, TPrimitive>? left, ValueObjectBase<TSelf, TPrimitive>? right) =>
        left is null ? right is null : left.Equals(right as TSelf);

    /// <summary>
    ///     <para xml:lang="en">Determines whether two value objects wrap an unequal value.</para>
    ///     <para xml:lang="ja">2つの値オブジェクトが等しくない値をラップしているかどうかを判定する。</para>
    /// </summary>
    public static bool operator !=(ValueObjectBase<TSelf, TPrimitive>? left, ValueObjectBase<TSelf, TPrimitive>? right) => !(left == right);
}
