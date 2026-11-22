# テスト

D3Fragments を使うアプリケーションのテストの方針と、テスト用パッケージの使い方を説明します。

## パッケージ

| パッケージ | 内容 | 主な依存先 |
|---|---|---|
| `Sashimi343.D3Fragments.Testing` | `ResultAssert`、`DomainEventAssert`、`SpyDomainEventDispatcher`、`TestData`、`TestTime` | xunit.v3.assert、Microsoft.Extensions.TimeProvider.Testing |
| `Sashimi343.D3Fragments.Testing.EFCore` | `DatabaseContainerFixture`、`TransactionalTestBase` | Sashimi343.D3Fragments.EFCore、Testcontainers、xunit.v3.extensibility.core |

DBMS ごとのパッケージ（`Testcontainers.PostgreSql`、`Npgsql.EntityFrameworkCore.PostgreSQL` など）は、アプリケーション側のテストプロジェクトで参照してください。

## 層ごとのテストの方針

| 対象 | 方法 | 使うパッケージ |
|---|---|---|
| ドメイン（集約・値オブジェクト・ドメインルール） | DB を使わない単体テスト | Testing |
| Handler（Command / Query） | 実 DB を使い、テストごとにトランザクションをロールバックする | Testing、Testing.EFCore |
| リポジトリ | 実 DB を使い、テストごとにトランザクションをロールバックする | Testing、Testing.EFCore |
| E2E | 数を絞る | 必要に応じて |

- リポジトリには複雑な読み取りも実装するため、リポジトリのフェイクは作りません。Handler のテストでも、本物のリポジトリを実 DB に対して使います。
- DB のコンテナはテストアセンブリ全体で1つだけ起動します。1テストあたりのコストはトランザクションの開始とロールバックだけです。
- ドメインイベントは「発行されたこと」だけを検証します。ドメインイベントハンドラは別のテストで検証します。

### テストプロジェクトの構成例

```text
Xxx.Domain.Tests         → Xxx.Domain, Sashimi343.D3Fragments.Testing
Xxx.Application.Tests    → Xxx.Tests.Helpers
Xxx.Infrastructure.Tests → Xxx.Tests.Helpers
Xxx.Tests.Helpers        → Xxx.Infrastructure, Sashimi343.D3Fragments.Testing.EFCore, Testcontainers.PostgreSql など
                           （フィクスチャとテストデータの生成ヘルパーを置く）
```

Handler のテストで実 DB を使うため、`Xxx.Application.Tests` は `Xxx.Infrastructure` に依存します。
本番のコードの依存の向きとは違いますが、テストに限って許容します。

## セットアップ

### 1. フィクスチャを定義する

`DatabaseContainerFixture<TDbContext>` を継承し、DB のコンテナと `DbContext` の作り方を定義します。

```csharp
public sealed class AppDbFixture : DatabaseContainerFixture<AppDbContext>
{
    protected override IDatabaseContainer CreateContainer() =>
        new PostgreSqlBuilder("postgres:18-alpine").Build();

    public override AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseD3Fragments([typeof(Order).Assembly])
            .Options);
}
```

スキーマは既定では `EnsureCreatedAsync` で作ります。
マイグレーションを適用する場合は、`InitializeDatabaseAsync` をオーバーライドします。

### 2. アセンブリフィクスチャとして登録する

テストプロジェクトのどれか1つのファイルに書きます。
これで、コンテナがテストアセンブリ全体で1つだけ起動します。

```csharp
[assembly: AssemblyFixture(typeof(AppDbFixture))]
```

### 3. アプリケーション用の基底クラスを用意する（任意）

```csharp
public abstract class AppDbTestBase(AppDbFixture fixture)
    : TransactionalTestBase<AppDbContext, AppDbFixture>(fixture);
```

## Handler のテスト

```csharp
public sealed class PlaceOrderHandlerTests(AppDbFixture fixture) : AppDbTestBase(fixture)
{
    [Fact]
    public async Task 注文を作成できる()
    {
        var customer = CustomerTestFactory.Create();
        await SeedAsync(customer);
        var handler = new PlaceOrderHandler(new OrderRepository(DbContext), new CustomerRepository(DbContext));

        var orderId = await ExecuteAsync(handler, new PlaceOrderCommand(customer.Id));

        await AssertEntityExistsAsync<Order>(x => x.Id == orderId);
        DomainEventAssert.Raised<OrderPlaced>(DispatchedEvents);
    }
}
```

- リポジトリは必ず `DbContext` を渡して作ります。ほかの `DbContext` からはトランザクション内のデータを参照できません。
- `ExecuteAsync` は Handler を実行し、Handler の応答を返します。Command の場合は、その後に `UnitOfWork.SaveChangesAsync` で保存して、変更追跡をクリアします。本番の `UnitOfWorkBehavior` と同じ保存処理を通るため、発行されたドメインイベントは `DispatchedEvents` に記録されます。
- `TransactionBehavior` は適用しません。テスト自体がトランザクション内で動くためです。Handler の中で `IUnitOfWork.ExecuteInTransactionAsync` を直接呼ぶと、トランザクションが入れ子になって失敗します。
- Handler が `IUnitOfWork` を必要とする場合は、基底クラスの `UnitOfWork` プロパティを渡します。
- 保存後の状態を詳しく検証したい場合は、リポジトリで取得し直します。`ExecuteAsync` の後は変更追跡がクリアされているため、DB から読み込まれます。

## ドメインのテスト

DB は使いません。
集約が記録したドメインイベントは、`DomainEventAssert` に集約を渡して検証します。

```csharp
[Fact]
public void 注文をキャンセルするとイベントが発行される()
{
    var order = OrderTestFactory.CreatePlaced();

    order.Cancel(TestTime.Now);

    DomainEventAssert.Raised<OrderCanceled>(order);
}

[Fact]
public void 空の書名は検証エラーになる()
{
    var result = BookTitle.TryCreate("");

    ResultAssert.HasError(result, ErrorType.Validation);
}
```

## テストデータの準備

テストデータの作り方はドメインごとに違うため、アプリケーション側で集約ごとに生成ヘルパー（Object Mother）を作ります。
次の規約に従ってください。

1. 集約ごとに `XxxTestFactory` を作ります。引数はすべて省略できるようにし、妥当な既定値を持たせます。テストでは検証に関係する値だけを指定します。
2. 関連する集約・エンティティまでつないだ状態で作り、`SeedAsync` 1回で登録できるようにします。
3. 一意制約のある値は `TestData.Unique("DEV")`（`DEV-1`、`DEV-2`…）で採番します。並列に実行されるテスト同士の衝突を防ぐためです。
4. 業務上意味のある日時（注文日時など）には `TestTime.Now` を使います。`TimeProvider` が必要な箇所には `TestTime.CreateProvider()` を渡します。`CreatedAt` / `UpdatedAt` はシステム日時で自動記録されるため、固定できません。検証する場合は、範囲（`Assert.InRange`）で確認します。
5. 特定の状態の集約（キャンセル済み、返却済みなど）は、ドメインのメソッドを順に呼ぶ `CreateCanceled()` のようなメソッドで作ります。リフレクションで内部の状態を書き換えないでください。不変条件を満たさないデータでテストすることになるためです。

```csharp
public static class OrderTestFactory
{
    public static Order CreatePlaced(Customer? customer = null, int quantity = 1) =>
        Order.Place(customer ?? CustomerTestFactory.Create(), quantity, TestTime.Now);

    public static Order CreateCanceled(Customer? customer = null)
    {
        var order = CreatePlaced(customer);
        order.Cancel(TestTime.Now);
        return order;
    }
}
```

`SeedAsync` には型の違うエンティティをまとめて渡せます。
登録したエンティティが記録しているドメインイベントは、発行せずに破棄します（既存のデータとして扱うため）。

```csharp
await SeedAsync(customer, product, order);
await SeedAsync(orders); // IEnumerable もそのまま渡せる
```

## xUnit v3 の注意点

- テストプロジェクトには `<OutputType>Exe</OutputType>` が必要です。
- `dotnet test` は Microsoft Testing Platform で実行します。`global.json` に次の設定を書きます。

  ```json
  "test": { "runner": "Microsoft.Testing.Platform" }
  ```

- `CancellationToken` を受け取るメソッドには `TestContext.Current.CancellationToken` を渡す必要があります。渡さないとアナライザ `xUnit1051` が警告し、`TreatWarningsAsErrors` が有効ならビルドエラーになります。
- `IAsyncLifetime` の `InitializeAsync` / `DisposeAsync` は `ValueTask` を返します。

## 関連する API

- @D3Fragments.Testing.ResultAssert
- @D3Fragments.Testing.DomainEventAssert
- @D3Fragments.Testing.TestData
- @D3Fragments.Testing.TestTime
- <xref:D3Fragments.Testing.EFCore.DatabaseContainerFixture`1>
- <xref:D3Fragments.Testing.EFCore.TransactionalTestBase`2>
