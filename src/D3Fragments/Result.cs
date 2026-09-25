using System.Diagnostics.CodeAnalysis;

namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents the outcome of an operation, holding either success or a list of errors on failure.</para>
///     <para xml:lang="ja">処理結果を表すクラス。成功または失敗（エラー一覧付き）を保持する。</para>
/// </summary>
public class Result
{
    private protected Result(IReadOnlyList<Error> errors)
    {
        Errors = errors;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the errors that caused the failure. Empty when the operation succeeded.</para>
    ///     <para xml:lang="ja">処理が失敗した場合のエラー一覧を取得する。成功時は空のリスト。</para>
    /// </summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether the operation succeeded.</para>
    ///     <para xml:lang="ja">処理が成功したかどうかを取得する。</para>
    /// </summary>
    public virtual bool IsSuccess => Errors.Count == 0;

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether the operation failed.</para>
    ///     <para xml:lang="ja">処理が失敗したかどうかを取得する。</para>
    /// </summary>
    public virtual bool IsFailure => !IsSuccess;

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Result"/> representing success.</para>
    ///     <para xml:lang="ja">成功を表す <see cref="Result"/> を生成する。</para>
    /// </summary>
    public static Result Success() => new(Array.Empty<Error>());

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Result"/> representing failure with the specified errors.</para>
    ///     <para xml:lang="ja">指定したエラー一覧で失敗を表す <see cref="Result"/> を生成する。</para>
    /// </summary>
    public static Result Failure(IEnumerable<Error> errors) => new(errors.ToArray());

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Result"/> representing failure with the specified errors.</para>
    ///     <para xml:lang="ja">指定したエラーで失敗を表す <see cref="Result"/> を生成する。</para>
    /// </summary>
    public static Result Failure(params Error[] errors) => new(errors);

    /// <summary>
    ///     <para xml:lang="en">Combines the errors of the specified results into a single list. Returns an empty list if all results succeeded.</para>
    ///     <para xml:lang="ja">引数の結果に含まれるエラーをすべてマージしたエラー一覧を返す。すべて成功している場合は空のリストを返す。</para>
    /// </summary>
    public static List<Error> CombineErrors(IEnumerable<Result> results)
    {
        var combinedErrors = new List<Error>();
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                combinedErrors.AddRange(result.Errors);
            }
        }
        return combinedErrors;
    }

    /// <summary>
    ///     <para xml:lang="en">Combines the errors of the specified results into a single list. Returns an empty list if all results succeeded.</para>
    ///     <para xml:lang="ja">引数の結果に含まれるエラーをすべてマージしたエラー一覧を返す。すべて成功している場合は空のリストを返す。</para>
    /// </summary>
    public static List<Error> CombineErrors(params Result[] results) => CombineErrors(results.AsEnumerable());

    /// <summary>
    ///     <para xml:lang="en">Converts this result into a <see cref="Result{T}"/>, producing the value via <paramref name="valueOnSuccess"/> when this result succeeded.</para>
    ///     <para xml:lang="ja">成功時に値を生成するファクトリ関数を使用して <see cref="Result{T}"/> に変換する。失敗時はエラー一覧を引き継いだ <see cref="Result{T}"/> を返す。</para>
    /// </summary>
    public Result<T> ToResult<T>(Func<T> valueOnSuccess) =>
        IsSuccess ? Result<T>.Success(valueOnSuccess()) : Result<T>.Failure(Errors);

    /// <summary>
    ///     <para xml:lang="en">Gets the first error message, or an empty string when there is no error.</para>
    ///     <para xml:lang="ja">最初のエラーメッセージを取得する。エラーがない場合は空文字列を返す。</para>
    /// </summary>
    public string GetFirstErrorMessage() => Errors.Count == 0 ? string.Empty : Errors[0].Message;

    /// <summary>
    ///     <para xml:lang="en">Merges this result with another. Succeeds only when both results succeeded; otherwise combines both error lists.</para>
    ///     <para xml:lang="ja">このインスタンスと引数の結果をマージする。両方が成功のときのみ成功を返し、それ以外は両方のエラーを含む失敗を返す。</para>
    /// </summary>
    public Result Merge(Result other)
    {
        if (IsSuccess && other.IsSuccess)
        {
            return Success();
        }
        return Failure(CombineErrors(this, other));
    }
}

/// <summary>
///     <para xml:lang="en">Represents the outcome of an operation that produces a value on success.</para>
///     <para xml:lang="ja">値を持つ処理結果を表すクラス。成功時は値を、失敗時はエラー一覧を保持する。</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The type of the value produced on success.</para>
///     <para xml:lang="ja">成功時の値の型。</para>
/// </typeparam>
public sealed class Result<T> : Result
{
    private Result(T? value, IReadOnlyList<Error> errors) : base(errors)
    {
        Value = value;
    }

    /// <inheritdoc cref="Result.IsSuccess" />
    [MemberNotNullWhen(true, nameof(Value))]
    public override bool IsSuccess => base.IsSuccess;

    /// <inheritdoc cref="Result.IsFailure" />
    [MemberNotNullWhen(false, nameof(Value))]
    public override bool IsFailure => base.IsFailure;

    /// <summary>
    ///     <para xml:lang="en">Gets the value produced on success. <c>null</c> on failure.</para>
    ///     <para xml:lang="ja">成功時の値を取得する。失敗時は <c>null</c>。</para>
    /// </summary>
    public T? Value { get; }

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Result{T}"/> representing success with the specified value.</para>
    ///     <para xml:lang="ja">指定した値で成功を表す <see cref="Result{T}"/> を生成する。</para>
    /// </summary>
    public static Result<T> Success(T value) => new(value, Array.Empty<Error>());

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Result{T}"/> representing failure with the specified errors.</para>
    ///     <para xml:lang="ja">指定したエラー一覧で失敗を表す <see cref="Result{T}"/> を生成する。</para>
    /// </summary>
    public static new Result<T> Failure(IEnumerable<Error> errors) => new(default, errors.ToArray());

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Result{T}"/> representing failure with the specified errors.</para>
    ///     <para xml:lang="ja">指定したエラーで失敗を表す <see cref="Result{T}"/> を生成する。</para>
    /// </summary>
    public static new Result<T> Failure(params Error[] errors) => new(default, errors);

    /// <summary>
    ///     <para xml:lang="en">Implicitly converts a value into a successful <see cref="Result{T}"/>.</para>
    ///     <para xml:lang="ja">値を成功を表す <see cref="Result{T}"/> に暗黙的に変換する。</para>
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    ///     <para xml:lang="en">Returns a failure result with <paramref name="error"/> when <paramref name="value"/> is <c>null</c>; otherwise a success result.</para>
    ///     <para xml:lang="ja">値が <c>null</c> の場合に <paramref name="error"/> を伴う失敗結果を、そうでない場合は成功結果を返す。</para>
    /// </summary>
    public static Result<T> FailIfNull(T? value, Error error, params Error[] additionalErrors)
    {
        if (value is not null)
        {
            return Success(value);
        }
        var errors = new List<Error> { error };
        errors.AddRange(additionalErrors);
        return Failure(errors);
    }

    /// <summary>
    ///     <para xml:lang="en">Maps the value on success into a <see cref="Result{TNew}"/> via <paramref name="valueMapper"/>. Propagates the errors on failure.</para>
    ///     <para xml:lang="ja">成功時に値をマッピング関数で別の型の <see cref="Result{TNew}"/> に変換する。失敗時はエラー一覧を引き継いだ <see cref="Result{TNew}"/> を返す。</para>
    /// </summary>
    public Result<TNew> ToResult<TNew>(Func<T, Result<TNew>> valueMapper) =>
        IsSuccess ? valueMapper(Value) : Result<TNew>.Failure(Errors);

    /// <summary>
    ///     <para xml:lang="en">Gets the value, or throws a <see cref="DomainException"/> when this result is a failure.</para>
    ///     <para xml:lang="ja">値を取得する。この結果が失敗の場合は <see cref="DomainException"/> をスローする。</para>
    /// </summary>
    /// <exception cref="DomainException">
    ///     <para xml:lang="en">Thrown when this result is a failure.</para>
    ///     <para xml:lang="ja">この結果が失敗の場合にスローされる。</para>
    /// </exception>
    public T GetValueOrThrow()
    {
        if (IsSuccess)
        {
            return Value;
        }
        throw new DomainException(ErrorType.InvalidState, string.Format(ExceptionMessages.InvalidOperationException_ResultValueAccessedOnFailure, GetFirstErrorMessage()));
    }
}
