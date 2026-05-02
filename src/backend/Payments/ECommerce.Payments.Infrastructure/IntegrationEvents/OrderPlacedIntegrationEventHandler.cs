using ECommerce.Contracts;
using ECommerce.Payments.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payments.Infrastructure.IntegrationEvents;

public sealed class OrderPlacedIntegrationEventHandler(PaymentsDbContext dbContext)
    : INotificationHandler<OrderPlacedIntegrationEvent>
{
    public async Task Handle(OrderPlacedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await dbContext.InboxMessages
            .AnyAsync(m => m.IdempotencyKey == notification.IdempotencyKey, cancellationToken);
        if (alreadyProcessed)
            return;

        var existing = await dbContext.PaymentOrders
            .FirstOrDefaultAsync(o => o.OrderId == notification.OrderId, cancellationToken);

        if (existing is null)
        {
            dbContext.PaymentOrders.Add(new PaymentOrderReadModel
            {
                Id = Guid.NewGuid(),
                OrderId = notification.OrderId,
                Amount = notification.TotalAmount,
                UserId = notification.CustomerId,
                CreatedAt = notification.OccurredAt,
                UpdatedAt = notification.OccurredAt
            });
        }

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
