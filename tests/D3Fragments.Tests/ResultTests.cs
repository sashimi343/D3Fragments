using D3Fragments.Validations;

namespace D3Fragments.Tests;

public class ResultTests
{
    [Fact]
    public void GetValueOrThrow_失敗結果の場合は例外をスローする()
    {
        var result = Result<int>.Failure(Error.Validation("invalid"));

        Assert.Throws<DomainException>(() => result.GetValueOrThrow());
    }

    [Fact]
    public void Merge_両方成功の場合は成功を返す()
    {
        var merged = Result.Success().Merge(Result.Success());

        Assert.True(merged.IsSuccess);
    }

    [Fact]
    public void Merge_一方が失敗の場合はエラーをマージした失敗を返す()
    {
        var merged = Result.Success().Merge(Result.Failure(Error.Validation("ng")));

        Assert.True(merged.IsFailure);
        Assert.Single(merged.Errors);
    }

    private static readonly Error First = Error.Validation("first");
    private static readonly Error Second = Error.Conflict("second");

    private sealed class CustomException(IReadOnlyList<Error> errors) : Exception
    {
        public IReadOnlyList<Error> Errors { get; } = errors;
    }

    [Fact]
    public void Merge_両方失敗の場合は自身のエラーに続けて相手のエラーを並べる()
    {
        var merged = Result.Failure(First).Merge(Result.Failure(Second));

        Assert.Equal([First, Second], merged.Errors);
    }

    [Fact]
    public void Merge_自身のみ失敗の場合は自身のエラーを返す()
    {
        Assert.Equal([First], Result.Failure(First).Merge(Result.Success()).Errors);
    }

    [Fact]
    public void CombineErrors_失敗した結果のエラーだけを順に集める()
    {
        IEnumerable<Result> results = [Result.Failure(First), Result.Success(), Result.Failure(Second)];

        Assert.Equal([First, Second], Result.CombineErrors(results));
    }

    [Fact]
    public void GetFirstErrorMessage_成功の場合は空文字で失敗の場合は先頭のメッセージを返す()
    {
        Assert.Equal(string.Empty, Result.Success().GetFirstErrorMessage());
        Assert.Equal("first", Result.Failure(First, Second).GetFirstErrorMessage());
    }

    [Fact]
    public void ToResult_成功の場合は値を生成した成功結果を返す()
    {
        var result = Result.Success().ToResult(() => 42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ToResult_失敗の場合は値を生成せずにエラーを引き継ぐ()
    {
        var result = Result.Failure(First).ToResult<int>(() => throw new InvalidOperationException());

        Assert.Equal([First], result.Errors);
    }

    [Fact]
    public void ThrowIfFailure_成功の場合は自身を返す()
    {
        var result = Result.Success();

        Assert.Same(result, result.ThrowIfFailure());
        Assert.Same(result, result.ThrowIfFailure("E001"));
        Assert.Same(result, result.ThrowIfFailure(errors => new CustomException(errors)));
    }

    [Fact]
    public void ThrowIfFailure_失敗の場合はエラー一覧を持つ例外をスローする()
    {
        var exception = Assert.Throws<DomainException>(() => Result.Failure(First, Second).ThrowIfFailure());

        Assert.Equal([First, Second], exception.Errors);
    }

    [Fact]
    public void ThrowIfFailure_例外ファクトリにエラー一覧を渡してスローする()
    {
        var exception = Assert.Throws<CustomException>(() => Result.Failure(First).ThrowIfFailure(errors => new CustomException(errors)));

        Assert.Equal([First], exception.Errors);
    }
}

public class ResultOfTTests
{
    private static readonly Error First = Error.Validation("first");
    private static readonly Error Second = Error.Conflict("second");

    private sealed class CustomException(IReadOnlyList<Error> errors) : Exception
    {
        public IReadOnlyList<Error> Errors { get; } = errors;
    }

    [Fact]
    public void 暗黙変換_値から成功結果を生成する()
    {
        Result<string> result = "value";

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void Failure_値は既定値になる()
    {
        Assert.Null(Result<string>.Failure(First).Value);
        Assert.Null(Result<string>.Failure(new List<Error> { First }).Value);
    }

    [Fact]
    public void FailIfNull_値がある場合は成功を返す()
    {
        var result = Result<string>.FailIfNull("value", First);

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void FailIfNull_nullの場合は指定したエラーを順に持つ失敗を返す()
    {
        var result = Result<string>.FailIfNull(null, First, Second);

        Assert.Equal([First, Second], result.Errors);
    }

    [Fact]
    public void ToResult_成功の場合は変換関数の結果を返す()
    {
        var result = Result<int>.Success(2).ToResult(x => Result<string>.Failure(Error.Validation($"{x}")));

        Assert.Equal("2", result.GetFirstErrorMessage());
    }

    [Fact]
    public void ToResult_失敗の場合は変換せずにエラーを引き継ぐ()
    {
        var result = Result<int>.Failure(First).ToResult<string>(_ => throw new InvalidOperationException());

        Assert.Equal([First], result.Errors);
    }

    [Fact]
    public void GetValueOrThrow_成功の場合は値を返す()
    {
        var result = Result<int>.Success(42);

        Assert.Equal(42, result.GetValueOrThrow());
        Assert.Equal(42, result.GetValueOrThrow("E001"));
        Assert.Equal(42, result.GetValueOrThrow(errors => new CustomException(errors)));
    }

    [Fact]
    public void GetValueOrThrow_失敗の場合はエラー一覧を持つ例外をスローする()
    {
        var exception = Assert.Throws<DomainException>(() => Result<int>.Failure(First, Second).GetValueOrThrow());

        Assert.Equal([First, Second], exception.Errors);
    }

    [Fact]
    public void GetValueOrThrow_エラーコードを指定した場合は例外に設定する()
    {
        var exception = Assert.Throws<DomainException>(() => Result<int>.Failure(First).GetValueOrThrow("E999"));

        Assert.Equal("E999", exception.ErrorCode);
        Assert.Equal([First], exception.Errors);
    }

    [Fact]
    public void GetValueOrThrow_例外ファクトリにエラー一覧を渡してスローする()
    {
        var exception = Assert.Throws<CustomException>(() => Result<int>.Failure(First).GetValueOrThrow(errors => new CustomException(errors)));

        Assert.Equal([First], exception.Errors);
    }

    [Fact]
    public void ThrowIfFailure_成功の場合は自身を返す()
    {
        var result = Result<int>.Success(42);

        Assert.Same(result, result.ThrowIfFailure());
        Assert.Same(result, result.ThrowIfFailure("E001"));
        Assert.Same(result, result.ThrowIfFailure(errors => new CustomException(errors)));
    }

    [Fact]
    public void ThrowIfFailure_失敗の場合は各オーバーロードの方法でスローする()
    {
        var result = Result<int>.Failure(First, Second);

        Assert.Equal([First, Second], Assert.Throws<DomainException>(() => result.ThrowIfFailure()).Errors);
        Assert.Equal("E999", Assert.Throws<DomainException>(() => result.ThrowIfFailure("E999")).ErrorCode);
        Assert.Equal([First, Second], Assert.Throws<CustomException>(() => result.ThrowIfFailure(errors => new CustomException(errors))).Errors);
    }
}

public class ResultCombineTests
{
    [Fact]
    public void Combine_すべてのエラーをまとめる()
    {
        var result = Result.Combine(
            new StringValidator("Name").Required().Check(null),
            new NumberValidator<int>("Age").Min(0).Check(-1),
            Result.Success());

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void ThrowIfFailure_ErrorのCodeとTargetを例外に引き継ぐ()
    {
        var result = Result.Failure(Error.Validation("invalid", code: "E001", target: "name"));

        var exception = Assert.Throws<DomainException>(() => result.ThrowIfFailure());

        Assert.Equal("E001", exception.ErrorCode);
        Assert.Equal("name", exception.Target);
        Assert.Equal("name", exception.GetDomainError().Target);
    }

    [Fact]
    public void ThrowIfFailure_全エラーを例外に引き継ぎ代表値は先頭エラーとする()
    {
        var result = Result.Failure(
            Error.Validation("first", target: "name"),
            Error.Conflict("second", code: "E002", target: "email"));

        var exception = Assert.Throws<DomainException>(() => result.ThrowIfFailure("E999"));

        Assert.Equal(["name", "email"], exception.Errors.Select(x => x.Target));
        Assert.Equal("E002", exception.Errors[1].Code);
        Assert.Equal("first", exception.Message);
        Assert.Equal(ErrorType.Validation, exception.ErrorType);
        Assert.Equal("E999", exception.ErrorCode);
    }

    [Fact]
    public void Errors_エラー一覧なしで生成した例外は後から設定した値を反映する()
    {
        var exception = new ConflictOccurredException("duplicated") { Target = "email" };

        Assert.Equal("email", Assert.Single(exception.Errors).Target);
    }

    [Fact]
    public void Combine_すべて成功なら成功を返す()
    {
        Assert.True(Result.Combine(Result.Success(), Result.Success()).IsSuccess);
    }
}
