namespace D3Fragments.Tests;

public class PagerTests
{
    [Fact]
    public void Empty_データ0件でもTotalPagesは1になる()
    {
        var pager = Pager<int>.Empty(10);

        Assert.Equal(1, pager.TotalPages);
        Assert.False(pager.HasNextPage);
    }

    [Fact]
    public void TotalPages_端数切り上げで計算される()
    {
        var pager = new Pager<int>([1, 2, 3], pageNo: 1, pageSize: 2, totalCount: 3);

        Assert.Equal(2, pager.TotalPages);
        Assert.True(pager.HasNextPage);
    }

    [Theory]
    [InlineData(1, false, null, true, 2)]
    [InlineData(2, true, 1, true, 3)]
    [InlineData(3, true, 2, false, null)]
    public void 前後のページ_位置に応じて有無とページ番号を返す(int pageNo, bool hasPrev, int? prev, bool hasNext, int? next)
    {
        var pager = new Pager<int>([1], pageNo, pageSize: 2, totalCount: 5);

        Assert.Equal(hasPrev, pager.HasPrevPage);
        Assert.Equal(prev, pager.PrevPage);
        Assert.Equal(hasNext, pager.HasNextPage);
        Assert.Equal(next, pager.NextPage);
    }

    [Fact]
    public void Constructor_指定した値を保持し項目は複製する()
    {
        var items = new List<int> { 1, 2 };

        var pager = new Pager<int>(items, pageNo: 2, pageSize: 2, totalCount: 4);
        items.Add(3);

        Assert.Equal([1, 2], pager.Items);
        Assert.Equal(2, pager.PageNo);
        Assert.Equal(2, pager.PageSize);
        Assert.Equal(4, pager.TotalCount);
    }

    [Fact]
    public void TotalPages_割り切れる場合は切り上げない()
    {
        Assert.Equal(2, new Pager<int>([], pageNo: 1, pageSize: 2, totalCount: 4).TotalPages);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 10, -1)]
    public void Constructor_範囲外の値は例外になる(int pageNo, int pageSize, int totalCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pager<int>([], pageNo, pageSize, totalCount));
    }

    [Fact]
    public void Constructor_境界の値は受け付ける()
    {
        var pager = new Pager<int>([], pageNo: 1, pageSize: 1, totalCount: 0);

        Assert.Equal(1, pager.TotalPages);
    }

    [Fact]
    public void Constructor_nullの項目は例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => new Pager<int>(null!, pageNo: 1, pageSize: 10, totalCount: 0));
    }

    [Fact]
    public void Empty_ページサイズが0以下の場合は例外になる()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Pager<int>.Empty(0));
    }
}
