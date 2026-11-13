using System.ComponentModel.DataAnnotations;

using BasicWebApp.Application.Books;
using BasicWebApp.Domain.Books;

using D3Fragments;
using D3Fragments.Application;

using Microsoft.AspNetCore.Mvc;

namespace BasicWebApp.Web.Pages.Admin;

public sealed class IndexModel(
    IRequestExecutor<ListBooksQuery, Pager<Book>> listBooks,
    IRequestExecutor<RegisterBookCommand, Book> registerBook,
    IRequestExecutor<DeleteBookCommand, Book> deleteBook,
    IWebHostEnvironment environment,
    TimeProvider timeProvider,
    PageErrorHandler errors) : AppPageModel(errors)
{
    public BookInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true), Range(IPager.StartPageNo, int.MaxValue, ErrorMessage = "ページ番号は1以上で指定してください。")]
    public int PageNo { get; set; } = IPager.StartPageNo;

    public Pager<Book> Books { get; private set; } = Pager<Book>.Empty(ListBooksQuery.DefaultPageSize);

    public bool CanAdvanceDate => environment.IsDevelopment();

    [BindProperty, Range(1, int.MaxValue, ErrorMessage = "日数は1以上で入力してください。")]
    public int Days { get; set; } = 1;

    public Task<IActionResult> OnGetAsync() => ExecuteWithErrorsAsync(async () =>
    {
        if (!ModelState.IsValid)
        {
            return InvalidInput();
        }
        await LoadAsync();
        if (Books.PageNo > Books.TotalPages)
        {
            return RedirectToPage(new { PageNo = Books.TotalPages });
        }
        return Page();
    });

    public Task<IActionResult> OnPostRegisterAsync([Bind(Prefix = "Input")] BookInput input) => ExecuteWithErrorsAsync(async () =>
    {
        Input = input;
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return InvalidInput();
        }
        await registerBook.ExecuteAsync(new RegisterBookCommand { Isbn = Input.Isbn, Title = Input.Title, Author = Input.Author }, HttpContext.RequestAborted);
        TempData["Success"] = "資料を登録しました。";
        return RedirectToPage(new { PageNo });
    }, LoadAsync);

    public Task<IActionResult> OnPostDeleteAsync(UuidV7 bookId) => ExecuteWithErrorsAsync(async () =>
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return InvalidInput();
        }
        await deleteBook.ExecuteAsync(new DeleteBookCommand { BookId = bookId }, HttpContext.RequestAborted);
        TempData["Success"] = "資料を除籍しました。";
        return RedirectToPage(new { PageNo });
    }, LoadAsync);

    public Task<IActionResult> OnPostAdvanceAsync() => ExecuteWithErrorsAsync(async () =>
    {
        if (!CanAdvanceDate)
        {
            return NotFound();
        }
        if (ModelState.IsValid)
        {
            try
            {
                ((DemoTimeProvider)timeProvider).Advance(Days);
                TempData["Success"] = $"業務日付を{Days}日進めました。";
                return RedirectToPage(new { PageNo });
            }
            catch (ArgumentOutOfRangeException)
            {
                ModelState.AddModelError(nameof(Days), "表現可能な日付の範囲を超えています。");
            }
        }
        await LoadAsync();
        return InvalidInput();
    });

    private async Task LoadAsync() => Books = await listBooks.ExecuteAsync(new ListBooksQuery { PageNo = PageNo }, HttpContext.RequestAborted);

    public sealed class BookInput
    {
        [Required(ErrorMessage = "ISBNを入力してください。"), Display(Name = "ISBN")]
        public string Isbn { get; set; } = string.Empty;

        [Required(ErrorMessage = "書名を入力してください。"), StringLength(200, ErrorMessage = "書名は200文字以内で入力してください。"), Display(Name = "書名")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "著者を入力してください。"), Display(Name = "著者")]
        public string Author { get; set; } = string.Empty;
    }
}
