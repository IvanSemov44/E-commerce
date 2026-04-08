using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Integration;

public sealed class OrderFulfillmentSagaService(
    AppDbContext dbContext,
    IOrderCompensationService compensationService,
    IOptions<OrderFulfillmentSagaOptions> options) : IOrderFulfillmentSagaService
{
    private readonly OrderFulfillmentSagaOptions _options = options.Value;

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

        var reason = integrationEvent.Reason.Length > 1000
            ? integrationEvent.Reason[..1000]
            : integrationEvent.Reason;

        await compensationService.CompensateOrderAsync(saga.OrderId, reason, cancellationToken);

        var now = DateTime.UtcNow;
        saga.CurrentState = OrderFulfillmentSagaStates.CompensatedFailed;
        saga.CompletedAt = now;
        saga.UpdatedAt = now;
        saga.FailureReason = reason;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> HandleTimeoutsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var timeoutMinutes = Math.Max(1, _options.InventoryTimeoutMinutes);
        var timeoutThreshold = utcNow.AddMinutes(-timeoutMinutes);

        var timedOutSagas = await dbContext.OrderFulfillmentSagaStates
            .Where(x =>
                x.CurrentState == OrderFulfillmentSagaStates.AwaitingInventory &&
                x.CreatedAt <= timeoutThreshold)
            .ToListAsync(cancellationToken);

        if (timedOutSagas.Count == 0)
            return 0;

        foreach (var saga in timedOutSagas)
        {
            const string timeoutReason = "Timed out waiting for inventory reservation.";
            await compensationService.CompensateOrderAsync(saga.OrderId, timeoutReason, cancellationToken);

            saga.CurrentState = OrderFulfillmentSagaStates.CompensatedFailed;
            saga.CompletedAt = utcNow;
            saga.UpdatedAt = utcNow;
            saga.FailureReason = timeoutReason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return timedOutSagas.Count;
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
