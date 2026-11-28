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

## D3DbContext を継承できない場合

`IdentityDbContext` など別の基底クラスから派生する `DbContext` は、`D3DbContext` を継承できません。
その場合は、`ConfigureConventions` で `ApplyD3FragmentsConventions` を呼び出します。

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Account, IdentityRole<Guid>, Guid>(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyD3FragmentsConventions(options);
    }
}
```

`ApplyD3FragmentsConventions` は、`D3DbContext` と同じ値コンバーターと同時実行トークンを設定し、値オブジェクトのマッピング漏れを検出します。
引数には、コンストラクタで受け取った `DbContextOptions` を渡します（型安全な列挙型を探すアセンブリを、`UseD3Fragments` の設定から読み取るためです）。

保存時の `UpdatedAt` / `Version` の更新は、`UseD3Fragments`（`AddD3FragmentsEFCore` が呼び出す）が登録するインターセプターが行います。
`UseD3Fragments` で設定したのに `ApplyD3FragmentsConventions` を呼び忘れると、保存時に `InvalidOperationException` がスローされます。

Identity と組み合わせる方法は [ASP.NET Core Identity 連携](../07-identity/01-setup.md) を参照してください。

## 値オブジェクトをマッピングする

値オブジェクトのプロパティのマッピング方法は2つあります。
通常は `ComplexValueObject` を使い、一意制約などのインデックスが必要なプロパティだけ `HasValueObjectConversion` を使います。

| | `ComplexValueObject`（通常） | `HasValueObjectConversion` |
|---|---|---|
| マッピング | Complex Type | 値コンバーター |
| 使う場面 | インデックスのない値オブジェクト | 一意制約・インデックスのある値オブジェクト |
| インデックス | 定義できない | 定義できる |
| クエリでラップしている値を使う（`x.Email.Value.EndsWith(...)`） | できる | できない（`x.Email == email` のように値オブジェクトどうしで比較する） |
| 列名 | プロパティ名（命名規約は適用されない） | 命名規約に従う |

### Complex Type としてマッピングする

`ComplexValueObject` は、値オブジェクトのプロパティを EF Core の Complex Type としてマッピングします。
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

列名はプロパティ名で明示的に設定するため、`UseSnakeCaseNamingConvention()` などの命名規約は適用されません。
命名規約に合わせた列名にするには、プロパティごとに列名を指定します。

```csharp
builder.ComplexValueObject(x => x.EmailAddress)
    .Property(x => x.Value).HasColumnName("email_address");
```

### 値コンバーターでマッピングする

一意制約などのインデックスが必要なプロパティは、`HasValueObjectConversion` で値コンバーターを使ってマッピングします。
EF Core は Complex Type のプロパティにインデックスを定義できないためです。
ラップしている型（`string` など）は指定しなくても、値オブジェクトの型から決まります。

```csharp
modelBuilder.Entity<Member>(builder =>
{
    builder.Property(x => x.EmailAddress).HasValueObjectConversion().HasMaxLength(254);
    builder.HasIndex(x => x.EmailAddress).IsUnique();
});
```

列名は設定しないため、DbContext に設定した命名規約（`UseSnakeCaseNamingConvention()` なら `email_address`）に従います。
DB から読み込むときは `Create` で値オブジェクトを生成するため、`ValidateValue` の検証を満たさない値は例外になります。

クエリでは、ラップしている値ではなく値オブジェクトどうしを比較します。

```csharp
var member = await DbContext.Members
    .SingleOrDefaultAsync(x => x.EmailAddress == emailAddress, cancellationToken);
```

一意制約に違反した場合、保存時に `DbUpdateException` がスローされます。

### マッピングを忘れた場合

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
- @D3Fragments.EFCore.D3FragmentsModelConfigurationBuilderExtensions
- @D3Fragments.EFCore.ValueObjectComplexTypeExtensions
- @D3Fragments.EFCore.ValueObjectConversionExtensions
- @D3Fragments.EFCore.D3FragmentsDbContextOptionsBuilderExtensions
- @D3Fragments.EFCore.D3FragmentsEFCoreServiceCollectionExtensions
- <xref:D3Fragments.EFCore.EfUnitOfWork`1>
