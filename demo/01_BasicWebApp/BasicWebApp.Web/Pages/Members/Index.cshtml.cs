using BasicWebApp.Application.Loans;
using BasicWebApp.Application.Members;
using BasicWebApp.Domain.Loans;

using D3Fragments;
using D3Fragments.Application;

using Microsoft.AspNetCore.Mvc;

namespace BasicWebApp.Web.Pages.Members;

public sealed class IndexModel(
    IRequestExecutor<GetMemberWithLoanCountQuery, MemberWithLoanCountDto> getMember,
    IRequestExecutor<ListMemberLoansQuery, List<LoanWithOverdueDto>> listLoans,
    IRequestExecutor<ReturnBookCommand, Loan> returnBook,
    IRequestExecutor<RenewLoanCommand, Loan> renewLoan,
    PageErrorHandler errors) : AppPageModel(errors)
{
    [BindProperty]
    public UuidV7 LoanId { get; set; }

    public MemberWithLoanCountDto? MemberInfo { get; private set; }

    public List<LoanWithOverdueDto> Loans { get; private set; } = [];

    public Task<IActionResult> OnGetAsync() => ExecuteWithErrorsAsync(async () =>
    {
        await LoadAsync();
        return Page();
    });

    public Task<IActionResult> OnPostReturnAsync() => ExecuteCommandAsync(async () =>
        await returnBook.ExecuteAsync(new ReturnBookCommand { LoanId = LoanId, MemberId = MemberId }, HttpContext.RequestAborted), "返却が完了しました。");

    public Task<IActionResult> OnPostRenewAsync() => ExecuteCommandAsync(async () =>
        await renewLoan.ExecuteAsync(new RenewLoanCommand { LoanId = LoanId, MemberId = MemberId }, HttpContext.RequestAborted), "貸出期限を延長しました。");

    private async Task LoadAsync()
    {
        MemberInfo = await getMember.ExecuteAsync(new GetMemberWithLoanCountQuery { EmailAddress = MemberEmail }, HttpContext.RequestAborted);
        Loans = await listLoans.ExecuteAsync(new ListMemberLoansQuery { MemberId = MemberId }, HttpContext.RequestAborted);
    }

    private Task<IActionResult> ExecuteCommandAsync(Func<Task> action, string message) => ExecuteWithErrorsAsync(async () =>
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return InvalidInput();
        }
        await action();
        TempData["Success"] = message;
        return RedirectToPage();
    }, LoadAsync);
}
