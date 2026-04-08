using ECommerce.Contracts;

namespace ECommerce.Infrastructure.Integration;

public static class OrderFulfillmentSagaStates
{
    public const string AwaitingInventory = "AwaitingInventory";
    public const string Completed = "Completed";
    public const string CompensatedFailed = "CompensatedFailed";
}

public sealed class OrderFulfillmentSagaOptions
{
    public int InventoryTimeoutMinutes { get; set; } = 15;

    public int TimeoutPollIntervalSeconds { get; set; } = 30;
}

public interface IOrderCompensationService
{
    Task CompensateOrderAsync(Guid orderId, string reason, CancellationToken cancellationToken = default);
}

public interface IOrderFulfillmentSagaService
{
    Task StartAsync(OrderPlacedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task HandleInventoryReservedAsync(InventoryReservedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task HandleInventoryReservationFailedAsync(InventoryReservationFailedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task<int> HandleTimeoutsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
