using Microsoft.AspNetCore.Http;

namespace D3Fragments.AspNetCore;

/// <summary>
///     <para xml:lang="en">Provides extension methods that convert a <see cref="Result"/> into an <see cref="IResult"/>.</para>
///     <para xml:lang="ja"><see cref="Result"/>を<see cref="IResult"/>へ変換する拡張メソッドを提供する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">A failed result is converted into a Problem Details response in the same format as an unhandled <see cref="DomainException"/>. The HTTP status code and the error code member follow <see cref="DomainProblemDetailsOptions"/>. The returned <see cref="IResult"/> can also be returned from an MVC controller action.</para>
///     <para xml:lang="ja">失敗した結果は、未処理の<see cref="DomainException"/>と同じ形式のProblem Details応答に変換する。HTTPステータスコードとエラーコードのメンバーは<see cref="DomainProblemDetailsOptions"/>に従う。戻り値の<see cref="IResult"/>はMVCのコントローラーアクションからも返却できる。</para>
/// </remarks>
public static class ResultHttpExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Converts the result into 204 (No Content) on success, or into a Problem Details response on failure.</para>
    ///     <para xml:lang="ja">成功時は204（No Content）、失敗時はProblem Details応答に変換する。</para>
    /// </summary>
    /// <param name="result">
    ///     <para xml:lang="en">The result to convert.</para>
    ///     <para xml:lang="ja">変換する結果。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The <see cref="IResult"/> that writes the response.</para>
    ///     <para xml:lang="ja">応答を書き出す<see cref="IResult"/>。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="result"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="result"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? TypedResults.NoContent() : ToProblem(result);
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the result into 200 (OK) with the value on success, or into a Problem Details response on failure.</para>
    ///     <para xml:lang="ja">成功時は値を持つ200（OK）、失敗時はProblem Details応答に変換する。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the value.</para>
    ///     <para xml:lang="ja">値の型。</para>
    /// </typeparam>
    /// <param name="result">
    ///     <para xml:lang="en">The result to convert.</para>
    ///     <para xml:lang="ja">変換する結果。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The <see cref="IResult"/> that writes the response.</para>
    ///     <para xml:lang="ja">応答を書き出す<see cref="IResult"/>。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="result"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="result"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : ToProblem(result);
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the result into the <see cref="IResult"/> created by <paramref name="onSuccess"/> on success, or into a Problem Details response on failure.</para>
    ///     <para xml:lang="ja">成功時は<paramref name="onSuccess"/>が生成した<see cref="IResult"/>、失敗時はProblem Details応答に変換する。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the value.</para>
    ///     <para xml:lang="ja">値の型。</para>
    /// </typeparam>
    /// <param name="result">
    ///     <para xml:lang="en">The result to convert.</para>
    ///     <para xml:lang="ja">変換する結果。</para>
    /// </param>
    /// <param name="onSuccess">
    ///     <para xml:lang="en">The function that creates the response from the value on success (e.g. <c>v =&gt; TypedResults.Created(uri, v)</c>). It is not called on failure.</para>
    ///     <para xml:lang="ja">成功時に値から応答を生成する関数（例：<c>v =&gt; TypedResults.Created(uri, v)</c>）。失敗時は呼び出さない。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The <see cref="IResult"/> that writes the response.</para>
    ///     <para xml:lang="ja">応答を書き出す<see cref="IResult"/>。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="result"/> or <paramref name="onSuccess"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="result"/>または<paramref name="onSuccess"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        return result.IsSuccess ? onSuccess(result.Value) : ToProblem(result);
    }

    private static DomainProblemResult ToProblem(Result result) => new(result.Errors, result.Errors[0].Code);
}
