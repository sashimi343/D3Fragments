using Microsoft.AspNetCore.Identity;

namespace IdentityIntegration.Infrastructure.Identity;

// 画面に表示する Identity のエラーメッセージ（登録で出るものだけ）
internal sealed class JapaneseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"ユーザー名「{userName}」は既に使われています。" };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"メールアドレス「{email}」は既に使われています。" };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "ユーザー名には半角英数字と「-._@+」だけを使えます。" };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "メールアドレスの形式が正しくありません。" };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"パスワードは{length}文字以上にしてください。" };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "パスワードには記号を1文字以上含めてください。" };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "パスワードには数字を1文字以上含めてください。" };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "パスワードには英小文字を1文字以上含めてください。" };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "パスワードには英大文字を1文字以上含めてください。" };
}
