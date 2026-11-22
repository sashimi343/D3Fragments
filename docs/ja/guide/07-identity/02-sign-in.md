# ログインとログアウト

## 基本の流れ

`SignInManager<TUser>` のサインインのメソッドは、データベース側の処理と Cookie 側の処理に分けて呼び出します。

1. プレゼンテーション層で、Command に必要な Cookie の状態（このブラウザを記憶しているか）を読み取る。
2. Command の中で、`Verify*` 拡張メソッドによりデータベース側の処理（パスワードの検証、ログイン失敗回数の記録、回復コードの使用など）を行う。変更は `UnitOfWorkBehavior` が保存する。
3. Command が成功した後に、プレゼンテーション層で Cookie 側の拡張メソッドにより Cookie を発行する。

`SignInManager<TUser>` のメソッドと、代わりに使うメソッドの対応は次のとおりです。

| `SignInManager<TUser>` のメソッド | Command の中（データベース側） | プレゼンテーション層（Cookie など HTTP 側） |
|---|---|---|
| `PasswordSignInAsync` | `VerifyPasswordAsync` | `CompleteSignInAsync` / `BeginTwoFactorSignInAsync` |
| `TwoFactorSignInAsync`、`TwoFactorAuthenticatorSignInAsync` | `VerifyTwoFactorCodeAsync` | `CompleteTwoFactorSignInAsync` |
| `TwoFactorRecoveryCodeSignInAsync` | `VerifyRecoveryCodeAsync` | `CompleteTwoFactorSignInAsync` |
| `PasskeySignInAsync` | `VerifyPasskeyAsync` | `PerformPasskeyAssertionAsync`（Command の前）、`CompleteSignInAsync` |
| `ExternalLoginSignInAsync` | `VerifyExternalLoginAsync` | `CompleteSignInAsync` / `BeginTwoFactorSignInAsync` |

左の列のメソッドを呼び出すと、[DFR0017](../../analyzers/DFR0017.md) が警告します。

`Verify*` は、対応する `SignInManager<TUser>` のメソッドと同じ手順で処理し、同じ `SignInResult` を返します。
サインインの失敗は例外ではなく結果として返してください。
Command が例外で終わると Unit of Work は変更を破棄するため、ログイン失敗回数が保存されず、ロックアウトが働きません。

`SignInManager<TUser>` のうち、データベースを変更しないメソッドは、プレゼンテーション層で直接呼び出してかまいません。
`GetTwoFactorAuthenticationUserAsync`、`ForgetTwoFactorClientAsync`、`RefreshSignInAsync`、`SignOutAsync`、パスキーの手続きのメソッド（`MakePasskeyRequestOptionsAsync`、`PerformPasskeyAssertionAsync` など）がこれにあたります。

## Command の結果の型

サインインの結果はアプリケーション層で定義した型で返します。
アプリケーション層は Identity の `SignInResult` に依存しないためです。

```csharp
public enum SignInStatus
{
    Succeeded,
    RequiresTwoFactor,
    LockedOut,
    NotAllowed,
    Failed,
    // 外部ログインに紐づくアカウントがない
    NotRegistered,
}

public sealed record SignInOutcome(SignInStatus Status, UuidV7? AccountId = null);
```

インフラストラクチャ層の実装で、`SignInResult` をこの型に変換します。

```csharp
private static SignInOutcome ToOutcome(SignInResult result, UserAccount account)
{
    SignInStatus status = result switch
    {
        { Succeeded: true } => SignInStatus.Succeeded,
        { RequiresTwoFactor: true } => SignInStatus.RequiresTwoFactor,
        { IsLockedOut: true } => SignInStatus.LockedOut,
        { IsNotAllowed: true } => SignInStatus.NotAllowed,
        _ => SignInStatus.Failed,
    };
    return new SignInOutcome(status, account.Id);
}
```

## パスワード

インフラストラクチャ層ではユーザーを探して `VerifyPasswordAsync` を呼び出します。
最後の引数にはこのブラウザを記憶しているユーザーかどうかを渡します。
記憶している場合は、二要素認証を求めません。

```csharp
public async Task<SignInOutcome> SignInWithPasswordAsync(string userName, string password, string? rememberedAccountId)
{
    if (await users.FindByNameAsync(userName) is not { } account)
    {
        return SignInOutcome.Failed;
    }
    SignInResult result = await signIn.VerifyPasswordAsync(
        account, password, lockoutOnFailure: true, isTwoFactorClientRemembered: rememberedAccountId == account.Id.ToString());
    return ToOutcome(result, account);
}
```

プレゼンテーション層では、Command の前に `GetTwoFactorClientRememberedUserIdAsync` で記憶しているユーザーの ID を読み取り、Command の結果に応じて Cookie を発行します。

```csharp
public async Task<IActionResult> OnPostAsync(string? returnUrl)
{
    // 1. Cookie から「このブラウザを記憶する」のアカウントを読み取る
    string? rememberedAccountId = await SignInManager.GetTwoFactorClientRememberedUserIdAsync();

    // 2. パスワードを検証する（ログイン失敗回数とロックアウトは UnitOfWork が保存する）
    SignInOutcome outcome = await signInWithPassword.ExecuteAsync(
        new SignInWithPasswordCommand(Input.UserName.Trim(), Input.Password, rememberedAccountId), HttpContext.RequestAborted);

    // 3. Cookie を発行する
    switch (outcome.Status)
    {
        case SignInStatus.Succeeded:
            await SignInManager.CompleteSignInAsync(await FindAccountAsync(outcome.AccountId!.Value), Input.RememberMe);
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/MyPage");
        case SignInStatus.RequiresTwoFactor:
            await SignInManager.BeginTwoFactorSignInAsync(await FindAccountAsync(outcome.AccountId!.Value));
            return RedirectToPage("/Account/LoginWith2fa", new { rememberMe = Input.RememberMe, returnUrl });
        default:
            AddSignInError(outcome);
            return Page();
    }
}
```

| メソッド | 呼び出す場面 | 発行する Cookie |
|---|---|---|
| `CompleteSignInAsync` | 二要素認証なしでサインインが成功した | アプリケーションの Cookie（`amr` クレームは `pwd`） |
| `BeginTwoFactorSignInAsync` | 二要素認証が必要と返された | 二要素認証待ちの Cookie（ユーザーの ID を記録する） |

## 二要素認証

二要素認証の画面では、`GetTwoFactorAuthenticationUserAsync` で二要素認証を待っているユーザーを取得し、そのユーザーのコードを Command で検証します。

インフラストラクチャ層では `VerifyTwoFactorCodeAsync` にトークンプロバイダーの名前とコードを渡します。
コードの誤りはロックアウトの回数に数えられます。

```csharp
public async Task<SignInOutcome> SignInWithTwoFactorCodeAsync(UuidV7 accountId, TwoFactorMethod method, string code)
{
    UserAccount account = await GetAsync(accountId);
    string provider = method == TwoFactorMethod.Authenticator
        ? users.Options.Tokens.AuthenticatorTokenProvider
        : TokenOptions.DefaultEmailProvider;
    SignInResult result = await signIn.VerifyTwoFactorCodeAsync(account, provider, code);
    return ToOutcome(result, account);
}
```

プレゼンテーション層では Command が成功した後に `CompleteTwoFactorSignInAsync` を呼び出します。

```csharp
public async Task<IActionResult> OnPostAsync()
{
    // 二要素認証を待っているアカウント（BeginTwoFactorSignInAsync が発行した Cookie から読み取る）
    if (await SignInManager.GetTwoFactorAuthenticationUserAsync() is not { } account)
    {
        return RedirectToPage("/Account/Login");
    }

    SignInOutcome outcome = await signInWithCode.ExecuteAsync(
        new SignInWithTwoFactorCodeCommand(account.Id, Method, Code), HttpContext.RequestAborted);
    if (outcome.Status != SignInStatus.Succeeded)
    {
        AddSignInError(outcome);
        return Page();
    }

    // 二要素認証待ちの Cookie を消してサインインし、選んだ場合はこのブラウザを記憶する
    await SignInManager.CompleteTwoFactorSignInAsync(account, RememberMe, RememberMachine);
    return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/MyPage");
}
```

`CompleteTwoFactorSignInAsync` は、次のことを行います。

- アプリケーションの Cookie を発行する（`amr` クレームは `mfa`。1要素目が外部ログインの場合は、そのプロバイダーも認証方法のクレームに追加する）
- 二要素認証待ちの Cookie と外部ログインの Cookie を削除する
- `rememberClient` が `true` の場合は、「このブラウザを記憶する」の Cookie を発行する

`user` が二要素認証を待っているユーザーと異なる場合や、二要素認証を待っているユーザーがいない場合は、`InvalidOperationException` をスローします。
別のユーザーをサインインさせないためです。

### メールでコードを送る

メールのコードを作る `UserManager<TUser>.GenerateTwoFactorTokenAsync` は、データベースを変更しません。
コードの作成とメールの送信を、1つの Command で行えます。

```csharp
public async Task<Result> HandleAsync(SendTwoFactorEmailCodeCommand request, CancellationToken cancellationToken)
{
    EmailCode code = await authenticator.GenerateEmailTwoFactorCodeAsync(request.AccountId);
    await sender.SendTwoFactorCodeAsync(code.EmailAddress, code.Code, cancellationToken);
    return Result.Success();
}
```

Identity はコードを送る回数を制限しません。
公開するアプリケーションでは ASP.NET Core のレート制限などで回数を制限してください。

### 回復コード

回復コードは `VerifyRecoveryCodeAsync` で検証します。
使った回復コードは、Command の変更が保存されると、次から使えなくなります。

Cookie は `CompleteTwoFactorSignInAsync` で発行します。
`SignInManager<TUser>.TwoFactorRecoveryCodeSignInAsync` と同じにする場合は、`isPersistent` と `rememberClient` に `false` を渡します。

```csharp
await SignInManager.CompleteTwoFactorSignInAsync(account, isPersistent: false, rememberClient: false);
```

## パスキー

パスキーの署名とオリジンの検証は、HttpContext を使うため、プレゼンテーション層で `PerformPasskeyAssertionAsync` により行います。
この検証はデータベースを変更しません。
検証の結果（パスキーの ID、新しい署名回数、バックアップの状態）を Command に渡し、`VerifyPasskeyAsync` で保存します。

```csharp
// プレゼンテーション層：署名とオリジンの検証（DB は変更しない）
PasskeyAssertionResult<UserAccount> assertion = await SignInManager.PerformPasskeyAssertionAsync(credentialJson);
if (!assertion.Succeeded)
{
    ModelState.AddModelError(string.Empty, $"パスキーでの認証に失敗しました。（{assertion.Failure.Message}）");
    return Page();
}

SignInOutcome outcome = await signInWithPasskey.ExecuteAsync(
    new SignInWithPasskeyCommand(assertion.User.Id, assertion.Passkey.CredentialId, assertion.Passkey.SignCount, assertion.Passkey.IsBackedUp),
    HttpContext.RequestAborted);
if (outcome.Status != SignInStatus.Succeeded)
{
    AddSignInError(outcome);
    return Page();
}

await SignInManager.CompleteSignInAsync(assertion.User, isPersistent: false);
```

```csharp
// インフラストラクチャ層：署名回数とバックアップの状態を保存対象にする
SignInResult result = await signIn.VerifyPasskeyAsync(account, credentialId, signCount, isBackedUp);
```

`VerifyPasskeyAsync` は、`PasskeySignInAsync` と同じく二要素認証を求めません（パスキーを十分に強い認証とみなします）。
ログイン失敗回数も変更しません。
二要素認証が必要なアプリケーションでは Command で判定してください。

パスキーの登録も同じように分けます。
`PerformPasskeyAttestationAsync` で検証し（プレゼンテーション層）、その結果を Command に渡して `UserManager<TUser>.AddOrUpdatePasskeyAsync` で保存します。

## 外部ログイン

外部の ID プロバイダーからの戻りでは、`GetExternalLoginInfoAsync` で認証結果を読み取ります（データベースは変更しません）。
インフラストラクチャ層では `FindByLoginAsync` でユーザーを探して `VerifyExternalLoginAsync` を呼び出します。

```csharp
public async Task<SignInOutcome> SignInWithExternalLoginAsync(string loginProvider, string providerKey, string? rememberedAccountId)
{
    if (await users.FindByLoginAsync(loginProvider, providerKey) is not { } account)
    {
        return new SignInOutcome(SignInStatus.NotRegistered);
    }
    SignInResult result = await signIn.VerifyExternalLoginAsync(
        account, isTwoFactorClientRemembered: rememberedAccountId == account.Id.ToString());
    return ToOutcome(result, account);
}
```

プレゼンテーション層では Cookie 側の拡張メソッドにプロバイダーの名前を渡します。
プロバイダーの名前は、アプリケーションの Cookie の認証方法のクレームになります。

```csharp
switch (outcome.Status)
{
    case SignInStatus.Succeeded:
        await SignInManager.CompleteSignInAsync(await FindAccountAsync(outcome.AccountId!.Value), isPersistent: false, info.LoginProvider);
        return LocalRedirect("/MyPage");
    case SignInStatus.RequiresTwoFactor:
        await SignInManager.BeginTwoFactorSignInAsync(await FindAccountAsync(outcome.AccountId!.Value), info.LoginProvider);
        return RedirectToPage("/Account/LoginWith2fa");
    // ...
}
```

`loginProvider` を渡した `CompleteSignInAsync` は、外部ログインの Cookie も削除します。

外部ログインに紐づくユーザーがいない場合（初めてのログイン）は、ユーザーの作成（`CreateAsync`）と外部ログインの紐づけ（`AddLoginAsync`）を1つの Command で行います。
外部の ID プロバイダーの認証トークンを保存する場合は、同じ Command で `UpdateExternalAuthenticationTokensAsync` を呼び出します（データベースの変更だけを行うメソッドです）。

## ログアウト

ログアウトは Cookie を削除するだけで、データベースを変更しません。
Command を使わず、プレゼンテーション層で `SignOutAsync` を呼び出します。

```csharp
public async Task<IActionResult> OnPostAsync()
{
    await SignInManager.SignOutAsync();
    return RedirectToPage("/Index");
}
```

## 関連する API

- @D3Fragments.Identity.EFCore.D3SignInManagerVerificationExtensions
- @D3Fragments.AspNetCore.Identity.D3SignInManagerSessionExtensions
