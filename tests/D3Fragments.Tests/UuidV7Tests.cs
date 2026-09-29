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
}
