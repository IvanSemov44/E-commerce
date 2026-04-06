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
        var existingProjection = await orderingDbContext.Addresses
            .FirstOrDefaultAsync(x => x.Id == notification.AddressId, cancellationToken);

        if (notification.IsDeleted)
        {
            if (existingProjection is not null)
            {
                orderingDbContext.Addresses.Remove(existingProjection);
                await orderingDbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (existingProjection is null)
        {
            existingProjection = new AddressReadModel
            {
                Id = notification.AddressId
            };
            orderingDbContext.Addresses.Add(existingProjection);
        }

        existingProjection.UserId = notification.UserId;
        existingProjection.StreetLine1 = notification.StreetLine1;
        existingProjection.City = notification.City;
        existingProjection.Country = notification.Country;
        existingProjection.PostalCode = notification.PostalCode;
        existingProjection.UpdatedAt = notification.OccurredAt;

        await orderingDbContext.SaveChangesAsync(cancellationToken);
    }
}
