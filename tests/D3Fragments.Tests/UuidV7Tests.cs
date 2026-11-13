using System.Text;

namespace D3Fragments.Tests;

public class UuidV7Tests
{
    [Fact]
    public void ToString_Guidと同じD形式の文字列を返す()
    {
        var id = UuidV7.NewUuidV7();

        Assert.Equal(id.Value.ToString(), id.ToString());
        Assert.Equal(id.Value.ToString(), $"{id}");
    }

    [Fact]
    public void TryFormat_書式指定子に従って書式化する()
    {
        var id = UuidV7.NewUuidV7();

        Assert.Equal(id.Value.ToString("N"), $"{id:N}");
        Assert.False(id.TryFormat(new char[35], out _, "D", null));

        var utf8 = new byte[36];
        Assert.True(id.TryFormat(utf8, out var bytesWritten, "D", null));
        Assert.Equal(id.Value.ToString(), Encoding.UTF8.GetString(utf8, 0, bytesWritten));
    }

    [Fact]
    public void 等価演算子_同じ値なら等しい()
    {
        var id = UuidV7.NewUuidV7();
        var same = UuidV7.FromGuid(id.Value);

        Assert.True(id == same);
        Assert.False(id != same);
        Assert.True(id != UuidV7.NewUuidV7());
    }

    // バージョン4のGUID（13桁目が4）
    private const string V4 = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";

    [Fact]
    public void Empty_既定値で空として扱う()
    {
        Assert.Equal(default, UuidV7.Empty);
        Assert.True(UuidV7.Empty.IsEmpty);
        Assert.False(UuidV7.NewUuidV7().IsEmpty);
    }

    [Fact]
    public void FromGuid_バージョン7以外は例外になる()
    {
        Assert.Throws<ArgumentException>(() => UuidV7.FromGuid(Guid.Parse(V4)));
        Assert.Throws<ArgumentException>(() => UuidV7.FromGuid(Guid.Empty));
    }

    [Fact]
    public void TryFromGuid_バージョン7のみ変換する()
    {
        var guid = Guid.CreateVersion7();

        Assert.True(UuidV7.TryFromGuid(guid, out var result));
        Assert.Equal(guid, result.Value);
        Assert.False(UuidV7.TryFromGuid(Guid.Parse(V4), out var failed));
        Assert.Equal(default, failed);
    }

    [Fact]
    public void Create_指定した時刻のUUIDを生成し時刻順に並ぶ()
    {
        var earlier = UuidV7.Create(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var later = UuidV7.Create(new DateTimeOffset(2026, 1, 1, 0, 0, 1, TimeSpan.Zero));

        Assert.Equal(7, earlier.Value.Version);
        Assert.True(earlier.CompareTo(later) < 0);
        Assert.True(later.CompareTo(earlier) > 0);
        Assert.Equal(0, earlier.CompareTo(earlier));
    }

    [Fact]
    public void Parse_各入力形式からバージョン7を変換する()
    {
        var id = UuidV7.NewUuidV7();
        var text = id.ToString();

        Assert.Equal(id, UuidV7.Parse(text, null));
        Assert.Equal(id, UuidV7.Parse(text.AsSpan(), null));
        Assert.Equal(id, UuidV7.Parse(Encoding.UTF8.GetBytes(text), null));
    }

    [Fact]
    public void Parse_バージョン7以外や不正な文字列は例外になる()
    {
        Assert.Throws<ArgumentException>(() => UuidV7.Parse(V4, null));
        Assert.Throws<ArgumentException>(() => UuidV7.Parse(V4.AsSpan(), null));
        Assert.Throws<ArgumentException>(() => UuidV7.Parse(Encoding.UTF8.GetBytes(V4), null));
        Assert.Throws<FormatException>(() => UuidV7.Parse("not-a-uuid", null));
    }

    [Fact]
    public void TryParse_各入力形式からバージョン7のみ変換する()
    {
        var id = UuidV7.NewUuidV7();
        var text = id.ToString();

        Assert.True(UuidV7.TryParse(text, null, out var fromString));
        Assert.Equal(id, fromString);
        Assert.True(UuidV7.TryParse(text.AsSpan(), null, out var fromSpan));
        Assert.Equal(id, fromSpan);
        Assert.True(UuidV7.TryParse(Encoding.UTF8.GetBytes(text), null, out var fromUtf8));
        Assert.Equal(id, fromUtf8);
    }

    [Theory]
    [InlineData(V4)]
    [InlineData("not-a-uuid")]
    [InlineData(null)]
    public void TryParse_バージョン7以外や不正な文字列は失敗して既定値を返す(string? text)
    {
        Assert.False(UuidV7.TryParse(text, null, out var fromString));
        Assert.Equal(default, fromString);
        Assert.False(UuidV7.TryParse(text.AsSpan(), null, out var fromSpan));
        Assert.Equal(default, fromSpan);
        Assert.False(UuidV7.TryParse(Encoding.UTF8.GetBytes(text ?? string.Empty), null, out var fromUtf8));
        Assert.Equal(default, fromUtf8);
    }

    [Fact]
    public void CompareTo_object版はnullより大きくUuidV7以外は例外になる()
    {
        var id = UuidV7.NewUuidV7();

        Assert.Equal(1, id.CompareTo(null));
        Assert.Equal(0, id.CompareTo((object)UuidV7.FromGuid(id.Value)));
        Assert.Throws<ArgumentException>(() => id.CompareTo((object)id.Value));
    }

    [Fact]
    public void Equals_object版は同じ値のUuidV7のみ等しい()
    {
        var id = UuidV7.NewUuidV7();

        Assert.True(id.Equals((object)UuidV7.FromGuid(id.Value)));
        Assert.False(id.Equals((object)id.Value));
        Assert.False(id.Equals(null));
        Assert.Equal(id.Value.GetHashCode(), id.GetHashCode());
    }

    [Fact]
    public void ToString_書式指定子とプロバイダーをGuidと同様に扱う()
    {
        var id = UuidV7.NewUuidV7();

        Assert.Equal(id.Value.ToString("B", null), id.ToString("B", null));
    }
}
