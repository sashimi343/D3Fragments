using System.ComponentModel.DataAnnotations;

using D3Fragments.Extensions;

namespace D3Fragments.Tests;

public class EnumExtensionsTests
{
    [Flags]
    public enum Status
    {
        [Display(Name = "有効")]
        Active = 1,
        Inactive = 2,
    }

    [Theory]
    [InlineData(Status.Active, "有効")]
    [InlineData(Status.Inactive, "Inactive")]
    [InlineData(Status.Active | Status.Inactive, "Active, Inactive")]
    [InlineData((Status)8, "8")]
    public void GetDisplayName_Display属性がなければ列挙子名を返す(Status value, string expected)
    {
        Assert.Equal(expected, value.GetDisplayName());
    }
}

public class EnumerableExtensionsTests
{
    [Fact]
    public void SelectWhere_成功した要素のみ変換して返す()
    {
        var result = new[] { "1", "x", "3" }.SelectWhere(x => (int.TryParse(x, out var n), n));

        Assert.Equal([1, 3], result);
    }

    [Fact]
    public void SelectWhere_引数がnullの場合は列挙前に例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => new[] { 1 }.SelectWhere<int, int>(null!));
    }
}

public class StringExtensionsTests
{
    [Theory]
    [InlineData("UserName", "user_name")]
    [InlineData("HTMLParser", "html_parser")]
    [InlineData("userID", "user_id")]
    [InlineData("Address1", "address1")]
    public void ToSnakeCase_連続する大文字を1単語として変換する(string value, string expected)
    {
        Assert.Equal(expected, value.ToSnakeCase());
    }
}
