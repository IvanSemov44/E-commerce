using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Integration;

public sealed class OrderFulfillmentSagaService(AppDbContext dbContext) : IOrderFulfillmentSagaService
{
    public async Task StartAsync(OrderPlacedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.OrderFulfillmentSagaStates
            .FirstOrDefaultAsync(x => x.OrderId == integrationEvent.OrderId, cancellationToken);

        if (existing is not null)
            return;

        var now = DateTime.UtcNow;
        dbContext.OrderFulfillmentSagaStates.Add(new OrderFulfillmentSagaState
        {
            Id = Guid.NewGuid(),
            CorrelationId = integrationEvent.CorrelationId,
            OrderId = integrationEvent.OrderId,
            CustomerId = integrationEvent.CustomerId,
            CurrentState = OrderFulfillmentSagaStates.AwaitingInventory,
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleInventoryReservedAsync(InventoryReservedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var saga = await FindSagaAsync(integrationEvent.CorrelationId, integrationEvent.OrderId, cancellationToken);
        if (saga is null)
            return;

        if (IsTerminal(saga.CurrentState))
            return;

        var now = DateTime.UtcNow;
        saga.CurrentState = OrderFulfillmentSagaStates.Completed;
        saga.CompletedAt = now;
        saga.UpdatedAt = now;
        saga.FailureReason = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleInventoryReservationFailedAsync(InventoryReservationFailedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var saga = await FindSagaAsync(integrationEvent.CorrelationId, integrationEvent.OrderId, cancellationToken);
        if (saga is null)
            return;

        if (IsTerminal(saga.CurrentState))
            return;

        var now = DateTime.UtcNow;
        saga.CurrentState = OrderFulfillmentSagaStates.CompensatedFailed;
        saga.CompletedAt = now;
        saga.UpdatedAt = now;
        saga.FailureReason = integrationEvent.Reason.Length > 1000
            ? integrationEvent.Reason[..1000]
            : integrationEvent.Reason;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<OrderFulfillmentSagaState?> FindSagaAsync(Guid correlationId, Guid orderId, CancellationToken cancellationToken)
    {
        return dbContext.OrderFulfillmentSagaStates
            .FirstOrDefaultAsync(
                x => x.CorrelationId == correlationId || x.OrderId == orderId,
                cancellationToken);
    }

    private static bool IsTerminal(string state)
        => state == OrderFulfillmentSagaStates.Completed ||
           state == OrderFulfillmentSagaStates.CompensatedFailed;
}
