# 値オブジェクト

値オブジェクトは書名やメールアドレスのような値を表す不変のオブジェクトです。
D3Fragments の値オブジェクトは、プリミティブ型の値を1つラップし、生成時に値を検証します。

## 値オブジェクトを定義する

`ValueObjectBase<TSelf, TPrimitive>` を継承し、`IValueObject<TSelf, TPrimitive>` を実装します。
`TSelf` には定義するクラス自身を、`TPrimitive` にはラップする型を指定します。

```csharp
using System.Diagnostics.CodeAnalysis;
using D3Fragments;

public sealed class EmailAddress : ValueObjectBase<EmailAddress, string>, IValueObject<EmailAddress, string>
{
    private EmailAddress(string value) : base(value)
    {
    }

    public static EmailAddress Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "メールアドレスを指定してください。";
            return false;
        }

        if (!System.Net.Mail.MailAddress.TryCreate(value, out _))
        {
            errorMessage = "メールアドレスの形式が不正です。";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
```

実装するメンバーは次の2つです。

| メンバー | 内容 |
|---|---|
| `static TSelf Create(TPrimitive value)` | プリミティブ値から値オブジェクトを生成する。正規化（前後の空白の除去など）はここで行う |
| `static bool ValidateValue(TPrimitive value, out string? errorMessage)` | 値を検証する。不正な場合は `false` を返し、`errorMessage` に理由を設定する |

基底クラスのコンストラクタが `ValidateValue` を呼びます。
不正な値を渡すと `DomainValidationException` がスローされるため、不正な状態の値オブジェクトは存在しません。

> [!NOTE]
> C# では、基底クラスから派生クラスに静的メソッドの実装を義務付けることができません。
> そのため、`Create` と `ValidateValue` の実装を求める `IValueObject` も宣言します。
> `IValueObject` の宣言を忘れると、コンパイルエラーになります。

## 値オブジェクトを生成する

値オブジェクトの生成方法は2つあります。

```csharp
// 不正な値なら DomainValidationException をスローする
var email = EmailAddress.Create("user@example.com");

// 不正な値なら失敗の Result を返す
Result<EmailAddress> result = EmailAddress.TryCreate(input);
if (result.IsFailure)
{
    // result.Errors に検証エラーが入っている
}
```

`TryCreate` は基底クラスが提供します。
複数の値をまとめて検証し、エラーをすべて返したい場合に使います。

```csharp
public static Result<Book> TryCreate(string isbn, string title, string author)
{
    Result<Isbn> isbnResult = Isbn.TryCreate(isbn);
    Result<BookTitle> titleResult = BookTitle.TryCreate(title);
    if (isbnResult.IsFailure || titleResult.IsFailure)
    {
        return Result<Book>.Failure(Result.CombineErrors(isbnResult, titleResult));
    }

    return Result<Book>.Success(new Book(isbnResult.Value, titleResult.Value, author, BookStatus.Available));
}
```

## 等価性と文字列表現

ラップしている値が等しい値オブジェクトは、等しいと判定されます（`Equals` と `==`）。
`ToString()` は、ラップしている値の文字列表現を返します。

## 永続化と JSON

値オブジェクトは次のように外部の表現へ変換できます。

- EF Core：`ComplexValueObject` で、プロパティ名の列1つにマッピングします。詳しくは [EF Core 連携](../05-efcore/01-dbcontext.md) を参照してください。
- JSON：`ValueObjectJsonConverterFactory` を使うと、ラップしている値そのもの（`"user@example.com"` など）として読み書きします。`AddD3FragmentsAspNetCore` を呼ぶと、ASP.NET Core に自動で登録されます。JSON から読み込むときも `ValidateValue` で検証します。

## 関連する API

- <xref:D3Fragments.ValueObjectBase`2>
- <xref:D3Fragments.IValueObject`2>
- @D3Fragments.ValueObjectJsonConverterFactory
- @D3Fragments.DomainValidationException
