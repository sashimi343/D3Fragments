using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace D3Fragments.AspNetCore.Tests;

public class ModelStateDictionaryExtensionsTests
{
    [Fact]
    public void AddDomainErrors_Targetをキーとし対象のないエラーは空文字キーに追加する()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddDomainErrors([Error.Validation("name", target: "Input.Name"), Error.Conflict("whole")]);

        Assert.Equal("name", Assert.Single(modelState["Input.Name"]!.Errors).ErrorMessage);
        Assert.Equal("whole", Assert.Single(modelState[string.Empty]!.Errors).ErrorMessage);
    }

    [Fact]
    public void AddDomainErrors_nullは例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => ((ModelStateDictionary)null!).AddDomainErrors([]));
        Assert.Throws<ArgumentNullException>(() => new ModelStateDictionary().AddDomainErrors(null!));
    }

    [Fact]
    public void GetStatusCode_対応表にないErrorTypeは400を返す()
    {
        var options = new DomainProblemDetailsOptions();
        options.StatusCodeMap.Remove(ErrorType.Conflict);

        Assert.Equal(StatusCodes.Status400BadRequest, options.GetStatusCode(ErrorType.Conflict));
        Assert.Equal(StatusCodes.Status404NotFound, options.GetStatusCode(ErrorType.NotFound));
    }
}
