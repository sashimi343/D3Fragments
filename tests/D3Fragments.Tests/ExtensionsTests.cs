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

    [Fact]
    public void GetDisplayName_nullは例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => ((Enum)null!).GetDisplayName());
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
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null!).SelectWhere(x => (true, x)));
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

    [Fact]
    public void ToSnakeCase_nullは例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => ((string)null!).ToSnakeCase());
    }
}

public class TimeProviderExtensionsTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo localTimeZone) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => localTimeZone;
    }

    [Fact]
    public void GetLocalToday_ローカルタイムゾーンの日付を返す()
    {
        var tokyo = TimeZoneInfo.CreateCustomTimeZone("UTC+9", TimeSpan.FromHours(9), "UTC+9", "UTC+9");
        var provider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 10, 20, 0, 0, TimeSpan.Zero), tokyo);

        Assert.Equal(new DateOnly(2026, 1, 11), provider.GetLocalToday());
    }

    [Fact]
    public void GetLocalToday_nullは例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => ((TimeProvider)null!).GetLocalToday());
    }
}
