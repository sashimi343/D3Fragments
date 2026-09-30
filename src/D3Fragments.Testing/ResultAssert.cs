using Xunit;

namespace D3Fragments.Testing;

/// <summary>
///     <para xml:lang="en">Provides assertions for <see cref="Result"/> and <see cref="Result{T}"/>.</para>
///     <para xml:lang="ja"><see cref="Result"/>型、<see cref="Result{T}"/>型に対するアサーションを提供する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">On failure, the assertion message includes the errors held by the result.</para>
///     <para xml:lang="ja">検証に失敗した場合、アサーションメッセージにはResultが保持するエラーが含まれる。</para>
/// </remarks>
public static class ResultAssert
{
    /// <summary>
    ///     <para xml:lang="en">Verifies that the result is a success.</para>
    ///     <para xml:lang="ja">Resultが成功であることを検証する。</para>
    /// </summary>
    /// <param name="result">
    ///     <para xml:lang="en">The result to verify.</para>
    ///     <para xml:lang="ja">検証対象のResult。</para>
    /// </param>
    public static void IsSuccess(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies that the result is a success and returns its value.</para>
    ///     <para xml:lang="ja">Resultが成功であることを検証し、その値を返す。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the value held by the result.</para>
    ///     <para xml:lang="ja">Resultが保持する値の型。</para>
    /// </typeparam>
    /// <param name="result">
    ///     <para xml:lang="en">The result to verify.</para>
    ///     <para xml:lang="ja">検証対象のResult。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The value held by the result.</para>
    ///     <para xml:lang="ja">Resultが保持する値。</para>
    /// </returns>
    public static T IsSuccess<T>(Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Assert.Empty(result.Errors);
        return result.Value!;
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies that the result is a failure and returns its errors.</para>
    ///     <para xml:lang="ja">Resultが失敗であることを検証し、そのエラーを返す。</para>
    /// </summary>
    /// <param name="result">
    ///     <para xml:lang="en">The result to verify.</para>
    ///     <para xml:lang="ja">検証対象のResult。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The errors held by the result.</para>
    ///     <para xml:lang="ja">Resultが保持するエラー。</para>
    /// </returns>
    public static IReadOnlyList<Error> IsFailure(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Assert.NotEmpty(result.Errors);
        return result.Errors;
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies that the result contains an error of the specified type.</para>
    ///     <para xml:lang="ja">指定した種類のエラーがResultに含まれることを検証する。</para>
    /// </summary>
    /// <param name="result">
    ///     <para xml:lang="en">The result to verify.</para>
    ///     <para xml:lang="ja">検証対象のResult。</para>
    /// </param>
    /// <param name="type">
    ///     <para xml:lang="en">The expected error type.</para>
    ///     <para xml:lang="ja">期待するエラーの種類。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The first error of the specified type.</para>
    ///     <para xml:lang="ja">指定した種類の最初のエラー。</para>
    /// </returns>
    public static Error HasError(Result result, ErrorType type)
    {
        ArgumentNullException.ThrowIfNull(result);
        Assert.Contains(result.Errors, e => e.Type == type);
        return result.Errors.First(e => e.Type == type);
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies that the result contains an error whose message contains the specified text.</para>
    ///     <para xml:lang="ja">指定した文字列をメッセージに含むエラーがResultに含まれることを検証する。</para>
    /// </summary>
    /// <param name="result">
    ///     <para xml:lang="en">The result to verify.</para>
    ///     <para xml:lang="ja">検証対象のResult。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">The text expected in the error message. The comparison is ordinal and matches a part of the message.</para>
    ///     <para xml:lang="ja">エラーメッセージに含まれることを期待する文字列。序数比較による部分一致で判定する。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The first matching error.</para>
    ///     <para xml:lang="ja">条件に一致する最初のエラー。</para>
    /// </returns>
    public static Error ContainsError(Result result, string message)
    {
        ArgumentNullException.ThrowIfNull(result);
        Assert.Contains(result.Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
        return result.Errors.First(e => e.Message.Contains(message, StringComparison.Ordinal));
    }
}
