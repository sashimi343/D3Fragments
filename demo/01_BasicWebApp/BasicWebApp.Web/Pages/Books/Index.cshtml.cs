using System.ComponentModel.DataAnnotations;

using BasicWebApp.Application.Books;
using BasicWebApp.Domain.Books;

using D3Fragments;
using D3Fragments.Application;

using Microsoft.AspNetCore.Mvc;

namespace BasicWebApp.Web.Pages.Books;

public sealed class IndexModel(IRequestExecutor<ListBooksQuery, Pager<Book>> listBooks, PageErrorHandler errors) : AppPageModel(errors)
{
    [BindProperty(SupportsGet = true), Range(IPager.StartPageNo, int.MaxValue, ErrorMessage = "ページ番号は1以上で指定してください。")]
    public int PageNo { get; set; } = IPager.StartPageNo;

    [BindProperty(SupportsGet = true), EnumDataType(typeof(BookStatus), ErrorMessage = "資料の状態が不正です。")]
    public BookStatus? Status { get; set; }

    public Pager<Book> Books { get; private set; } = Pager<Book>.Empty(ListBooksQuery.DefaultPageSize);

    public Task<IActionResult> OnGetAsync()
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(InvalidInput());
        }
        return ExecuteWithErrorsAsync(async () =>
        {
            Books = await listBooks.ExecuteAsync(new ListBooksQuery { PageNo = PageNo, BookStatus = Status }, HttpContext.RequestAborted);
            if (Books.PageNo > Books.TotalPages)
            {
                return RedirectToPage(new { PageNo = Books.TotalPages, Status });
            }
            return Page();
        });
    }
}
