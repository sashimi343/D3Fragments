using System.Diagnostics.CodeAnalysis;

namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Implemented by a value object. Defines how the wrapped primitive value is obtained, created, and validated.</para>
///     <para xml:lang="ja">値オブジェクトが実装すべきインターフェース。ラップするプリミティブ値の取得・生成・バリデーションを定義する。</para>
/// </summary>
/// <typeparam name="TSelf">
///     <para xml:lang="en">The implementing type itself.</para>
///     <para xml:lang="ja">実装クラス自身の型。</para>
/// </typeparam>
/// <typeparam name="TPrimitive">
///     <para xml:lang="en">The wrapped primitive type.</para>
///     <para xml:lang="ja">ラップするプリミティブ型。</para>
/// </typeparam>
public interface IValueObject<TSelf, TPrimitive> where TSelf : IValueObject<TSelf, TPrimitive>
{
    /// <summary>
    ///     <para xml:lang="en">Gets the wrapped primitive value.</para>
    ///     <para xml:lang="ja">ラップされたプリミティブ値を取得する。</para>
    /// </summary>
    TPrimitive Value { get; }

    /// <summary>
    ///     <para xml:lang="en">Creates an instance of the value object from a primitive value.</para>
    ///     <para xml:lang="ja">プリミティブ値から値オブジェクトのインスタンスを生成する。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The primitive value to set.</para>
    ///     <para xml:lang="ja">設定するプリミティブ値。</para>
    /// </param>
    static abstract TSelf Create(TPrimitive value);

    /// <summary>
    ///     <para xml:lang="en">Validates a primitive value.</para>
    ///     <para xml:lang="ja">プリミティブ値のバリデーションを実行する。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The primitive value to validate.</para>
    ///     <para xml:lang="ja">検証するプリミティブ値。</para>
    /// </param>
    /// <param name="errorMessage">
    ///     <para xml:lang="en">The error message when validation fails.</para>
    ///     <para xml:lang="ja">バリデーション失敗時のエラーメッセージ。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><c>true</c> if validation succeeded; otherwise, <c>false</c>.</para>
    ///     <para xml:lang="ja">バリデーションが成功した場合は<c>true</c>、失敗した場合は<c>false</c>。</para>
    /// </returns>
    static abstract bool ValidateValue(TPrimitive value, [NotNullWhen(false)] out string? errorMessage);
}
