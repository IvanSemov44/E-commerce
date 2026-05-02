using ECommerce.Contracts;
using ECommerce.Ordering.Application.Commands.ConfirmOrder;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.IntegrationEvents;

public sealed class PaymentProcessedIntegrationEventHandler(
    OrderingDbContext dbContext,
    ISender sender)
    : INotificationHandler<PaymentProcessedIntegrationEvent>
{
    public async Task Handle(PaymentProcessedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await dbContext.InboxMessages
            .AnyAsync(m => m.IdempotencyKey == notification.IdempotencyKey, cancellationToken);
        if (alreadyProcessed)
            return;

        await sender.Send(new ConfirmOrderCommand(notification.OrderId), cancellationToken);

        dbContext.InboxMessages.Add(new InboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = notification.IdempotencyKey,
            EventType = notification.GetType().Name,
            ReceivedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            AttemptCount = 1
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
