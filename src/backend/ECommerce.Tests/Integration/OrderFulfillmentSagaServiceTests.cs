using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrderFulfillmentSagaServiceTests
{
    private static OrderFulfillmentSagaService CreateService(
        AppDbContext dbContext,
        TestOrderCompensationService? compensationService = null,
        OrderFulfillmentSagaOptions? options = null)
    {
        return new OrderFulfillmentSagaService(
            dbContext,
            compensationService ?? new TestOrderCompensationService(),
            Options.Create(options ?? new OrderFulfillmentSagaOptions()));
    }

    [TestMethod]
    public async Task StartThenReserved_MarksSagaCompleted()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-success-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
    var compensationService = new TestOrderCompensationService();
    var service = CreateService(dbContext, compensationService);

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
        Assert.AreEqual(0, compensationService.Calls.Count);
    }

    [TestMethod]
    public async Task StartThenReservationFailed_MarksSagaCompensatedFailed()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-failed-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
    var compensationService = new TestOrderCompensationService();
    var service = CreateService(dbContext, compensationService);

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
        Assert.AreEqual(1, compensationService.Calls.Count);
        Assert.AreEqual(orderId, compensationService.Calls[0].OrderId);
        Assert.AreEqual("No stock", compensationService.Calls[0].Reason);
    }

    [TestMethod]
    public async Task DuplicateFinalEvent_DoesNotChangeCompletedTimestamp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-duplicate-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
    var service = CreateService(dbContext);

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
    var service = CreateService(dbContext);

        await service.HandleInventoryReservedAsync(new InventoryReservedIntegrationEvent(Guid.NewGuid(), [Guid.NewGuid()], [1]), CancellationToken.None);
        await service.HandleInventoryReservationFailedAsync(new InventoryReservationFailedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), "none"), CancellationToken.None);

        Assert.AreEqual(0, await dbContext.OrderFulfillmentSagaStates.CountAsync());
    }

    [TestMethod]
    public async Task HandleTimeoutsAsync_TimedOutSaga_IsCompensatedAndMarkedFailed()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-timeout-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
        var compensationService = new TestOrderCompensationService();
        var service = CreateService(dbContext, compensationService, new OrderFulfillmentSagaOptions
        {
            InventoryTimeoutMinutes = 10,
            TimeoutPollIntervalSeconds = 30
        });

        var orderId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddMinutes(-20);

        await service.StartAsync(new OrderPlacedIntegrationEvent(orderId, Guid.NewGuid(), [Guid.NewGuid()], 42m)
        {
            CorrelationId = orderId,
            PublishedAt = startTime
        }, CancellationToken.None);

        var updatedSaga = await dbContext.OrderFulfillmentSagaStates.SingleAsync(x => x.OrderId == orderId);
        updatedSaga.CreatedAt = startTime;
        updatedSaga.UpdatedAt = startTime;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var now = DateTime.UtcNow;
        var handledCount = await service.HandleTimeoutsAsync(now, CancellationToken.None);

        Assert.AreEqual(1, handledCount);
        Assert.AreEqual(1, compensationService.Calls.Count);
        Assert.AreEqual(orderId, compensationService.Calls[0].OrderId);

        var saga = await dbContext.OrderFulfillmentSagaStates.SingleAsync(x => x.OrderId == orderId);
        Assert.AreEqual(OrderFulfillmentSagaStates.CompensatedFailed, saga.CurrentState);
        Assert.AreEqual("Timed out waiting for inventory reservation.", saga.FailureReason);
        Assert.AreEqual(now, saga.CompletedAt);
    }

    [TestMethod]
    public async Task HandleTimeoutsAsync_CompletedSaga_IsIgnored()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"saga-timeout-completed-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);
        var compensationService = new TestOrderCompensationService();
        var service = CreateService(dbContext, compensationService, new OrderFulfillmentSagaOptions
        {
            InventoryTimeoutMinutes = 1,
            TimeoutPollIntervalSeconds = 30
        });

        var orderId = Guid.NewGuid();
        await service.StartAsync(new OrderPlacedIntegrationEvent(orderId, Guid.NewGuid(), [Guid.NewGuid()], 99m)
        {
            CorrelationId = orderId
        }, CancellationToken.None);

        await service.HandleInventoryReservedAsync(new InventoryReservedIntegrationEvent(orderId, [Guid.NewGuid()], [1])
        {
            CorrelationId = orderId
        }, CancellationToken.None);

        var handledCount = await service.HandleTimeoutsAsync(DateTime.UtcNow.AddHours(1), CancellationToken.None);

        Assert.AreEqual(0, handledCount);
        Assert.AreEqual(0, compensationService.Calls.Count);
    }

    private sealed class TestOrderCompensationService : IOrderCompensationService
    {
        public List<(Guid OrderId, string Reason)> Calls { get; } = [];

        public Task CompensateOrderAsync(Guid orderId, string reason, CancellationToken cancellationToken = default)
        {
            Calls.Add((orderId, reason));
            return Task.CompletedTask;
        }
    }
}
