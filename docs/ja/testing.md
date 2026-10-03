# テストの書き方

D3Fragmentsを利用したアプリケーションのテスト方針と、`D3Fragments.Testing` / `D3Fragments.Testing.EFCore` の使い方。

## パッケージ構成

| パッケージ | 内容 | 主な依存 |
|---|---|---|
| `D3Fragments.Testing` | `ResultAssert`、`DomainEventAssert`、`SpyDomainEventDispatcher`、`TestData`、`TestTime` | xunit.v3.assert、Microsoft.Extensions.TimeProvider.Testing |
| `D3Fragments.Testing.EFCore` | `DatabaseContainerFixture`、`TransactionalTestBase` | D3Fragments.EFCore、Testcontainers、xunit.v3.extensibility.core |

DBMS固有のパッケージ（`Testcontainers.PostgreSql`、`Npgsql.EntityFrameworkCore.PostgreSQL` 等）はアプリケーション側で参照する。

## 層ごとのテスト方針

| 対象 | 手段 | 参照するパッケージ |
|---|---|---|
| ドメイン（集約・値オブジェクト・ドメインルール） | 純粋な単体テスト | Testing |
| Handler（Command / Query） | 実DB＋テストごとのトランザクションロールバック | Testing、Testing.EFCore |
| Repository | 実DB＋テストごとのトランザクションロールバック | Testing、Testing.EFCore |
| E2E | 少数に絞る | 必要に応じて |

- Repositoryには複雑な読み取りも実装するため、Repositoryのフェイクは作らない。Handlerのテストも本物のRepositoryを実DBに対して使う
- 1テストあたりのコストはトランザクションの開始とロールバックのみで、コンテナはテストアセンブリ全体で1つだけ起動する
- ドメインイベントは「発火したこと」だけを検証する。ドメインイベントハンドラは別のテストで検証する

### アプリケーション側のテストプロジェクト構成例

```
Xxx.Domain.Tests        → Xxx.Domain, D3Fragments.Testing
Xxx.Application.Tests   → Xxx.Tests.Helpers
Xxx.Infrastructure.Tests→ Xxx.Tests.Helpers
Xxx.Tests.Helpers       → Xxx.Infrastructure, D3Fragments.Testing.EFCore, Testcontainers.PostgreSql 等
                          （フィクスチャとテストデータ生成ヘルパーを置く）
```

Handlerのテストが実DBを使うため、`Xxx.Application.Tests` は `Xxx.Infrastructure` に依存する。プロダクションコードの依存方向とは異なるが、テストに限り許容する。

## セットアップ

### 1. フィクスチャを定義する

```csharp
public sealed class AppDbFixture : DatabaseContainerFixture<AppDbContext>
{
    protected override IDatabaseContainer CreateContainer() =>
        new PostgreSqlBuilder("postgres:18-alpine").Build();

    public override AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseD3Fragments([typeof(Order).Assembly], TestTime.CreateProvider())
            .Options);
}
```

スキーマは既定で `EnsureCreatedAsync` により作成する。マイグレーションを適用する場合は `InitializeDatabaseAsync` をオーバーライドする。

### 2. アセンブリフィクスチャとして登録する

テストプロジェクト内のいずれかのファイルに記述する。これによりコンテナはテストアセンブリ全体で1つだけ起動する。

```csharp
[assembly: AssemblyFixture(typeof(AppDbFixture))]
```

### 3. アプリケーション用の基底クラスを用意する（任意）

```csharp
public abstract class AppDbTestBase(AppDbFixture fixture)
    : TransactionalTestBase<AppDbContext, AppDbFixture>(fixture);
```

## Handlerのテスト

```csharp
public sealed class PlaceOrderHandlerTests(AppDbFixture fixture) : AppDbTestBase(fixture)
{
    [Fact]
    public async Task 注文を作成できる()
    {
        var customer = CustomerTestFactory.Create();
        await SeedAsync(customer);
        var handler = new PlaceOrderHandler(new OrderRepository(DbContext), new CustomerRepository(DbContext));

        var result = await ExecuteAsync(handler, new PlaceOrderCommand(customer.Id));

        var orderId = ResultAssert.IsSuccess(result);
        await AssertEntityExistsAsync<Order>(x => x.Id == orderId);
        DomainEventAssert.Raised<OrderPlaced>(DispatchedEvents);
    }
}
```

- Repositoryは必ず `DbContext` を渡して生成する。別のDbContextからはトランザクション内のデータを参照できない
- `ExecuteAsync` はHandlerを実行した後、Commandの場合は `UnitOfWork.SaveChangesAsync` で保存し、ChangeTrackerをクリアする。Pipeline上の `UnitOfWorkBehavior` と同じ保存処理を通るため、発火したドメインイベントは `DispatchedEvents` に記録される
- `TransactionBehavior` は適用しない（テスト自体がトランザクション内で実行されるため）。Handlerの中で `IUnitOfWork.ExecuteInTransactionAsync` を直接呼ぶ場合は入れ子のトランザクションになり失敗する
- Handlerが `IUnitOfWork` を必要とする場合は、基底クラスの `UnitOfWork` プロパティを渡す
- 保存後の状態を詳しく検証したい場合は、Repositoryで取得し直す（`ExecuteAsync` の後はChangeTrackerがクリアされているため、DBから読み込まれる）

## ドメインのテスト

DBを使わない。集約が保持するドメインイベントは `DomainEventAssert` に集約を渡して検証する。

```csharp
[Fact]
public void 注文をキャンセルするとイベントが発火する()
{
    var order = OrderTestFactory.CreatePlaced();

    ResultAssert.IsSuccess(order.Cancel(TestTime.Now));

    DomainEventAssert.Raised<OrderCanceled>(order);
}
```

## テストデータの準備

テストデータ生成はドメイン固有であるため、アプリケーション側で集約ごとに生成ヘルパー（Object Mother）を作成する。以下の規約に従う。

1. 集約ごとに `XxxTestFactory` を作成し、引数はすべて省略可能にして妥当な既定値を持たせる。テストでは検証に関係する値だけを指定する
2. 関連する集約・エンティティまでオブジェクトグラフを結線して生成し、`SeedAsync` 1回で登録できるようにする
3. 一意制約のある値は `TestData.Unique("DEV")`（`DEV-1`、`DEV-2`…）で自動採番する。並列実行されるテスト同士の衝突を防ぐ
4. ドメイン上意味のある時刻（注文日時等）は `TestTime.Now` を使用する。`TimeProvider` が必要な箇所には `TestTime.CreateProvider()` を渡す。`CreatedAt` / `UpdatedAt` はシステム日時で自動記録される監査項目のため固定できない。検証する場合は範囲（`Assert.InRange`）で確認する
5. 特定の状態の集約（キャンセル済み、返却済みなど）は、ドメインのメソッドを順に呼び出す `CreateCanceled()` のようなメソッドとして提供する。リフレクションで内部状態を書き換えない（不変条件を満たさないデータでテストすることになるため）

```csharp
public static class OrderTestFactory
{
    public static Order CreatePlaced(Customer? customer = null, int quantity = 1) =>
        Order.Place(customer ?? CustomerTestFactory.Create(), quantity, TestTime.Now).GetValueOrThrow();

    public static Order CreateCanceled(Customer? customer = null)
    {
        var order = CreatePlaced(customer);
        order.Cancel(TestTime.Now).ThrowIfFailure();
        return order;
    }
}
```

`SeedAsync` は型の異なるエンティティをまとめて受け取る。登録したエンティティが保持するドメインイベントは発火せずに破棄する（既存データとして扱うため）。

```csharp
await SeedAsync(customer, product, order);
await SeedAsync(orders); // IEnumerable もそのまま渡せる
```

## xUnit v3に関する注意

- テストプロジェクトには `<OutputType>Exe</OutputType>` が必要
- `dotnet test` はMicrosoft Testing Platformで実行する。`global.json` に以下を設定する

  ```json
  "test": { "runner": "Microsoft.Testing.Platform" }
  ```

- アナライザー `xUnit1051` により、`CancellationToken` を受け取るメソッドには `TestContext.Current.CancellationToken` を渡す必要がある（`TreatWarningsAsErrors` 有効時はビルドエラーになる）
- `IAsyncLifetime` の `InitializeAsync` / `DisposeAsync` は `ValueTask` を返す
