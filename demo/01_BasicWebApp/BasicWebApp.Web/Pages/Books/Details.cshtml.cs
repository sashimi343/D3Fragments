using BasicWebApp.Application.Books;
using BasicWebApp.Application.Loans;
using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Loans;

using D3Fragments;
using D3Fragments.Application;

using Microsoft.AspNetCore.Mvc;

namespace BasicWebApp.Web.Pages.Books;

public sealed class DetailsModel(
    IRequestExecutor<GetBookQuery, Book> getBook,
    IRequestExecutor<LoanBookCommand, Loan> loanBook,
    IRequestExecutor<ReserveBookCommand, Book> reserveBook,
    IRequestExecutor<CancelReservationCommand, Book> cancelReservation,
    PageErrorHandler errors) : AppPageModel(errors)
{
    [BindProperty(SupportsGet = true)]
    public UuidV7 Id { get; set; }

    public Book? Book { get; private set; }

    public UuidV7? CurrentMemberId => User.Identity?.IsAuthenticated == true ? MemberId : null;

    public Task<IActionResult> OnGetAsync()
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(InvalidInput());
        }
        return ExecuteWithErrorsAsync(async () =>
        {
            await LoadAsync();
            return Page();
        });
    }

    public Task<IActionResult> OnPostBorrowAsync() => ExecuteCommandAsync(async () =>
        await loanBook.ExecuteAsync(new LoanBookCommand { BookId = Id, MemberId = MemberId }, HttpContext.RequestAborted), "貸出が完了しました。");

    public Task<IActionResult> OnPostReserveAsync() => ExecuteCommandAsync(async () =>
        await reserveBook.ExecuteAsync(new ReserveBookCommand { BookId = Id, MemberId = MemberId }, HttpContext.RequestAborted), "予約が完了しました。");

    public Task<IActionResult> OnPostCancelAsync() => ExecuteCommandAsync(async () =>
        await cancelReservation.ExecuteAsync(new CancelReservationCommand { BookId = Id, MemberId = MemberId }, HttpContext.RequestAborted), "予約を取り消しました。");

    private async Task LoadAsync() => Book = await getBook.ExecuteAsync(new GetBookQuery { BookId = Id }, HttpContext.RequestAborted);

    private Task<IActionResult> ExecuteCommandAsync(Func<Task> action, string message)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult<IActionResult>(Challenge());
        }
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(InvalidInput());
        }
        return ExecuteWithErrorsAsync(async () =>
        {
            await action();
            TempData["Success"] = message;
            return RedirectToPage(new { id = Id });
        }, LoadAsync);
    }
}
