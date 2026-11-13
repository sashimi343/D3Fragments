namespace D3Fragments.Tests;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_内部例外を指定しても種類とメッセージを保持する()
    {
        var inner = new InvalidOperationException();

        var exception = new DomainException(ErrorType.InvalidState, "invalid", inner);

        Assert.Equal(ErrorType.InvalidState, exception.ErrorType);
        Assert.Equal("invalid", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void EntityNotFoundException_NotFoundとして既定のメッセージを持つ()
    {
        var exception = new EntityNotFoundException();

        Assert.Equal(ErrorType.NotFound, exception.ErrorType);
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void EntityNotFoundException_メッセージにエンティティ名と補足を含める()
    {
        var withName = new EntityNotFoundException("Book");
        var withMessage = new EntityNotFoundException("Book", "id=1");

        Assert.Equal(ErrorType.NotFound, withName.ErrorType);
        Assert.Contains("Book", withName.Message);
        Assert.Equal(ErrorType.NotFound, withMessage.ErrorType);
        Assert.Contains("Book", withMessage.Message);
        Assert.Contains("id=1", withMessage.Message);
    }

    [Fact]
    public void ConflictOccurredException_書式に引数を埋め込んだメッセージを持つ()
    {
        var inner = new InvalidOperationException();

        var exception = new ConflictOccurredException("{0} is duplicated", "email");
        var withInner = new ConflictOccurredException(inner, "{0} is duplicated", "email");

        Assert.Equal(ErrorType.Conflict, exception.ErrorType);
        Assert.Equal("email is duplicated", exception.Message);
        Assert.Equal(ErrorType.Conflict, withInner.ErrorType);
        Assert.Equal("email is duplicated", withInner.Message);
        Assert.Same(inner, withInner.InnerException);
    }
}
