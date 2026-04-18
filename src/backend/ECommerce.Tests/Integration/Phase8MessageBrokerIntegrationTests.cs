using ECommerce.Contracts;
using ECommerce.Infrastructure.Integration;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ECommerce.Tests.Integration;

[TestClass]
public class Phase8MessageBrokerIntegrationTests
{
    private static IntegrationEventDispatcher CreateDispatcher(IntegrationPersistenceDbContext dbContext, IPublisher mediator)
    {
        return new IntegrationEventDispatcher(
            new InboxIdempotencyProcessor(dbContext),
            mediator,
            NullLogger<IntegrationEventDispatcher>.Instance);
    }

    [TestMethod]
    public async Task ProductProjectionEvent_Consumer_ForwardsToMediator()
    {
        var message = new ProductProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            "Harness Product",
            12.34m,
            false,
            DateTime.UtcNow);

        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"inbox-product-test-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);

        var mediatorMock = new Mock<IPublisher>(MockBehavior.Strict);
        mediatorMock
            .Setup(m => m.Publish(It.IsAny<ProductProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var dispatcher = CreateDispatcher(dbContext, mediatorMock.Object);
        await dispatcher.DispatchAsync(message, CancellationToken.None);

        mediatorMock.Verify(m => m.Publish(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ProductProjectionEvent_Consumer_DuplicateDelivery_ProcessesOnce()
    {
        var idempotencyKey = Guid.NewGuid();
        var message = new ProductProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            "Harness Product",
            12.34m,
            false,
            DateTime.UtcNow)
        {
            IdempotencyKey = idempotencyKey
        };

        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"inbox-product-dup-test-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);

        var mediatorMock = new Mock<IPublisher>(MockBehavior.Strict);
        mediatorMock
            .Setup(m => m.Publish(It.IsAny<ProductProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dispatcher = CreateDispatcher(dbContext, mediatorMock.Object);

        await dispatcher.DispatchAsync(message, CancellationToken.None);
        await dispatcher.DispatchAsync(message, CancellationToken.None);

        mediatorMock.Verify(
            m => m.Publish(It.IsAny<ProductProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var inbox = await dbContext.InboxMessages.SingleAsync(x => x.IdempotencyKey == idempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
    }

    [TestMethod]
    public async Task InventoryProjectionEvent_Consumer_DuplicateDelivery_ProcessesOnce()
    {
        var idempotencyKey = Guid.NewGuid();
        var message = new InventoryStockProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            5,
            "reduce",
            DateTime.UtcNow)
        {
            IdempotencyKey = idempotencyKey
        };

        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"inbox-test-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);

        var mediatorMock = new Mock<IPublisher>(MockBehavior.Strict);
        mediatorMock
            .Setup(m => m.Publish(It.IsAny<InventoryStockProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dispatcher = CreateDispatcher(dbContext, mediatorMock.Object);

        await dispatcher.DispatchAsync(message, CancellationToken.None);
        await dispatcher.DispatchAsync(message, CancellationToken.None);

        mediatorMock.Verify(
            m => m.Publish(It.IsAny<InventoryStockProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var inbox = await dbContext.InboxMessages.SingleAsync(x => x.IdempotencyKey == idempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
    }

    [TestMethod]
    public async Task InboxProcessor_WhenHandlerFails_StoresAttemptAndError()
    {
        var idempotencyKey = Guid.NewGuid();
        var message = new PromoCodeProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            "SAVE20",
            20m,
            true,
            false,
            DateTime.UtcNow)
        {
            IdempotencyKey = idempotencyKey
        };

        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"inbox-failure-test-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);
        var processor = new InboxIdempotencyProcessor(dbContext);

        Func<Task> act = () => processor.ExecuteAsync(
            message,
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await Should.ThrowAsync<InvalidOperationException>(act);

        var inbox = await dbContext.InboxMessages.SingleAsync(x => x.IdempotencyKey == idempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNull(inbox.ProcessedAt);
        Assert.AreEqual("boom", inbox.LastError);
    }

    [TestMethod]
    public async Task InboxProcessor_AfterFailure_RetrySucceedsAndMarksProcessed()
    {
        var idempotencyKey = Guid.NewGuid();
        var message = new PromoCodeProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            "SAVE20",
            20m,
            true,
            false,
            DateTime.UtcNow)
        {
            IdempotencyKey = idempotencyKey
        };

        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"inbox-retry-test-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);
        var processor = new InboxIdempotencyProcessor(dbContext);

        Func<Task> act = () => processor.ExecuteAsync(
            message,
            _ => throw new InvalidOperationException("transient"),
            CancellationToken.None);

        await Should.ThrowAsync<InvalidOperationException>(act);

        await processor.ExecuteAsync(message, _ => Task.CompletedTask, CancellationToken.None);

        var inbox = await dbContext.InboxMessages.SingleAsync(x => x.IdempotencyKey == idempotencyKey);
        Assert.AreEqual(2, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
        Assert.IsNull(inbox.LastError);
    }

    [TestMethod]
    public async Task AddressProjectionEvent_Consumer_DuplicateDelivery_ProcessesOnce()
    {
        var idempotencyKey = Guid.NewGuid();
        var message = new AddressProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Main Street",
            "City",
            "US",
            "12345",
            false,
            DateTime.UtcNow)
        {
            IdempotencyKey = idempotencyKey
        };

        var options = new DbContextOptionsBuilder<IntegrationPersistenceDbContext>()
            .UseInMemoryDatabase($"inbox-address-dup-test-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new IntegrationPersistenceDbContext(options);

        var mediatorMock = new Mock<IPublisher>(MockBehavior.Strict);
        mediatorMock
            .Setup(m => m.Publish(It.IsAny<AddressProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dispatcher = CreateDispatcher(dbContext, mediatorMock.Object);

        await dispatcher.DispatchAsync(message, CancellationToken.None);
        await dispatcher.DispatchAsync(message, CancellationToken.None);

        mediatorMock.Verify(
            m => m.Publish(It.IsAny<AddressProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var inbox = await dbContext.InboxMessages.SingleAsync(x => x.IdempotencyKey == idempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
    }
}
