using Microsoft.AspNetCore.Identity;

namespace D3Fragments.Identity.EFCore.Tests;

public class IdentityResultExtensionsTests
{
    private static IdentityError ErrorOf(string code) => new() { Code = code, Description = $"{code} description" };

    [Fact]
    public void 成功した結果は成功のResultになる()
    {
        Assert.True(IdentityResult.Success.ToResult().IsSuccess);
    }

    [Theory]
    [InlineData("DuplicateUserName", ErrorType.Conflict)]
    [InlineData("DuplicateEmail", ErrorType.Conflict)]
    [InlineData("DuplicateRoleName", ErrorType.Conflict)]
    [InlineData("LoginAlreadyAssociated", ErrorType.Conflict)]
    [InlineData("ConcurrencyFailure", ErrorType.Conflict)]
    [InlineData("UserAlreadyHasPassword", ErrorType.InvalidState)]
    [InlineData("UserAlreadyInRole", ErrorType.InvalidState)]
    [InlineData("UserNotInRole", ErrorType.InvalidState)]
    [InlineData("UserLockoutNotEnabled", ErrorType.InvalidState)]
    [InlineData("PasswordTooShort", ErrorType.Validation)]
    [InlineData("InvalidToken", ErrorType.Validation)]
    public void エラーのコードに応じた種類のErrorになる(string code, ErrorType expected)
    {
        var result = IdentityResult.Failed(ErrorOf(code)).ToResult();

        var error = Assert.Single(result.Errors);
        Assert.Equal(expected, error.Type);
        Assert.Equal(code, error.Code);
        Assert.Equal($"{code} description", error.Message);
    }

    [Fact]
    public void 複数のエラーをすべて変換する()
    {
        var result = IdentityResult.Failed(ErrorOf("PasswordTooShort"), ErrorOf("PasswordRequiresDigit")).ToResult();

        Assert.Equal(["PasswordTooShort", "PasswordRequiresDigit"], result.Errors.Select(x => x.Code));
    }

    [Fact]
    public void 変換関数を指定するとその関数で変換する()
    {
        var result = IdentityResult.Failed(ErrorOf("PasswordTooShort")).ToResult(x => Error.PreconditionFailed("custom", x.Code));

        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.PreconditionFailed, error.Type);
        Assert.Equal("custom", error.Message);
    }

    [Fact]
    public void 引数がnullなら例外を送出する()
    {
        Assert.Throws<ArgumentNullException>(() => ((IdentityResult)null!).ToResult());
        Assert.Throws<ArgumentNullException>(() => IdentityResult.Success.ToResult(null!));
    }
}
