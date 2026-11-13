# 入力値の検証

D3Fragments には、値に対する検証ルールを宣言して使うバリデータがあります。
検証の結果は `Result` で返り、違反したルールのエラーがすべて含まれます。

| バリデータ | 検証する型 |
|---|---|
| `StringValidator` | `string` |
| `NumberValidator<T>` | 数値型（`int`、`decimal` など） |
| `DateOnlyValidator` | `DateOnly` |
| `DateTimeOffsetValidator` | `DateTimeOffset` |

## ルールを宣言する

コンストラクタに表示名（エラーメッセージで使う項目名）を渡し、ルールのメソッドをつないで宣言します。
ルールは最初に1回だけ宣言し、その後は変更しない前提です。
`static readonly` フィールドに置いて、使い回してかまいません。

```csharp
private static readonly StringValidator NameValidator =
    new StringValidator("氏名", target: "name").Required().MaxLength(50);

private static readonly NumberValidator<int> QuantityValidator =
    new NumberValidator<int>("数量").Required().Range(1, 99);
```

`target` に指定した値はエラーの `Target` に入ります。
入力項目とエラーを結びつけるのに使います（[Razor Pages](../06-aspnetcore/02-razor-pages.md) を参照）。

## 検証する

`Check` に値を渡すと、宣言したすべてのルールで検証し、`Result` を返します。

```csharp
Result result = NameValidator.Check(input.Name);
```

- 値が `null` の場合（`StringValidator` で `Required` を宣言した場合は、空文字や空白だけの文字列も含む）は、値がないものとして扱います。
- 値がないときは、`Required` を宣言していれば必須エラーだけを返します。`Required` を宣言していなければ、すべてのルールをスキップして成功を返します。
- 値があるときは、すべてのルールを評価し、違反したルールのエラーをすべて返します。

表示名を多言語化する場合などは、`Check(value, displayName)` で検証時に表示名を上書きできます。

## 主なルール

| バリデータ | ルール |
|---|---|
| `StringValidator` | `Required`、`NotNull`、`MinLength`、`MaxLength`、`Length`、`Matches`、`Email`、`OnlyCharacters`、`Must` |
| `NumberValidator<T>` | `Required`、`Min`、`Max`、`Range`、`Positive`、`NonNegative`、`Digits`、`Must` |
| `DateOnlyValidator` / `DateTimeOffsetValidator` | `Required`、`Min`、`Max`、`Range`、`Past`、`Future`、`Must` |

`Past` と `Future` は現在日時を `TimeProvider` から取得します。

`OnlyCharacters` には許可する文字種を `CharacterKinds` のフラグで指定します（半角英数字、ひらがな、全角カタカナなど）。

```csharp
new StringValidator("会員番号").Required().Length(8).OnlyCharacters(CharacterKinds.AsciiDigit);
```

### 文字数の数え方

`StringValidator` は、既定では UTF-16 のコード単位（`string.Length`）で文字数を数えます。
コンストラクタの `lengthUnit` で、数え方を変えられます。
DB の列の長さの数え方に合わせて選んでください。

| `StringLengthUnit` | 数え方 | 一致する DB の型の例 |
|---|---|---|
| `Utf16CodeUnit`（既定） | UTF-16 のコード単位 | SQL Server の `nvarchar(n)` |
| `CodePoint` | Unicode のコードポイント | PostgreSQL・MySQL の `varchar(n)` |
| `TextElement` | 見た目の1文字（書記素クラスタ） | — |

## メッセージ

各ルールの `message` 引数で、エラーメッセージを変えられます。
メッセージは複合書式文字列で、`{0}` が表示名、`{1}` 以降がルールの引数に置き換わります。

```csharp
new StringValidator("書名").MaxLength(200, "{0}が長すぎます（最大{1}文字）。");
```

`message` を省略すると、既定のメッセージを使います。
既定のメッセージは、検証時の `CultureInfo.CurrentUICulture` に応じて日本語または英語になります。

## 値オブジェクトで使う

値オブジェクトの `ValidateValue` の中でバリデータを使うこともできます。

```csharp
private static readonly StringValidator Validator = new StringValidator("書名").Required().MaxLength(200);

public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
{
    var result = Validator.Check(value);
    errorMessage = result.IsFailure ? result.GetFirstErrorMessage() : null;
    return result.IsSuccess;
}
```

## 関連する API

- @D3Fragments.Validations.StringValidator
- <xref:D3Fragments.Validations.NumberValidator`1>
- @D3Fragments.Validations.DateOnlyValidator
- @D3Fragments.Validations.DateTimeOffsetValidator
- @D3Fragments.Validations.CharacterKinds
- @D3Fragments.Validations.StringLengthUnit
