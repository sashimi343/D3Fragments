using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;

using D3Fragments.Application;

namespace BasicWebApp.Application.Books;

// 取り置き期限切れの資料を一括処理し、処理した資料の件数を返す（日次バッチ想定）
public class ExpireHoldsCommand : ICommand<int>
{
}

public class ExpireHoldsCommandHandler : IRequestHandler<ExpireHoldsCommand, int>
{
    private readonly IBookRepository _bookRepository;
    private readonly TimeProvider _timeProvider;

    public ExpireHoldsCommandHandler(IBookRepository bookRepository, TimeProvider timeProvider)
    {
        _bookRepository = bookRepository;
        _timeProvider = timeProvider;
    }

    // ponytail: 全件を1トランザクションで処理するため、1件でも競合すると全体が失敗する。件数が増えたら資料ごとにCommandを分ける
    public async Task<int> HandleAsync(ExpireHoldsCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetLocalNow();
        List<Book> books = await _bookRepository.FindExpiredHoldsAsync(DateOnly.FromDateTime(now.DateTime), cancellationToken);
        foreach (Book book in books)
        {
            book.ExpireHold(now);
        }
        return books.Count;
    }
}
