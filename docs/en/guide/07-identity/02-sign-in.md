# Sign-in and sign-out

## Basic flow

Call the `SignInManager<TUser>` sign-in methods split into the database side and the cookie side.

1. In the presentation layer, read the cookie state that the Command needs (whether this browser is remembered).
2. In the Command, perform the database side (verifying the password, recording failed sign-in attempts, redeeming recovery codes, and so on) with the `Verify*` extension methods. `UnitOfWorkBehavior` saves the changes.
3. After the Command succeeds, issue cookies in the presentation layer with the cookie-side extension methods.

The `SignInManager<TUser>` methods and the methods to use instead are as follows.

| `SignInManager<TUser>` method | In the Command (database side) | Presentation layer (cookies and other HTTP side) |
|---|---|---|
| `PasswordSignInAsync` | `VerifyPasswordAsync` | `CompleteSignInAsync` / `BeginTwoFactorSignInAsync` |
| `TwoFactorSignInAsync`, `TwoFactorAuthenticatorSignInAsync` | `VerifyTwoFactorCodeAsync` | `CompleteTwoFactorSignInAsync` |
| `TwoFactorRecoveryCodeSignInAsync` | `VerifyRecoveryCodeAsync` | `CompleteTwoFactorSignInAsync` |
| `PasskeySignInAsync` | `VerifyPasskeyAsync` | `PerformPasskeyAssertionAsync` (before the Command), `CompleteSignInAsync` |
| `ExternalLoginSignInAsync` | `VerifyExternalLoginAsync` | `CompleteSignInAsync` / `BeginTwoFactorSignInAsync` |

Calling the methods in the left column triggers a [DFR0017](../../analyzers/DFR0017.md) warning.

The `Verify*` methods follow the same steps as the corresponding `SignInManager<TUser>` methods and return the same `SignInResult`.
Return sign-in failures as results, not as exceptions.
When a Command ends with an exception, the Unit of Work discards the changes, so failed sign-in attempts are not saved and lockout does not work.

The `SignInManager<TUser>` methods that do not change the database may be called directly in the presentation layer.
These are `GetTwoFactorAuthenticationUserAsync`, `ForgetTwoFactorClientAsync`, `RefreshSignInAsync`, `SignOutAsync`, and the passkey ceremony methods (`MakePasskeyRequestOptionsAsync`, `PerformPasskeyAssertionAsync`, and so on).

## The Command's outcome type

Return the sign-in outcome as a type defined in the application layer, because the application layer does not depend on the Identity `SignInResult`.

```csharp
public enum SignInStatus
{
    Succeeded,
    RequiresTwoFactor,
    LockedOut,
    NotAllowed,
    Failed,
    // No account is linked to the external login
    NotRegistered,
}

public sealed record SignInOutcome(SignInStatus Status, UuidV7? AccountId = null);
```

The infrastructure layer implementation converts `SignInResult` into this type.

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

## Password

In the infrastructure layer, find the user and call `VerifyPasswordAsync`.
The last argument tells whether this browser is remembered for the user.
If it is remembered, two-factor authentication is not required.

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

In the presentation layer, read the ID of the remembered user with `GetTwoFactorClientRememberedUserIdAsync` before the Command, and issue cookies according to the Command's outcome.

```csharp
public async Task<IActionResult> OnPostAsync(string? returnUrl)
{
    // 1. Read the "remember this browser" account from the cookie
    string? rememberedAccountId = await SignInManager.GetTwoFactorClientRememberedUserIdAsync();

    // 2. Verify the password (the UnitOfWork saves failed attempts and lockout)
    SignInOutcome outcome = await signInWithPassword.ExecuteAsync(
        new SignInWithPasswordCommand(Input.UserName.Trim(), Input.Password, rememberedAccountId), HttpContext.RequestAborted);

    // 3. Issue cookies
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

| Method | When to call | Cookie issued |
|---|---|---|
| `CompleteSignInAsync` | Sign-in succeeded without two-factor authentication | The application cookie (the `amr` claim is `pwd`) |
| `BeginTwoFactorSignInAsync` | Two-factor authentication is required | The two-factor pending cookie (records the user's ID) |

## Two-factor authentication

On the two-factor authentication page, get the user waiting for two-factor authentication with `GetTwoFactorAuthenticationUserAsync`, and verify that user's code in a Command.

In the infrastructure layer, pass the token provider name and the code to `VerifyTwoFactorCodeAsync`.
Wrong codes count toward lockout.

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

In the presentation layer, call `CompleteTwoFactorSignInAsync` after the Command succeeds.

```csharp
public async Task<IActionResult> OnPostAsync()
{
    // The account waiting for two-factor authentication (read from the cookie issued by BeginTwoFactorSignInAsync)
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

    // Remove the two-factor pending cookie, sign in, and remember this browser if requested
    await SignInManager.CompleteTwoFactorSignInAsync(account, RememberMe, RememberMachine);
    return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/MyPage");
}
```

`CompleteTwoFactorSignInAsync` does the following.

- Issues the application cookie (the `amr` claim is `mfa`; if the first factor was an external login, that provider is also added as the authentication method claim)
- Removes the two-factor pending cookie and the external login cookie
- Issues the "remember this browser" cookie when `rememberClient` is `true`

It throws an `InvalidOperationException` if `user` differs from the user waiting for two-factor authentication, or if no user is waiting for two-factor authentication.
This prevents signing in a different user.

### Sending a code by email

`UserManager<TUser>.GenerateTwoFactorTokenAsync`, which creates the email code, does not change the database.
Creating the code and sending the email can be done in one Command.

```csharp
public async Task<Result> HandleAsync(SendTwoFactorEmailCodeCommand request, CancellationToken cancellationToken)
{
    EmailCode code = await authenticator.GenerateEmailTwoFactorCodeAsync(request.AccountId);
    await sender.SendTwoFactorCodeAsync(code.EmailAddress, code.Code, cancellationToken);
    return Result.Success();
}
```

Identity does not limit how many times a code can be sent.
In a public application, limit it with ASP.NET Core rate limiting or similar.

### Recovery codes

Verify recovery codes with `VerifyRecoveryCodeAsync`.
A used recovery code can no longer be used once the Command's changes are saved.

Issue cookies with `CompleteTwoFactorSignInAsync`.
To behave the same as `SignInManager<TUser>.TwoFactorRecoveryCodeSignInAsync`, pass `false` for `isPersistent` and `rememberClient`.

```csharp
await SignInManager.CompleteTwoFactorSignInAsync(account, isPersistent: false, rememberClient: false);
```

## Passkeys

Verifying the passkey signature and origin uses the HttpContext, so do it in the presentation layer with `PerformPasskeyAssertionAsync`.
This verification does not change the database.
Pass the verification result (the passkey ID, the new signature count, and the backup state) to the Command, and save it with `VerifyPasskeyAsync`.

```csharp
// Presentation layer: verify the signature and origin (does not change the database)
PasskeyAssertionResult<UserAccount> assertion = await SignInManager.PerformPasskeyAssertionAsync(credentialJson);
if (!assertion.Succeeded)
{
    ModelState.AddModelError(string.Empty, $"Passkey authentication failed. ({assertion.Failure.Message})");
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
// Infrastructure layer: mark the signature count and backup state to be saved
SignInResult result = await signIn.VerifyPasskeyAsync(account, credentialId, signCount, isBackedUp);
```

Like `PasskeySignInAsync`, `VerifyPasskeyAsync` does not require two-factor authentication (it treats a passkey as sufficiently strong authentication).
It does not change the failed sign-in count either.
If your application requires two-factor authentication, decide it in the Command.

Split passkey registration in the same way.
Verify with `PerformPasskeyAttestationAsync` (presentation layer), pass the result to a Command, and save it with `UserManager<TUser>.AddOrUpdatePasskeyAsync`.

## External login

When returning from the external identity provider, read the authentication result with `GetExternalLoginInfoAsync` (it does not change the database).
In the infrastructure layer, find the user with `FindByLoginAsync` and call `VerifyExternalLoginAsync`.

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

In the presentation layer, pass the provider name to the cookie-side extension methods.
The provider name becomes the authentication method claim of the application cookie.

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

`CompleteSignInAsync` with a `loginProvider` also removes the external login cookie.

If no user is linked to the external login (the first sign-in), create the user (`CreateAsync`) and link the external login (`AddLoginAsync`) in one Command.
To save the authentication tokens of the external identity provider, call `UpdateExternalAuthenticationTokensAsync` in the same Command (it only changes the database).

## Sign-out

Sign-out only removes cookies and does not change the database.
Call `SignOutAsync` in the presentation layer without a Command.

```csharp
public async Task<IActionResult> OnPostAsync()
{
    await SignInManager.SignOutAsync();
    return RedirectToPage("/Index");
}
```

## Related APIs

- @D3Fragments.Identity.EFCore.D3SignInManagerVerificationExtensions
- @D3Fragments.AspNetCore.Identity.D3SignInManagerSessionExtensions
