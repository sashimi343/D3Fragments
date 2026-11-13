# Result とエラー

D3Fragments では処理の失敗を次の2つの方法で表します。

- `Result` / `Result<T>`：失敗を戻り値で返します。検証エラーをまとめて返したい場合などに使います。
- `DomainException`：失敗を例外で伝えます。Command / Query の Handler は、失敗を例外で表します。

どちらも失敗の内容を `Error` で表します。
そのため、`Result` と `DomainException` は相互に変換できます。

## Error と ErrorType

`Error` は、1件のエラーの情報を持つクラスです。

| プロパティ | 内容 |
|---|---|
| `Type` | エラーの分類（`ErrorType`） |
| `Message` | エラーメッセージ |
| `Code` | アプリケーション固有のエラーコード（省略可）。API の応答にエラーコードを含める場合などに使う |
| `Target` | エラーの対象を表すキー（省略可）。入力項目とエラーを結びつける場合などに使う |

`Error` は、分類ごとのファクトリーメソッドで作ります。

```csharp
var error = Error.Validation("書名を指定してください。", target: "title");
```

`ErrorType` の値と、ASP.NET Core 連携での既定の HTTP ステータスコードは次のとおりです。

| `ErrorType` | 意味 | HTTP ステータス |
|---|---|---|
| `Unauthenticated` | 未認証（誰なのかを識別できない） | 401 |
| `Validation` | 入力値が妥当でない | 400 |
| `NotFound` | 対象が存在しない | 404 |
| `Conflict` | 状態の競合（キーの重複など） | 409 |
| `Forbidden` | 権限がない（誰なのかは分かる） | 403 |
| `InvalidState` | エンティティや集約が、その処理を受け付けない状態にある | 409 |
| `PreconditionFailed` | 事前条件を満たしていない | 422 |

## Result

`Result` は成功または失敗を表し、失敗の場合はエラーの一覧（`Errors`）を持ちます。
`Result<T>` は、成功の場合に値（`Value`）も持ちます。

```csharp
Result success = Result.Success();
Result failure = Result.Failure(Error.PreconditionFailed("貸出停止中のため貸出できません。"));

Result<BookTitle> title = BookTitle.TryCreate(input);
if (title.IsSuccess)
{
    Use(title.Value); // IsSuccess が true なら Value は null でない
}
```

`IsSuccess` が `true` の分岐では、`Value` を使っても null 許容の警告は出ません。
`Result<T>` には `T` の値を直接代入することもできます（暗黙の変換）。

### 複数の結果をまとめる

| メソッド | 内容 |
|---|---|
| `Result.Combine(results)` | すべて成功なら成功、それ以外はすべてのエラーを持つ失敗を返す |
| `Result.CombineErrors(results)` | すべての結果のエラーを1つのリストにまとめる |
| `result.Merge(other)` | 2つの結果をまとめる |

### 例外に変換する

`Result` が失敗の場合に `DomainException` をスローするメソッドがあります。
Handler の中で、`Result` を返すドメインのメソッドやドメインルールを使う場合に便利です。

```csharp
// 失敗なら、すべてのエラーを持つ DomainException をスローする
rule.Check(context).ThrowIfFailure();

// 成功なら値を返し、失敗なら DomainException をスローする
Book book = Book.TryCreate(isbn, title, author).GetValueOrThrow();
```

どちらにも、エラーコードを指定するオーバーロードと、スローする例外を自分で作るオーバーロードがあります。

## DomainException

`DomainException` は、ドメインのルールに反したことを表す例外です。
`Error` から作るか、分類とメッセージを指定して作ります。

```csharp
throw new DomainException(Error.InvalidState("この資料は貸出できません。"));
throw new DomainException(ErrorType.InvalidState, "在架でない資料は除籍できません。");
```

複数のエラーから作ることもできます。
すべてのエラーは `Errors` で取得でき、`ErrorType` と `Message` には先頭のエラーの値が入ります。

よく使う分類には、専用の派生クラスがあります。

| 例外 | `ErrorType` |
|---|---|
| `DomainValidationException` | `Validation` |
| `EntityNotFoundException` | `NotFound` |
| `ConflictOccurredException` | `Conflict` |

```csharp
var book = await books.FindAsync(id, cancellationToken)
    ?? throw new EntityNotFoundException(nameof(Book));
```

`EntityNotFoundException` にエンティティ名を渡すと、それを含んだ既定のメッセージになります。

### 例外から Result に変換する

`IRequestExecutor` の拡張メソッド `TryExecuteAsync` を使うと、Handler がスローした `DomainException` を失敗の `Result` として受け取れます。
`DomainException` 以外の例外はそのまま伝わります。
詳しくは [Command と Query](../04-application/01-commands-and-queries.md) を参照してください。

## 関連する API

- @D3Fragments.Result
- <xref:D3Fragments.Result`1>
- @D3Fragments.Error
- @D3Fragments.ErrorType
- @D3Fragments.DomainException
