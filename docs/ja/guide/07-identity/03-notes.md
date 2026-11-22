# 注意点

## UserManager の書き込みは Command の中で行う

`AddD3FragmentsStores` のストアは、自身では変更を保存しません。
`UserManager<TUser>` の書き込みのメソッド（`CreateAsync`、`SetTwoFactorEnabledAsync`、`AddOrUpdatePasskeyAsync` など）を Command の外で呼び出すと、エラーにならずに変更が失われます。
書き込みは Command の中（アプリケーションが定義したインターフェースのインフラストラクチャ層の実装）で行ってください。

プレゼンテーション層では `UserManager<TUser>` を読み取り（ログイン中のユーザーの取得、二要素認証の設定の表示など）だけに使います。

Commit 後のドメインイベントハンドラでも、`UserManager<TUser>` の書き込みは保存されません。
Commit 後のハンドラによる変更は破棄されるためです（[ドメインイベントハンドラ](../04-application/03-domain-event-handlers.md) を参照）。
Commit 後にユーザーを変更したい場合は、ハンドラから別の Command を実行します。

## security stamp と Cookie の作り直し

Identity はパスワードの変更などのセキュリティに関わる操作で、ユーザーの security stamp を変えます。
アプリケーションの Cookie は定期的に（既定では30分ごとに）security stamp で検証されるため、stamp が変わると、次の検証でそのユーザーのセッションはログアウトされます。

ログイン中のユーザーが自分の stamp を変える操作をした場合、そのままでは操作したセッションもログアウトされます。
セッションを続ける場合は、Command が成功した後に `RefreshSignInAsync` で Cookie を作り直します。

```csharp
public async Task<IActionResult> OnPostEnableEmailAsync()
{
    UserAccount account = await GetCurrentAccountAsync();
    IReadOnlyList<string> codes = await enableEmail.ExecuteAsync(new EnableEmailTwoFactorCommand(account.Id), HttpContext.RequestAborted);
    // 二要素認証の有効化で security stamp が変わるため、自分の Cookie を作り直す
    await SignInManager.RefreshSignInAsync(account);
    return ShowRecoveryCodes(codes, "二要素認証を有効にしました。");
}
```

`UserManager<TUser>` のメソッドのうち、security stamp を変えるかどうかは次のとおりです（.NET 10 の場合）。

| security stamp | メソッド |
|---|---|
| 変える | `SetTwoFactorEnabledAsync`（有効化・無効化の両方）、`ResetAuthenticatorKeyAsync`、`ChangePasswordAsync` などのパスワード関連のメソッド、`SetUserNameAsync`、`SetEmailAsync`、`ChangeEmailAsync`、`SetPhoneNumberAsync`、`ChangePhoneNumberAsync`、`RemoveLoginAsync`、`UpdateSecurityStampAsync` |
| 変えない | 回復コードの作成と使用、パスキーの追加・更新・削除、ロールの追加・削除、クレームの追加、ロックアウト関連、外部 ID プロバイダーの認証トークンの保存 |

どの操作でセッションを続け、どの操作でほかのセッションを切るかは、アプリケーションが決めます。

> [!NOTE]
> パスキーを削除しても security stamp は変わらないため、そのパスキーでログインしたほかのセッションは切れません。
> 漏れたパスキーを削除するときにほかのセッションも切る場合は、同じ Command で `UpdateSecurityStampAsync` を呼び出してください。

## 保存を Unit of Work に任せることによる違い

`AddEntityFrameworkStores` と比べて、次の点が異なります。

- ユーザー名の重複：`UserManager<TUser>` は保存済みのユーザーと比べて重複を確認します。同じ保存単位で作ったユーザー同士の重複は確認できず、保存時にデータベースの一意インデックスで `DbUpdateException` になります。`DbUpdateException` は `DomainException` ではないため、エラー応答には変換されません。
- 同時実行の競合：ユーザーの `ConcurrencyStamp` による競合は、`IdentityResult`（`ConcurrencyFailure`）ではなく、Unit of Work が `ConflictOccurredException` として報告します。

## Identity の仕様

次の点は D3Fragments ではなく Identity の仕様です。
アプリケーションの要件に合わない場合は、アプリケーションで対応してください。

- 時刻：ロックアウトの判定と解除、トークン（メールの確認用リンクなど）の期限、認証アプリのコードは、`TimeProvider` ではなくシステムの時刻を使います。`TimeProvider` を替えても、これらの時刻は変わりません。
- 二要素認証待ちの Cookie の期限：二要素認証待ちの Cookie（`IdentityConstants.TwoFactorUserIdScheme`）の有効期限は5分で、有効期限の延長が有効です。そのため、コードの入力を続けていると、5分を超えて有効なままになることがあります。
- 回復コードとロックアウト：`VerifyRecoveryCodeAsync` は `TwoFactorRecoveryCodeSignInAsync` と同じく、サインインできるか・ロックアウト中かを確認せず、コードの誤りもロックアウトの回数に数えません。必要な場合は Command で確認してください。
- メールのコードの送信回数：Identity は二要素認証のコードを送る回数を制限しません（[ログインとログアウト](02-sign-in.md#メールでコードを送る) を参照）。
- パスキーと二要素認証：パスキーでのログインでは、二要素認証を求めません（[ログインとログアウト](02-sign-in.md#パスキー) を参照）。
