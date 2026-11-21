using System.ComponentModel;

namespace D3Fragments.Tests;

public class UuidV7TypeConverterTests
{
    private static readonly TypeConverter Converter = TypeDescriptor.GetConverter(typeof(UuidV7));

    [Fact]
    public void UuidV7_属性指定だけで文字列と相互変換できる()
    {
        var id = UuidV7.NewUuidV7();

        Assert.True(Converter.CanConvertFrom(typeof(string)));
        Assert.Equal(id, Converter.ConvertFromInvariantString(id.ToString()));
        Assert.Equal(id.ToString(), Converter.ConvertToInvariantString(id));
    }

    [Theory]
    [InlineData("not-a-uuid")]
    [InlineData("")]
    [InlineData("00000000-0000-4000-8000-000000000000")] // version 4
    public void UuidV7_不正な文字列はFormatExceptionになる(string text)
    {
        Assert.Throws<FormatException>(() => Converter.ConvertFromInvariantString(text));
    }

    [Fact]
    public void UuidV7_文字列以外からは変換できない()
    {
        Assert.False(Converter.CanConvertFrom(typeof(Guid)));
        Assert.Throws<NotSupportedException>(() => Converter.ConvertFrom(UuidV7.NewUuidV7().Value));
    }
}
