using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrderFulfillmentSagaServiceTests
{
    [TestMethod]
    public async Task StartThenReserved_MarksSagaCompleted()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-success-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
        var service = new OrderFulfillmentSagaService(dbContext);

        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var orderPlaced = new OrderPlacedIntegrationEvent(orderId, Guid.NewGuid(), [Guid.NewGuid()], 120m)
        {
            CorrelationId = correlationId
        };

        await service.StartAsync(orderPlaced, CancellationToken.None);

        var inventoryReserved = new InventoryReservedIntegrationEvent(orderId, [Guid.NewGuid()], [1])
        {
            CorrelationId = correlationId
        };

        await service.HandleInventoryReservedAsync(inventoryReserved, CancellationToken.None);

        var saga = await dbContext.OrderFulfillmentSagaStates.SingleAsync(x => x.OrderId == orderId);
        Assert.AreEqual(OrderFulfillmentSagaStates.Completed, saga.CurrentState);
        Assert.IsNotNull(saga.CompletedAt);
        Assert.IsNull(saga.FailureReason);
    }

    [TestMethod]
    public async Task StartThenReservationFailed_MarksSagaCompensatedFailed()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-failed-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
        var service = new OrderFulfillmentSagaService(dbContext);

        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await service.StartAsync(new OrderPlacedIntegrationEvent(orderId, Guid.NewGuid(), [Guid.NewGuid()], 80m)
        {
            CorrelationId = correlationId
        }, CancellationToken.None);

        await service.HandleInventoryReservationFailedAsync(new InventoryReservationFailedIntegrationEvent(orderId, Guid.NewGuid(), "No stock")
        {
            CorrelationId = correlationId
        }, CancellationToken.None);

        var saga = await dbContext.OrderFulfillmentSagaStates.SingleAsync(x => x.OrderId == orderId);
        Assert.AreEqual(OrderFulfillmentSagaStates.CompensatedFailed, saga.CurrentState);
        Assert.IsNotNull(saga.CompletedAt);
        Assert.AreEqual("No stock", saga.FailureReason);
    }

    [TestMethod]
    public async Task DuplicateFinalEvent_DoesNotChangeCompletedTimestamp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-duplicate-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
        var service = new OrderFulfillmentSagaService(dbContext);

        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await service.StartAsync(new OrderPlacedIntegrationEvent(orderId, Guid.NewGuid(), [Guid.NewGuid()], 150m)
        {
            CorrelationId = correlationId
        }, CancellationToken.None);

        var reserved = new InventoryReservedIntegrationEvent(orderId, [Guid.NewGuid()], [1])
        {
            CorrelationId = correlationId
        };

        await service.HandleInventoryReservedAsync(reserved, CancellationToken.None);
        var firstCompletion = (await dbContext.OrderFulfillmentSagaStates.SingleAsync(x => x.OrderId == orderId)).CompletedAt;

        await service.HandleInventoryReservedAsync(reserved, CancellationToken.None);
        var secondCompletion = (await dbContext.OrderFulfillmentSagaStates.SingleAsync(x => x.OrderId == orderId)).CompletedAt;

        Assert.AreEqual(firstCompletion, secondCompletion);
    }

    [TestMethod]
    public async Task UnknownOrderInventoryEvent_IsIgnoredSafely()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-unknown-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
        var service = new OrderFulfillmentSagaService(dbContext);

        await service.HandleInventoryReservedAsync(new InventoryReservedIntegrationEvent(Guid.NewGuid(), [Guid.NewGuid()], [1]), CancellationToken.None);
        await service.HandleInventoryReservationFailedAsync(new InventoryReservationFailedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), "none"), CancellationToken.None);

        Assert.AreEqual(0, await dbContext.OrderFulfillmentSagaStates.CountAsync());
    }
}
