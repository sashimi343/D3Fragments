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

    [BindProperty(SupportsGet = true), Range(IPager.StartPageNo, int.MaxValue, ErrorMessage = "The page number must be 1 or greater.")]
    public int PageNo { get; set; } = IPager.StartPageNo;

    public Pager<Book> Books { get; private set; } = Pager<Book>.Empty(ListBooksQuery.DefaultPageSize);

    public bool CanAdvanceDate => environment.IsDevelopment();

    [BindProperty, Range(1, int.MaxValue, ErrorMessage = "The number of days must be 1 or greater.")]
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
        TempData["Success"] = "The book has been registered.";
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
        TempData["Success"] = "The book has been withdrawn.";
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
                TempData["Success"] = $"The business date has been advanced by {Days} days.";
                return RedirectToPage(new { PageNo });
            }
            catch (ArgumentOutOfRangeException)
            {
                ModelState.AddModelError(nameof(Days), "The date is outside the representable range.");
            }
        }
        await LoadAsync();
        return InvalidInput();
    });

    private async Task LoadAsync() => Books = await listBooks.ExecuteAsync(new ListBooksQuery { PageNo = PageNo }, HttpContext.RequestAborted);

    public sealed class BookInput
    {
        [Required(ErrorMessage = "Enter an ISBN."), Display(Name = "ISBN")]
        public string Isbn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a title."), StringLength(200, ErrorMessage = "The title must be 200 characters or fewer."), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter an author."), Display(Name = "Author")]
        public string Author { get; set; } = string.Empty;
    }
}
