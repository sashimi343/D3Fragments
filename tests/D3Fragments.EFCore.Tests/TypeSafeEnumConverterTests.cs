namespace D3Fragments.EFCore.Tests;

public sealed class TestColor : TypeSafeEnum<TestColor, int>
{
    public static readonly TestColor Red = new(1);

    private TestColor(int key) : base(key)
    {
    }
}

public class TypeSafeEnumConverterTests
{
    [Fact]
    public void キーと相互変換する()
    {
        var converter = new TypeSafeEnumConverter<TestColor, int>();

        Assert.Equal(1, converter.ConvertToProvider(TestColor.Red));
        Assert.Same(TestColor.Red, converter.ConvertFromProvider(1));
    }
}
