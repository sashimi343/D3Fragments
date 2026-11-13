# 型安全な列挙型

`TypeSafeEnum<TSelf, TKey>` は、値ごとにデータや振る舞いを持てる列挙型です。
組み込みの `enum` と違い、定義した値以外は存在しません。キャストで不正な値を作ることもできません。

## 列挙型を定義する

`TypeSafeEnum<TSelf, TKey>` を継承し、各値を `static readonly` フィールドとして定義します。
`TKey` は、各値を識別するキー（DB に保存する値など）の型です。
コンストラクタは `private` にして、クラスの外で値を作れないようにします。

```csharp
using D3Fragments;

public sealed class MemberType : TypeSafeEnum<MemberType, int>
{
    public static readonly MemberType Regular = new(0, "一般", loanLimit: 5, loanPeriodDays: 14);
    public static readonly MemberType Student = new(1, "学生", loanLimit: 10, loanPeriodDays: 28);

    private MemberType(int key, string name, int loanLimit, int loanPeriodDays) : base(key, name)
    {
        LoanLimit = loanLimit;
        LoanPeriodDays = loanPeriodDays;
    }

    public int LoanLimit { get; }

    public int LoanPeriodDays { get; }
}
```

キーと名前は、それぞれ一意である必要があります。
重複していると、型の初期化時に例外がスローされます。

次のような定義の誤りは、アナライザが警告します。

- コンストラクタが `private` でない（[DFR0006](../../analyzers/DFR0006.md)）
- `TSelf` に自身以外の型を指定している（[DFR0007](../../analyzers/DFR0007.md)）
- 値を `static readonly` フィールド以外で定義している（[DFR0008](../../analyzers/DFR0008.md)）

値ごとに振る舞いを変えたい場合は、`TSelf` を抽象クラスにし、各値を `private` な入れ子クラスで定義します。

## 値を取得する

| メンバー | 内容 |
|---|---|
| `Key` / `Name` | 値のキーと名前（表示名）。名前を省略すると、キーの文字列表現が名前になる |
| `Values` / `Keys` / `Count` | 定義されているすべての値・キーと、その数（定義順） |
| `Parse(key)` | キーに対応する値を取得する。定義されていなければ例外をスローする |
| `TryParse(key, out value)` | キーに対応する値の取得を試みる |
| `ParseAsResult(key)` | キーに対応する値を `Result<T>` で返す。定義されていなければ検証エラーになる |

外部から受け取ったキーを変換する場合は、`TryParse` か `ParseAsResult` を使います。

```csharp
if (!MemberType.TryParse(input.MemberTypeKey, out var memberType))
{
    ModelState.AddModelError(nameof(input.MemberTypeKey), "会員種別を選択してください。");
}
```

各値はインスタンスが1つだけなので、`==` で比較できます。

## 永続化と JSON

- EF Core：`AddD3FragmentsEFCore`（または `UseD3Fragments`）に渡したアセンブリにある型安全な列挙型には、キーで保存する値コンバーターが自動で登録されます。
- JSON：`TypeSafeEnumJsonConverterFactory` を使うと、キー（既定）または名前で読み書きできます。どちらで表すかはアプリケーションによって違うため、`AddD3FragmentsAspNetCore` は自動で登録しません。必要に応じて登録してください。

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new TypeSafeEnumJsonConverterFactory(TypeSafeEnumJsonFormat.Name)));
```

## 関連する API

- <xref:D3Fragments.TypeSafeEnum`2>
- @D3Fragments.TypeSafeEnumJsonConverterFactory
- @D3Fragments.TypeSafeEnumJsonFormat
