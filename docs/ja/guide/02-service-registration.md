# サービスの登録

D3Fragments の機能はパッケージごとの拡張メソッドで DI コンテナに登録します。
このページでは、各メソッドが登録するサービスと、登録の決まりをまとめて説明します。

## 登録の例

[デモアプリケーション](https://github.com/sashimi343/D3Fragments/tree/main/demo/01_BasicWebApp) の `Program.cs` から、登録に関係する部分を抜き出しました。

```csharp
var builder = WebApplication.CreateBuilder(args);

// アプリケーション層：Handler、Pipeline Behavior、ドメインイベントハンドラ
builder.Services.AddD3FragmentsApplication([typeof(GetBookQuery).Assembly]);

// インフラストラクチャ層：DbContext、Unit of Work、リポジトリ
builder.Services.AddD3FragmentsEFCore<BasicWebAppDbContext>(
    [typeof(MemberType).Assembly, typeof(BookRepository).Assembly],
    options => options.UseSqlite(connectionString));

// プレゼンテーション層：エラー応答、値オブジェクトの JSON コンバーター
builder.Services.AddD3FragmentsAspNetCore();

var app = builder.Build();

app.UseExceptionHandler("/Error");
// ...
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api => api.UseCsrfProtection());
```

`AddD3FragmentsApplication` と `AddD3FragmentsEFCore` は両方を呼ぶ必要があります。
`AddD3FragmentsApplication` が登録する Pipeline Behavior は `IUnitOfWork` を使いますが、`IUnitOfWork` の実装は `AddD3FragmentsEFCore` が登録するためです。

## 共通の決まり

- 各メソッドはすでに登録されているサービスを置き換えません。既定の実装を自分の実装に替えたい場合は、各メソッドを呼ぶ前に登録します（[既定の実装を替える](#既定の実装を替える) を参照）。
- 同じメソッドを複数回呼んでも、サービスが重複して登録されることはありません。
- アセンブリを受け取るメソッドは、渡したアセンブリの中から抽象型とジェネリック型定義を除いた型を探して登録します。アセンブリを1つも渡さないと `ArgumentException` をスローします。
- `ILogger<T>` は登録しません。汎用ホストと ASP.NET Core では既定で登録されています。

## AddD3FragmentsApplication

`Sashimi343.D3Fragments.Application` パッケージのメソッドです。

```csharp
services.AddD3FragmentsApplication(assemblies, configure: null);
```

| 引数 | 内容 |
|---|---|
| `assemblies` | Handler とドメインイベントハンドラを探すアセンブリ。1つ以上必要 |
| `configure` | `D3FragmentsApplicationOptions` を設定するデリゲート（省略可）。設定項目は [ドメインイベントハンドラ](04-application/03-domain-event-handlers.md) を参照 |

登録するサービスは次のとおりです。

| サービス | 実装 | 有効期間 |
|---|---|---|
| `IRequestExecutor<TRequest, TResponse>` | `RequestExecutor<TRequest, TResponse>` | Scoped |
| `IPipelineBehavior<TRequest, TResponse>` | `TransactionBehavior<TRequest, TResponse>`、`UnitOfWorkBehavior<TRequest, TResponse>` | Scoped |
| `IDomainEventDispatcher` | `DomainEventDispatcher` | Scoped |
| `IRequestHandler<TRequest, TResponse>` | `assemblies` にある実装 | Scoped |
| `IBeforeCommitDomainEventHandler<TEvent>`、`IAfterCommitDomainEventHandler<TEvent>` | `assemblies` にある実装（同じイベントに複数登録できる） | Scoped |
| `TimeProvider` | `TimeProvider.System` | Singleton |

Handler のリクエスト型が `ICommand` と `IQuery` のどちらも実装していない場合、または両方を実装している場合は、`InvalidOperationException` をスローします（[Command と Query](04-application/01-commands-and-queries.md) を参照）。

## AddD3FragmentsEFCore

`Sashimi343.D3Fragments.EFCore` パッケージのメソッドです。

```csharp
services.AddD3FragmentsEFCore<TContext>(assemblies, configureDbContext);
```

| 引数 | 内容 |
|---|---|
| `TContext` | アプリケーションの `DbContext`。`D3DbContext` を継承している必要がある |
| `assemblies` | 型安全な列挙型とリポジトリの実装を探すアセンブリ。1つ以上必要 |
| `configureDbContext` | DB プロバイダーの指定（`UseNpgsql` など）など、`DbContext` を設定するデリゲート |

登録するサービスは次のとおりです。

| サービス | 実装 | 有効期間 |
|---|---|---|
| `TContext` | `configureDbContext` の後に `UseD3Fragments(assemblies)` を適用した `DbContext`（`AddDbContext` で登録） | Scoped |
| `IUnitOfWork` | `EfUnitOfWork<TContext>` | Scoped |
| `IDomainEventDispatchStrategy` | `ImmediateDispatchStrategy` | Scoped |
| リポジトリのインターフェース | `assemblies` にある実装 | Scoped |

`IDomainEventDispatchStrategy` は、Commit 後のハンドラにドメインイベントを配信する方法です。
既定の `ImmediateDispatchStrategy` は Commit の直後に Commit 後のハンドラを呼びます。

リポジトリの実装は、それが実装している `IRepository<TAggregate, TId>` と、それを継承したインターフェースのすべてで登録します。
たとえば `BookRepository : RepositoryBase<…>, IBookRepository` は、`IBookRepository` と `IRepository<Book, UuidV7>` の両方で取得できます。
同じインターフェースの実装が複数見つかった場合は、`InvalidOperationException` をスローします。

`assemblies` にある型安全な列挙型には、キーで保存する値コンバーターが登録されます。
そのため、型安全な列挙型を定義したドメイン層のアセンブリも渡してください。

## AddD3FragmentsAspNetCore

`Sashimi343.D3Fragments.AspNetCore` パッケージのメソッドです。

```csharp
services.AddD3FragmentsAspNetCore(configure: null);
```

| 引数 | 内容 |
|---|---|
| `configure` | `DomainProblemDetailsOptions` を設定するデリゲート（省略可）。設定項目は [エラー応答](06-aspnetcore/01-error-responses.md) を参照 |

登録するものは次のとおりです。

- Problem Details のサービス（`AddProblemDetails`）
- `DomainException` を Problem Details 応答に変換する例外ハンドラ（`IExceptionHandler`）
- 値オブジェクトの JSON コンバーター（`ValueObjectJsonConverterFactory`）。Minimal API の `JsonOptions` と MVC の `JsonOptions` の両方に追加する

例外ハンドラを有効にするには、`app.UseExceptionHandler()` を呼びます。
型安全な列挙型の JSON コンバーターは登録しません（[型安全な列挙型](03-domain/03-type-safe-enum.md) を参照）。

## ミドルウェアと OpenAPI

DI への登録のほかに、必要に応じて次の設定を行います。

| 設定 | 方法 | 詳細 |
|---|---|---|
| CSRF 対策のミドルウェア | `app.UseCsrfProtection()`。設定は `services.Configure<CsrfProtectionOptions>(...)` | [CSRF 対策](06-aspnetcore/03-csrf.md) |
| OpenAPI ドキュメントの変換 | `services.AddOpenApi(options => options.AddD3Fragments())` | [OpenAPI](06-aspnetcore/04-openapi.md) |
| 独自の Pipeline Behavior | `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MyBehavior<,>))` | [Pipeline とトランザクション](04-application/02-pipeline-and-transactions.md) |

## 既定の実装を替える

各メソッドは登録済みのサービスを置き換えないため、先に登録した実装が使われます。
次の例では開発環境だけ現在日時を変えられる `TimeProvider` を使います。

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<TimeProvider>(new DemoTimeProvider());
}

// TimeProvider が登録済みなので、TimeProvider.System は登録されない
builder.Services.AddD3FragmentsApplication([typeof(GetBookQuery).Assembly]);
```

同じ方法で、`IDomainEventDispatchStrategy` などもアプリケーション独自の実装に替えられます。

## 関連する API

- @D3Fragments.Application.D3FragmentsApplicationServiceCollectionExtensions
- @D3Fragments.EFCore.D3FragmentsEFCoreServiceCollectionExtensions
- @D3Fragments.AspNetCore.D3FragmentsServiceCollectionExtensions
- @D3Fragments.AspNetCore.CsrfProtectionApplicationBuilderExtensions
- @D3Fragments.AspNetCore.OpenApi.D3FragmentsOpenApiOptionsExtensions
