using Microsoft.AspNetCore.Identity;

namespace D3Fragments.Identity.EFCore;

/// <summary>
///     <para xml:lang="en">Extension methods that convert an <see cref="IdentityResult"/> to a D3Fragments <see cref="Result"/>.</para>
///     <para xml:lang="ja"><see cref="IdentityResult"/>をD3Fragmentsの<see cref="Result"/>に変換する拡張メソッド。</para>
/// </summary>
public static class IdentityResultExtensions
{
    // 重複・同時実行の競合
    private static readonly HashSet<string> ConflictCodes =
    [
        nameof(IdentityErrorDescriber.DuplicateUserName),
        nameof(IdentityErrorDescriber.DuplicateEmail),
        nameof(IdentityErrorDescriber.DuplicateRoleName),
        nameof(IdentityErrorDescriber.LoginAlreadyAssociated),
        nameof(IdentityErrorDescriber.ConcurrencyFailure),
    ];

    // ユーザーの状態が操作を許容しない
    private static readonly HashSet<string> InvalidStateCodes =
    [
        nameof(IdentityErrorDescriber.UserAlreadyHasPassword),
        nameof(IdentityErrorDescriber.UserAlreadyInRole),
        nameof(IdentityErrorDescriber.UserNotInRole),
        nameof(IdentityErrorDescriber.UserLockoutNotEnabled),
    ];

    /// <summary>
    ///     <para xml:lang="en">Converts the result to a <see cref="Result"/>. Each <see cref="IdentityError"/> becomes an <see cref="Error"/> whose message is <see cref="IdentityError.Description"/> and whose code is <see cref="IdentityError.Code"/>.</para>
    ///     <para xml:lang="ja">結果を<see cref="Result"/>に変換する。各<see cref="IdentityError"/>は、<see cref="IdentityError.Description"/>をメッセージ、<see cref="IdentityError.Code"/>をコードとする<see cref="Error"/>になる。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">The error type is <see cref="ErrorType.Conflict"/> for duplicates and concurrency failures (<c>DuplicateUserName</c>, <c>DuplicateEmail</c>, <c>DuplicateRoleName</c>, <c>LoginAlreadyAssociated</c>, <c>ConcurrencyFailure</c>), <see cref="ErrorType.InvalidState"/> for operations that the state of the user does not allow (<c>UserAlreadyHasPassword</c>, <c>UserAlreadyInRole</c>, <c>UserNotInRole</c>, <c>UserLockoutNotEnabled</c>), and <see cref="ErrorType.Validation"/> otherwise. To map the errors differently, use <see cref="ToResult(IdentityResult, Func{IdentityError, Error})"/>.</para>
    ///     <para xml:lang="ja">エラーの種類は、重複・同時実行の競合（<c>DuplicateUserName</c>、<c>DuplicateEmail</c>、<c>DuplicateRoleName</c>、<c>LoginAlreadyAssociated</c>、<c>ConcurrencyFailure</c>）は<see cref="ErrorType.Conflict"/>、ユーザーの状態が許容しない操作（<c>UserAlreadyHasPassword</c>、<c>UserAlreadyInRole</c>、<c>UserNotInRole</c>、<c>UserLockoutNotEnabled</c>）は<see cref="ErrorType.InvalidState"/>、それ以外は<see cref="ErrorType.Validation"/>とする。別の対応づけにする場合は<see cref="ToResult(IdentityResult, Func{IdentityError, Error})"/>を使う。</para>
    /// </remarks>
    /// <param name="result">
    ///     <para xml:lang="en">The result to convert.</para>
    ///     <para xml:lang="ja">変換する結果。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A successful <see cref="Result"/> when <paramref name="result"/> succeeded; otherwise a failed <see cref="Result"/> that contains the converted errors.</para>
    ///     <para xml:lang="ja"><paramref name="result"/>が成功していれば成功の<see cref="Result"/>。それ以外は変換したエラーを含む失敗の<see cref="Result"/>。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="result"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="result"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static Result ToResult(this IdentityResult result) => result.ToResult(ToError);

    /// <summary>
    ///     <para xml:lang="en">Converts the result to a <see cref="Result"/>, converting each <see cref="IdentityError"/> with <paramref name="map"/>.</para>
    ///     <para xml:lang="ja">各<see cref="IdentityError"/>を<paramref name="map"/>で変換して、結果を<see cref="Result"/>に変換する。</para>
    /// </summary>
    /// <param name="result">
    ///     <para xml:lang="en">The result to convert.</para>
    ///     <para xml:lang="ja">変換する結果。</para>
    /// </param>
    /// <param name="map">
    ///     <para xml:lang="en">The function that converts an <see cref="IdentityError"/> to an <see cref="Error"/>.</para>
    ///     <para xml:lang="ja"><see cref="IdentityError"/>を<see cref="Error"/>に変換する関数。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A successful <see cref="Result"/> when <paramref name="result"/> succeeded; otherwise a failed <see cref="Result"/> that contains the converted errors.</para>
    ///     <para xml:lang="ja"><paramref name="result"/>が成功していれば成功の<see cref="Result"/>。それ以外は変換したエラーを含む失敗の<see cref="Result"/>。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="result"/> or <paramref name="map"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="result"/>または<paramref name="map"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static Result ToResult(this IdentityResult result, Func<IdentityError, Error> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return result.Succeeded ? Result.Success() : Result.Failure(result.Errors.Select(map));
    }

    private static Error ToError(IdentityError error)
    {
        if (ConflictCodes.Contains(error.Code))
        {
            return Error.Conflict(error.Description, error.Code);
        }

        return InvalidStateCodes.Contains(error.Code)
            ? Error.InvalidState(error.Description, error.Code)
            : Error.Validation(error.Description, error.Code);
    }
}
