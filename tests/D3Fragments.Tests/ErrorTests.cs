namespace D3Fragments.Tests;

public class ErrorTests
{
    public static TheoryData<Func<string, string?, string?, Error>, ErrorType> Factories => new()
    {
        { Error.Unauthenticated, ErrorType.Unauthenticated },
        { Error.Validation, ErrorType.Validation },
        { Error.NotFound, ErrorType.NotFound },
        { Error.Conflict, ErrorType.Conflict },
        { Error.Forbidden, ErrorType.Forbidden },
        { Error.InvalidState, ErrorType.InvalidState },
        { Error.PreconditionFailed, ErrorType.PreconditionFailed },
    };

    [Theory]
    [MemberData(nameof(Factories))]
    public void ファクトリ_種類と指定した値を持つエラーを生成する(Func<string, string?, string?, Error> factory, ErrorType expected)
    {
        var error = factory("message", "E001", "name");

        Assert.Equal(expected, error.Type);
        Assert.Equal("message", error.Message);
        Assert.Equal("E001", error.Code);
        Assert.Equal("name", error.Target);
    }
}
