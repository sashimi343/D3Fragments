namespace D3Fragments.EFCore.Tests;

public class PagingExtensionsTests
{
    [Fact]
    public async Task ToPagedListAsync_ページ番号が不正な場合はDomainValidationExceptionを送出する()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<DomainValidationException>(() => dbContext.Entities.ToPagedListAsync(pageNo: 0, pageSize: 10, CancellationToken.None));
    }

    [Fact]
    public async Task ToPagedListAsync_ページサイズが上限を超える場合はDomainValidationExceptionを送出する()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<DomainValidationException>(() => dbContext.Entities.ToPagedListAsync(pageNo: 1, pageSize: 10, CancellationToken.None, maxPageSize: 5));
    }

    [Fact]
    public async Task ToPagedListAsync_データが0件の場合は空ページを返す()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await dbContext.Entities.ToPagedListAsync(pageNo: 1, pageSize: 10, CancellationToken.None);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ToPagedListAsync_指定したページのデータを返す()
    {
        await using var dbContext = await CreateWithValuesAsync(5);

        var result = await dbContext.Entities.OrderBy(x => x.Value).ToPagedListAsync(pageNo: 2, pageSize: 2, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    private static async Task<TestDbContext> CreateWithValuesAsync(int count)
    {
        var dbContext = TestDbContextFactory.Create();
        dbContext.Entities.AddRange(Enumerable.Range(0, count).Select(x => new TestEntity(x)));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return dbContext;
    }

    [Theory]
    [InlineData(1, new[] { 0, 1 })]
    [InlineData(2, new[] { 2, 3 })]
    [InlineData(3, new[] { 4 })]
    public async Task ToPagedListAsync_ページ番号に対応する範囲の項目を返す(int pageNo, int[] expected)
    {
        await using var dbContext = await CreateWithValuesAsync(5);

        var result = await dbContext.Entities.OrderBy(x => x.Value).ToPagedListAsync(pageNo, pageSize: 2, CancellationToken.None);

        Assert.Equal(expected, result.Items.Select(x => x.Value));
        Assert.Equal(pageNo, result.PageNo);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task ToPagedListAsync_最終ページを超えるページ番号は空ページを返す()
    {
        await using var dbContext = await CreateWithValuesAsync(5);

        var result = await dbContext.Entities.OrderBy(x => x.Value).ToPagedListAsync(pageNo: 10, pageSize: 2, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToPagedListAsync_ページサイズが0以下の場合はDomainValidationExceptionを送出する(int pageSize)
    {
        await using var dbContext = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<DomainValidationException>(() => dbContext.Entities.ToPagedListAsync(pageNo: 1, pageSize, CancellationToken.None));
    }

    [Fact]
    public async Task ToPagedListAsync_ページサイズが上限ちょうどの場合は取得できる()
    {
        await using var dbContext = await CreateWithValuesAsync(5);

        var result = await dbContext.Entities.ToPagedListAsync(pageNo: 1, pageSize: 5, CancellationToken.None, maxPageSize: 5);

        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task ToPagedListAsync_読み飛ばす件数が桁あふれするページ番号はDomainValidationExceptionを送出する()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var maxPageNo = (int.MaxValue / 200) + 1;

        await Assert.ThrowsAsync<DomainValidationException>(() => dbContext.Entities.ToPagedListAsync(pageNo: maxPageNo + 1, pageSize: 200, CancellationToken.None));
        Assert.Equal(maxPageNo, (await dbContext.Entities.ToPagedListAsync(pageNo: maxPageNo, pageSize: 200, CancellationToken.None)).PageNo);
    }

    [Fact]
    public async Task ToPagedListAsync_データが0件の場合も指定したページ番号の空ページを返す()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await dbContext.Entities.ToPagedListAsync(pageNo: 3, pageSize: 10, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.PageNo);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalCount);
    }
}
