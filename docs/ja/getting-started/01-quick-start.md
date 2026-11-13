# クイックスタート

書籍を登録・取得する Web API を作りながら、D3Fragments の基本的な使い方を説明します。
DB には SQLite を使います。

完成すると、プロジェクトは次の4つになります。

| プロジェクト | 内容 |
|---|---|
| `Library.Domain` | 集約 `Book`、値オブジェクト `BookTitle`、リポジトリのインターフェース |
| `Library.Application` | 登録の Command と取得の Query |
| `Library.Infrastructure` | `DbContext` とリポジトリの実装 |
| `Library.Web` | Minimal API のエンドポイントと DI の登録 |

## 1. ドメイン層を作る

`Library.Domain` に `Sashimi343.D3Fragments` を追加します。

まず、書名を表す値オブジェクトを作ります。
値オブジェクトは `ValueObjectBase<TSelf, TPrimitive>` を継承し、`IValueObject<TSelf, TPrimitive>` を実装します。
検証は `ValidateValue` に書きます。基底クラスのコンストラクタが `ValidateValue` を呼び、不正な値なら `DomainValidationException` をスローします。

```csharp
using System.Diagnostics.CodeAnalysis;
using D3Fragments;

namespace Library.Domain.Books;

public sealed class BookTitle : ValueObjectBase<BookTitle, string>, IValueObject<BookTitle, string>
{
    private BookTitle(string value) : base(value)
    {
    }

    public static BookTitle Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 200)
        {
            errorMessage = "書名は1文字以上、200文字以内で指定してください。";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
```

次に、集約ルート `Book` を作ります。
`AggregateRoot` を継承すると、ID（`UuidV7`）が自動で採番され、作成日時・更新日時・バージョン番号も自動で記録されます。

```csharp
using D3Fragments;

namespace Library.Domain.Books;

public sealed class Book : AggregateRoot
{
    private Book(BookTitle title, string author)
    {
        Title = title;
        Author = author;
    }

    // EF Core 用（値オブジェクトのプロパティはコンストラクタ引数にバインドできない）
    private Book()
    {
        Title = null!;
        Author = null!;
    }

    public BookTitle Title { get; private set; }

    public string Author { get; private set; }

    public static Book Create(string title, string author) => new(BookTitle.Create(title), author);
}
```

最後に、リポジトリのインターフェースを定義します。
`IRepository<TAggregate>` を継承すると、`FindAsync`・`Add`・`Delete` などの基本操作が定義済みになります。

```csharp
using D3Fragments;

namespace Library.Domain.Books;

public interface IBookRepository : IRepository<Book>
{
}
```

## 2. アプリケーション層を作る

`Library.Application` に `Sashimi343.D3Fragments.Application` を追加し、`Library.Domain` を参照します。

変更を伴う処理は Command（`ICommand<TResponse>`）、読み取りだけの処理は Query（`IQuery<TResponse>`）として定義し、それぞれに Handler を実装します。
Handler はリポジトリを操作するだけで、保存は行いません。
Command の Handler が正常に終わると、Pipeline が変更を保存します。

```csharp
using D3Fragments;
using D3Fragments.Application;
using Library.Domain.Books;

namespace Library.Application.Books;

public sealed record RegisterBookCommand(string Title, string Author) : ICommand<UuidV7>;

public sealed class RegisterBookCommandHandler(IBookRepository books)
    : IRequestHandler<RegisterBookCommand, UuidV7>
{
    public Task<UuidV7> HandleAsync(RegisterBookCommand request, CancellationToken cancellationToken)
    {
        var book = Book.Create(request.Title, request.Author);
        books.Add(book);
        return Task.FromResult(book.Id);
    }
}
```

```csharp
using D3Fragments;
using D3Fragments.Application;
using Library.Domain.Books;

namespace Library.Application.Books;

public sealed record BookDto(UuidV7 Id, string Title, string Author);

public sealed record GetBookQuery(UuidV7 BookId) : IQuery<BookDto>;

public sealed class GetBookQueryHandler(IBookRepository books)
    : IRequestHandler<GetBookQuery, BookDto>
{
    public async Task<BookDto> HandleAsync(GetBookQuery request, CancellationToken cancellationToken)
    {
        var book = await books.FindAsync(request.BookId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Book));
        return new BookDto(book.Id, book.Title.Value, book.Author);
    }
}
```

失敗は例外で表します。
`EntityNotFoundException` などの `DomainException` はプレゼンテーション層でエラー応答に変換されます。

## 3. インフラストラクチャ層を作る

`Library.Infrastructure` に `Sashimi343.D3Fragments.EFCore` と `Microsoft.EntityFrameworkCore.Sqlite` を追加し、`Library.Application` を参照します。

`DbContext` は `D3DbContext` を継承します。
値オブジェクトのプロパティは `ComplexValueObject` でマッピングします。
マッピングしないと、モデルの構築時に例外がスローされます。

```csharp
using D3Fragments.EFCore;
using Library.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure;

public sealed class LibraryDbContext(DbContextOptions options) : D3DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(builder => builder.ComplexValueObject(x => x.Title));
    }
}
```

リポジトリの実装は `RepositoryBase<TContext, TAggregate>` を継承します。
基本操作は基底クラスが実装しているため、この例では何も書く必要がありません。

```csharp
using D3Fragments.EFCore;
using Library.Domain.Books;

namespace Library.Infrastructure;

public sealed class BookRepository(LibraryDbContext dbContext)
    : RepositoryBase<LibraryDbContext, Book>(dbContext), IBookRepository
{
}
```

## 4. プレゼンテーション層を作る

`Library.Web`（ASP.NET Core の空のプロジェクト）に `Sashimi343.D3Fragments.AspNetCore` を追加し、`Library.Application` と `Library.Infrastructure` を参照します。

`Program.cs` で D3Fragments のサービスを登録し、エンドポイントを定義します。
エンドポイントからは `IRequestExecutor<TRequest, TResponse>` を通して Command / Query を実行します。

```csharp
using D3Fragments;
using D3Fragments.Application;
using D3Fragments.AspNetCore;
using D3Fragments.EFCore;
using Library.Application.Books;
using Library.Domain.Books;
using Library.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Handler と Pipeline を登録する
builder.Services.AddD3FragmentsApplication([typeof(RegisterBookCommand).Assembly]);

// DbContext、Unit of Work、リポジトリを登録する
builder.Services.AddD3FragmentsEFCore<LibraryDbContext>(
    [typeof(Book).Assembly, typeof(BookRepository).Assembly],
    options => options.UseSqlite("Data Source=library.db"));

// DomainException を Problem Details 応答に変換する
builder.Services.AddD3FragmentsAspNetCore();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();

app.MapPost("/books", async (
    RegisterBookCommand command,
    IRequestExecutor<RegisterBookCommand, UuidV7> executor,
    CancellationToken cancellationToken) =>
{
    var id = await executor.ExecuteAsync(command, cancellationToken);
    return TypedResults.Created($"/books/{id}", new { id });
});

app.MapGet("/books/{id}", (
    UuidV7 id,
    IRequestExecutor<GetBookQuery, BookDto> executor,
    CancellationToken cancellationToken) =>
    executor.ExecuteAsync(new GetBookQuery(id), cancellationToken));

app.Run();
```

`AddD3FragmentsApplication` に渡したアセンブリから Handler が、`AddD3FragmentsEFCore` に渡したアセンブリからリポジトリが自動で登録されます。
`AddD3FragmentsEFCore` に渡したアセンブリは型安全な列挙型の値コンバーターの登録にも使うため、ドメイン層のアセンブリも渡します。

## 5. 動かしてみる

アプリケーションを起動し、書籍を登録します。

```bash
curl -i -X POST http://localhost:5000/books -H "Content-Type: application/json" -d '{"title":"Domain-Driven Design","author":"Eric Evans"}'
```

`201 Created` と、採番された ID が返ります。
返った ID で `GET /books/{id}` を呼ぶと、登録した書籍を取得できます。
書名を空にして送ると、`ValidateValue` の検証に失敗し、`400 Bad Request` の Problem Details 応答が返ります。
存在しない ID を取得すると `404 Not Found` が返ります。

## 次のステップ

もっと大きな実装例は [デモアプリケーション](https://github.com/sashimi343/D3Fragments/tree/main/demo/01_BasicWebApp)（図書館の貸出管理）で確認できます。
子エンティティを持つ集約、ドメインイベント、ドメインルール、Razor Pages でのエラー表示なども実装しています。


- [アーキテクチャ](../guide/01-architecture.md)：層の分け方と依存関係のルール
- [ドメイン層](../guide/03-domain/index.md)：エンティティや値オブジェクトなどの部品
- [アプリケーション層](../guide/04-application/index.md)：Command / Query と Pipeline
