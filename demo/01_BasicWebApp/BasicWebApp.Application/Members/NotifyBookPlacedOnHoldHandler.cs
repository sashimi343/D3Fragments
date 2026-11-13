using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;

using D3Fragments;
using D3Fragments.Application;

using Microsoft.Extensions.Logging;

namespace BasicWebApp.Application.Members;

public class NotifyBookPlacedOnHoldHandler : IAfterCommitDomainEventHandler<BookPlacedOnHoldDomainEvent>
{
    private readonly ILogger<NotifyBookPlacedOnHoldHandler> _logger;

    public NotifyBookPlacedOnHoldHandler(ILogger<NotifyBookPlacedOnHoldHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(BookPlacedOnHoldDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // 取り置きされたことをメールなどで通知する（デモAPではログ記録のみ）
        _logger.LogInformation("MemberId = '{memberId}': BookId '{bookId}' の取り置きが完了しました。", domainEvent.MemberId, domainEvent.BookId);
        return Task.CompletedTask;
    }
}
