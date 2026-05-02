using ECommerce.Contracts;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.IntegrationEvents;

public sealed class AddressProjectionUpdatedIntegrationEventHandler(OrderingDbContext orderingDbContext)
    : INotificationHandler<AddressProjectionUpdatedIntegrationEvent>
{
    public async Task Handle(AddressProjectionUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await orderingDbContext.InboxMessages
            .AnyAsync(m => m.IdempotencyKey == notification.IdempotencyKey, cancellationToken);
        if (alreadyProcessed)
            return;

        var existingProjection = await orderingDbContext.Addresses
            .FirstOrDefaultAsync(x => x.Id == notification.AddressId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
                orderingDbContext.Addresses.Remove(existingProjection);
        }
        else
        {
            if (existingProjection is null)
            {
                existingProjection = new AddressReadModel { Id = notification.AddressId };
                orderingDbContext.Addresses.Add(existingProjection);
            }

            existingProjection.UserId = notification.UserId;
            existingProjection.StreetLine1 = notification.StreetLine1;
            existingProjection.City = notification.City;
            existingProjection.Country = notification.Country;
            existingProjection.PostalCode = notification.PostalCode;
            existingProjection.UpdatedAt = notification.OccurredAt;
        }

        orderingDbContext.InboxMessages.Add(new InboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = notification.IdempotencyKey,
            EventType = notification.GetType().Name,
            ReceivedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            AttemptCount = 1
        });

        await orderingDbContext.SaveChangesAsync(cancellationToken);
    }
}
