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
        // Notify the member of the hold by email, etc. (the demo app only writes a log)
        _logger.LogInformation("MemberId = '{memberId}': BookId '{bookId}' has been placed on hold.", domainEvent.MemberId, domainEvent.BookId);
        return Task.CompletedTask;
    }
}
