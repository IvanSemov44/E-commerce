using ECommerce.Contracts;

namespace ECommerce.Infrastructure.Integration;

public static class OrderFulfillmentSagaStates
{
    public const string AwaitingInventory = "AwaitingInventory";
    public const string Completed = "Completed";
    public const string CompensatedFailed = "CompensatedFailed";
}

public interface IOrderFulfillmentSagaService
{
    Task StartAsync(OrderPlacedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task HandleInventoryReservedAsync(InventoryReservedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task HandleInventoryReservationFailedAsync(InventoryReservationFailedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}