# EF Core 連携

`Sashimi343.D3Fragments.EFCore` パッケージは D3Fragments のドメインモデルを EF Core で永続化する機能を提供します。
インフラストラクチャ層で使います。

| ページ | 主な型 |
|---|---|
| [DbContext とマッピング](01-dbcontext.md) | `D3DbContext`、`ComplexValueObject`、`AddD3FragmentsEFCore` |
| [リポジトリとページング](02-repositories.md) | `RepositoryBase<TContext, TAggregate>`、`ToPagedListAsync`、`Pager<T>` |

## サービスの登録

`AddD3FragmentsEFCore` に、次の2つを渡します。

- ドメイン層のアセンブリと、リポジトリの実装があるアセンブリ
- DB プロバイダーを設定するデリゲート

```csharp
builder.Services.AddD3FragmentsEFCore<LibraryDbContext>(
    [typeof(Book).Assembly, typeof(BookRepository).Assembly],
    options => options.UseNpgsql(connectionString));
```

`DbContext`、`IUnitOfWork` の実装、渡したアセンブリにあるリポジトリの実装が登録されます。
登録されるサービスの一覧は [サービスの登録](../02-service-registration.md) を参照してください。
