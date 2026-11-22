# 構成

## プロジェクトとパッケージ

Identity 連携で追加するパッケージは次のとおりです。

| 層 | 追加するパッケージ |
|---|---|
| インフラストラクチャ層 | `Sashimi343.D3Fragments.Identity.EFCore`、`Microsoft.AspNetCore.Identity.EntityFrameworkCore` |
| プレゼンテーション層 | なし（Cookie 側の拡張メソッドは `Sashimi343.D3Fragments.AspNetCore` に含まれる） |

アプリケーション層とドメイン層には、Identity のパッケージを追加しません。

## ユーザーの型

Identity のユーザーの型（`IdentityUser<TKey>` の派生クラス）は、インフラストラクチャ層に置きます。
ユーザー名・パスワードのハッシュ・二要素認証の設定などの、認証のためのデータだからです。

```csharp
public sealed class UserAccount : IdentityUser<UuidV7>
{
    public UserAccount()
    {
        Id = UuidV7.NewUuidV7();
        SecurityStamp = Guid.NewGuid().ToString();
    }
}
```

プレゼンテーション層は `SignInManager<TUser>` の型引数や Cookie の発行のためにユーザーの型を参照します。
プレゼンテーション層からインフラストラクチャ層の型への参照は [DFR0002](../../analyzers/DFR0002.md) で禁止されていますが、ユーザーの型は例外として許可しています（[アーキテクチャ](../01-architecture.md) の `Infrastructure_IdentityUser` を参照）。

### 業務上の利用者を分ける

ニックネームや入会日などの業務上のデータは、ユーザーの型に追加せず、ドメイン層の集約として定義します。
集約は Identity のユーザーの ID を持ちます。

```csharp
public sealed class Member : AggregateRoot
{
    // Identity のユーザーの ID
    public UuidV7 AccountId { get; private set; }

    public Nickname Nickname { get; private set; }

    public DateOnly JoinedOn { get; private set; }
    // ...
}
```

ユーザーと集約は同じ `DbContext` に置き、同じ Command で登録します。
`UnitOfWorkBehavior` がまとめて保存するため、どちらか一方だけが保存されることはありません。

```csharp
public async Task<Result<RegisteredAccount>> HandleAsync(RegisterAccountCommand request, CancellationToken cancellationToken)
{
    // 失敗を Result で返しても UnitOfWork は保存するため、Identity のユーザーを作る前に会員の入力を検証する
    Result<Nickname> nickname = Nickname.TryCreate(request.Nickname);
    if (nickname.IsFailure)
    {
        return Result<RegisteredAccount>.Failure(nickname.Errors);
    }

    Result<RegisteredAccount> account = await accounts.RegisterAsync(request.UserName, request.EmailAddress, request.Password);
    if (account.IsSuccess)
    {
        members.Add(Member.TryRegister(account.Value!.AccountId, request.Nickname, timeProvider.GetLocalToday()).GetValueOrThrow());
    }
    return account;
}
```

`accounts` はアプリケーションが定義したインターフェースで、インフラストラクチャ層で `UserManager<TUser>` を使って実装します。

> [!WARNING]
> `UnitOfWorkBehavior` は、Handler が失敗の `Result` を返した場合も変更を保存します。
> ユーザーを作った後に失敗を `Result` で返すと、ユーザーだけが保存されます。
> 入力の検証はユーザーを作る前に済ませ、ユーザーを作った後の失敗は例外にしてください（例外の場合は変更が破棄されます）。

## DbContext

Identity のテーブルを持つ `DbContext` は、`IdentityDbContext`（ロールを使う場合）または `IdentityUserContext`（ロールを使わない場合）を継承します。
そのため `D3DbContext` は継承できません。
代わりに、`ConfigureConventions` で `ApplyD3FragmentsConventions` を呼び出します。

```csharp
public sealed class IdentityIntegrationDbContext(DbContextOptions<IdentityIntegrationDbContext> options)
    : IdentityUserContext<UserAccount, UuidV7>(options)
{
    public DbSet<Member> Members => Set<Member>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyD3FragmentsConventions(options);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity のテーブルの定義
        base.OnModelCreating(builder);

        builder.Entity<Member>(member =>
        {
            member.ComplexValueObject(x => x.Nickname);
            member.HasIndex(x => x.AccountId).IsUnique();
        });
    }
}
```

`ApplyD3FragmentsConventions` は `D3DbContext` と同じ規約（`UuidV7` と型安全な列挙型の値コンバーター、`Version` の同時実行トークン、値オブジェクトのマッピング漏れの検出）を適用します。
保存時の `UpdatedAt`・`Version` の更新は、`AddD3FragmentsEFCore`（内部で `UseD3Fragments` を呼ぶ）が登録するインターセプターが行います。
`UseD3Fragments` で構成したコンテキストで `ApplyD3FragmentsConventions` を呼び忘れると、保存時に `InvalidOperationException` がスローされます。
詳しくは [DbContext とマッピング](../05-efcore/01-dbcontext.md) を参照してください。

## サービスの登録

インフラストラクチャ層では、`AddIdentity` ではなく `AddIdentityCore` を使い、`AddEntityFrameworkStores` の代わりに `AddD3FragmentsStores` を呼び出します。

```csharp
services.AddD3FragmentsEFCore<IdentityIntegrationDbContext>(
    [typeof(IdentityIntegrationDbContext).Assembly],
    options => options.UseSqlite(connectionString));

services.AddIdentityCore<UserAccount>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
        // パスキーのテーブル（AspNetUserPasskeys）を作る
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddSignInManager()
    .AddDefaultTokenProviders()
    // 保存を UnitOfWork に任せるストア
    .AddD3FragmentsStores<IdentityIntegrationDbContext>();
```

プレゼンテーション層では Identity の Cookie を登録します。

```csharp
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
```

`AddIdentityCookies` は、アプリケーション・外部ログイン・二要素認証待ち・「このブラウザを記憶する」の4つの Cookie を登録します。
`AddIdentity` は Cookie もまとめて登録するため、Cookie をプレゼンテーション層で登録できるよう `AddIdentityCore` を使います。

### AddD3FragmentsStores

`AddD3FragmentsStores<TContext>` は、保存を Unit of Work に任せるユーザーストア（ロールを使う場合はロールストアも）を登録します。
`UserManager<TUser>` の操作は追跡中のエンティティを変更するだけで、変更は Command の `UnitOfWorkBehavior` が、ドメインの変更とまとめて保存します。

| 項目 | 内容 |
|---|---|
| 登録するストア | ロールを使う場合は `D3UserStore` と `D3RoleStore`、使わない場合は `D3UserOnlyStore` |
| ストアの型引数 | `AddEntityFrameworkStores` と同じ方法で、`TContext` の基底型（`IdentityDbContext` または `IdentityUserContext`）から決める |
| 有効期間 | Scoped |
| 登録済みのストア | 置き換える（`AddEntityFrameworkStores` で登録したものなど） |
| `TContext` の登録 | 行わない。`AddD3FragmentsEFCore` で登録する |

ほかの D3Fragments の登録メソッドと違い、`AddD3FragmentsStores` は登録済みのストアを置き換えます。
ユーザーの型が `IdentityUser<TKey>` を、ロールの型が `IdentityRole<TKey>` を継承していない場合は `InvalidOperationException` をスローします。

パスキーを使う場合は、`IdentitySchemaVersions.Version3` を設定してください。
設定しないとパスキーのテーブルが作られず、初めてパスキーを使った時点で例外になります。

## IdentityResult を Result に変換する

`UserManager<TUser>` の操作は `IdentityResult` を返します。
`ToResult` で D3Fragments の `Result` に変換すると、ほかの Command と同じ方法でエラーを扱えます。

```csharp
Result created = (await users.CreateAsync(account, password)).ToResult();
if (created.IsFailure)
{
    return Result<RegisteredAccount>.Failure(created.Errors);
}
```

各 `IdentityError` は、`Description` をメッセージ、`Code` をコードとする `Error` になります。
エラーの種類は次のとおりです。

| `IdentityError.Code` | `ErrorType` |
|---|---|
| `DuplicateUserName`、`DuplicateEmail`、`DuplicateRoleName`、`LoginAlreadyAssociated`、`ConcurrencyFailure` | `Conflict` |
| `UserAlreadyHasPassword`、`UserAlreadyInRole`、`UserNotInRole`、`UserLockoutNotEnabled` | `InvalidState` |
| 上記以外 | `Validation` |

別の対応づけにする場合は、`ToResult(map)` に変換する関数を渡します。

```csharp
return (await users.ConfirmEmailAsync(account, token))
    .ToResult(_ => Error.Validation("確認用のリンクが正しくないか、期限が切れています。"));
```

## 関連する API

- @D3Fragments.Identity.EFCore.D3FragmentsIdentityBuilderExtensions
- @D3Fragments.Identity.EFCore.IdentityResultExtensions
- @D3Fragments.EFCore.D3FragmentsModelConfigurationBuilderExtensions
