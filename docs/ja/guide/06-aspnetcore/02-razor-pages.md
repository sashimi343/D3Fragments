# Razor Pages

Razor Pages や MVC のビューでは、エラーを Problem Details 応答で返すのではなく、入力フォームと一緒に表示します。
`AddDomainErrors` を使うと、ドメインのエラーを `ModelState` に追加できます。

## エラーを ModelState に追加する

`ModelState.AddDomainErrors(errors)` は、各エラーを `ModelState` に追加します。

- `Error.Target` があるエラーは `Target` をキーにして追加します。キーが入力項目の名前と一致すれば、その項目のエラーとして表示されます。
- `Target` がないエラーはモデル全体のエラー（空文字のキー）として追加します。

```csharp
public sealed class RegisterModel(IRequestExecutor<RegisterBookCommand, Book> registerBook) : PageModel
{
    [BindProperty]
    public BookInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Result<Book> result = await registerBook.TryExecuteAsync(Input.ToCommand(), HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            ModelState.AddDomainErrors(result.Errors);
            return Page();
        }

        return RedirectToPage("Index");
    }
}
```

`TryExecuteAsync` の代わりに `DomainException` を捕まえる場合は、`exception.Errors` を渡します。

```csharp
catch (DomainException exception)
{
    ModelState.AddDomainErrors(exception.Errors);
    Response.StatusCode = options.Value.GetStatusCode(exception.ErrorType);
    return Page();
}
```

この例では `IOptions<DomainProblemDetailsOptions>` から、エラーの種類に応じた HTTP ステータスコードを取得して設定しています。

## 入力項目とエラーを結びつける

エラーを特定の入力項目に表示したい場合は、エラーを作るときに `target` を指定します。

```csharp
throw new DomainException(Error.Conflict("このメールアドレスはすでに登録されています。", target: "Input.EmailAddress"));
```

バリデータの場合は、コンストラクタの `target` で指定します（[入力値の検証](../03-domain/05-validation.md) を参照）。

## 関連する API

- @D3Fragments.AspNetCore.ModelStateDictionaryExtensions
- @D3Fragments.Application.RequestExecutorExtensions
