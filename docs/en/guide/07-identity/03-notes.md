# Notes

## Write with UserManager only inside a Command

The stores registered by `AddD3FragmentsStores` do not save changes by themselves.
If you call a `UserManager<TUser>` write method (`CreateAsync`, `SetTwoFactorEnabledAsync`, `AddOrUpdatePasskeyAsync`, and so on) outside a Command, the changes are lost without any error.
Perform writes inside a Command (in the infrastructure layer implementation of an interface defined by the application).

In the presentation layer, use `UserManager<TUser>` only for reading (getting the signed-in user, showing the two-factor authentication settings, and so on).

`UserManager<TUser>` writes are not saved in after-commit domain event handlers either, because changes made by after-commit handlers are discarded (see [Domain event handlers](../04-application/03-domain-event-handlers.md)).
To change a user after the commit, execute another Command from the handler.

## Security stamps and re-issuing cookies

Identity changes the user's security stamp on security-related operations such as changing the password.
The application cookie is validated against the security stamp periodically (every 30 minutes by default), so once the stamp changes, the user's sessions are signed out at the next validation.

When a signed-in user performs an operation that changes their own stamp, the session that performed the operation is also signed out unless you do something.
To keep the session, re-issue the cookie with `RefreshSignInAsync` after the Command succeeds.

```csharp
public async Task<IActionResult> OnPostEnableEmailAsync()
{
    UserAccount account = await GetCurrentAccountAsync();
    IReadOnlyList<string> codes = await enableEmail.ExecuteAsync(new EnableEmailTwoFactorCommand(account.Id), HttpContext.RequestAborted);
    // Enabling two-factor authentication changes the security stamp, so re-issue our own cookie
    await SignInManager.RefreshSignInAsync(account);
    return ShowRecoveryCodes(codes, "Two-factor authentication has been enabled.");
}
```

Whether each `UserManager<TUser>` method changes the security stamp is as follows (for .NET 10).

| Security stamp | Methods |
|---|---|
| Changed | `SetTwoFactorEnabledAsync` (both enabling and disabling), `ResetAuthenticatorKeyAsync`, password-related methods such as `ChangePasswordAsync`, `SetUserNameAsync`, `SetEmailAsync`, `ChangeEmailAsync`, `SetPhoneNumberAsync`, `ChangePhoneNumberAsync`, `RemoveLoginAsync`, `UpdateSecurityStampAsync` |
| Not changed | Generating and redeeming recovery codes, adding, updating, and removing passkeys, adding and removing roles, adding claims, lockout-related methods, saving the authentication tokens of external identity providers |

Which operations keep the session and which ones sign out other sessions is up to the application.

> [!NOTE]
> Removing a passkey does not change the security stamp, so other sessions signed in with that passkey are not signed out.
> To also sign out other sessions when removing a leaked passkey, call `UpdateSecurityStampAsync` in the same Command.

## Differences caused by leaving saving to the Unit of Work

Compared with `AddEntityFrameworkStores`, the following points differ.

- Duplicate user names: `UserManager<TUser>` checks for duplicates against saved users. Duplicates among users created in the same unit of saving cannot be detected; they cause a `DbUpdateException` from the database unique index on save. `DbUpdateException` is not a `DomainException`, so it is not converted into an error response.
- Concurrency conflicts: a conflict on the user's `ConcurrencyStamp` is reported by the Unit of Work as a `ConflictOccurredException`, not as an `IdentityResult` (`ConcurrencyFailure`).

## Identity behavior

The following points are Identity behavior, not D3Fragments behavior.
If they do not fit your application's requirements, handle them in the application.

- Time: lockout checks and expiration, token expiration (such as email confirmation links), and authenticator app codes use the system clock, not `TimeProvider`. Replacing `TimeProvider` does not change these times.
- Expiration of the two-factor pending cookie: the two-factor pending cookie (`IdentityConstants.TwoFactorUserIdScheme`) expires in 5 minutes, and sliding expiration is enabled. Because of this, it can stay valid for more than 5 minutes while the user keeps entering codes.
- Recovery codes and lockout: like `TwoFactorRecoveryCodeSignInAsync`, `VerifyRecoveryCodeAsync` does not check whether the user can sign in or is locked out, and wrong codes do not count toward lockout. Check them in the Command if needed.
- Number of email codes sent: Identity does not limit how many times a two-factor code can be sent (see [Sign-in and sign-out](02-sign-in.md#sending-a-code-by-email)).
- Passkeys and two-factor authentication: signing in with a passkey does not require two-factor authentication (see [Sign-in and sign-out](02-sign-in.md#passkeys)).
