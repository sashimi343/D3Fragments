# DbContext とマッピング

## D3DbContext を継承する

アプリケーションの `DbContext` は `D3DbContext` を継承します。
コンストラクタでは `DbContextOptions` を基底クラスに渡します。

```csharp
using D3Fragments.EFCore;
using Microsoft.EntityFrameworkCore;

public sealed class LibraryDbContext(DbContextOptions options) : D3DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Member> Members => Set<Member>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(builder =>
        {
            builder.ComplexValueObject(x => x.Isbn);
            builder.ComplexValueObject(x => x.Title);
        });
        modelBuilder.Entity<Member>(builder =>
        {
            builder.ComplexValueObject(x => x.Name);
            builder.ComplexValueObject(x => x.EmailAddress);
        });
    }
}
```

`D3DbContext` は、次の設定を自動で行います。

| 対象 | 設定 |
|---|---|
| `UuidV7` のプロパティ | `Guid` として保存する値コンバーター |
| 型安全な列挙型のプロパティ | キーで保存する値コンバーター（`UseD3Fragments` に渡したアセンブリの型が対象） |
| `Entity` と `AggregateRoot` の `Version` | 楽観的同時実行制御のトークン |
| `Entity` と `AggregateRoot` の `UpdatedAt` / `Version` | 変更を保存するときに更新する |

`ConfigureConventions` をオーバーライドする場合は、`base.ConfigureConventions` を呼んでください。
呼ばないと、上記の設定が適用されません。

## 値オブジェクトをマッピングする

値オブジェクトのプロパティは `ComplexValueObject` で EF Core の Complex Type としてマッピングします。
既定ではプロパティ名の列1つ（`Email_Value` ではなく `Email`）に保存します。

```csharp
modelBuilder.Entity<Member>(builder =>
{
    builder.ComplexValueObject(x => x.EmailAddress)
        .Property(x => x.Value).HasMaxLength(254);
});
```

戻り値のビルダーで、列名や長さなどを変えられます。

Complex Type でマッピングすると、クエリの中でラップしている値を直接使えます。

```csharp
var members = await DbContext.Members
    .Where(x => x.EmailAddress.Value.EndsWith("@example.com"))
    .ToListAsync(cancellationToken);
```

値オブジェクトのマッピングを忘れると、EF Core は値オブジェクトをエンティティとして扱おうとします。
`D3DbContext` はこれを検出し、マッピングを忘れたプロパティの名前を示す `InvalidOperationException` をモデルの構築時にスローします。

> [!NOTE]
> 値オブジェクトのプロパティを持つエンティティは、値オブジェクトを受け取らない引数なしのコンストラクタ（`private` でよい）が必要です。
> EF Core は、Complex Type のプロパティをコンストラクタの引数に渡せないためです。

## 楽観的同時実行制御

`Entity` と `AggregateRoot` の `Version` は同時実行トークンとして扱われます。
ほかの処理が先に同じ集約を更新していた場合、保存時に EF Core が競合を検出します。
Unit of Work はこれを `ConflictOccurredException`（`ErrorType.Conflict`）に変換してスローします。

子エンティティだけを変更した場合も、集約ルートの `Version` が増えます。
そのため、集約の一部が同時に変更された場合も競合として検出できます。
子エンティティを別の集約へ移した場合は、移動元と移動先の両方の集約ルートの `Version` が増えます。

## UseD3Fragments

`AddD3FragmentsEFCore` を使わずに `DbContext` を設定する場合（テストなど）は、`UseD3Fragments` に、型安全な列挙型を定義したアセンブリを渡します。

```csharp
var options = new DbContextOptionsBuilder<LibraryDbContext>()
    .UseNpgsql(connectionString)
    .UseD3Fragments([typeof(Book).Assembly])
    .Options;
```

## 関連する API

- @D3Fragments.EFCore.D3DbContext
- @D3Fragments.EFCore.ValueObjectComplexTypeExtensions
- @D3Fragments.EFCore.D3FragmentsDbContextOptionsBuilderExtensions
- @D3Fragments.EFCore.D3FragmentsEFCoreServiceCollectionExtensions
- <xref:D3Fragments.EFCore.EfUnitOfWork`1>
