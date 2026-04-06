using ECommerce.Contracts;
using ECommerce.Infrastructure.Integration;
using ECommerce.Infrastructure.Data;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ECommerce.Tests.Integration;

[TestClass]
public class Phase8MessageBrokerIntegrationTests
{
    [TestMethod]
    public async Task ProductProjectionEvent_Consumer_ForwardsToMediator()
    {
        var message = new ProductProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(),
            "Harness Product",
            12.34m,
            false,
            DateTime.UtcNow);

        var mediatorMock = new Mock<IPublisher>(MockBehavior.Strict);
        mediatorMock
            .Setup(m => m.Publish(It.IsAny<ProductProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var contextMock = new Mock<ConsumeContext<ProductProjectionUpdatedIntegrationEvent>>(MockBehavior.Strict);
        contextMock.SetupGet(c => c.Message).Returns(message);
        contextMock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var consumer = new ProductProjectionUpdatedIntegrationEventConsumer(mediatorMock.Object);
        await consumer.Consume(contextMock.Object);

        mediatorMock.Verify(m => m.Publish(message, It.IsAny<CancellationToken>()), Times.Once);
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

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"inbox-test-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new AppDbContext(options);

        var mediatorMock = new Mock<IPublisher>(MockBehavior.Strict);
        mediatorMock
            .Setup(m => m.Publish(It.IsAny<InventoryStockProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var contextMock = new Mock<ConsumeContext<InventoryStockProjectionUpdatedIntegrationEvent>>(MockBehavior.Strict);
        contextMock.SetupGet(c => c.Message).Returns(message);
        contextMock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var consumer = new InventoryStockProjectionUpdatedIntegrationEventConsumer(
            dbContext,
            mediatorMock.Object,
            NullLogger<InventoryStockProjectionUpdatedIntegrationEventConsumer>.Instance);

        await consumer.Consume(contextMock.Object);
        await consumer.Consume(contextMock.Object);

        mediatorMock.Verify(
            m => m.Publish(It.IsAny<InventoryStockProjectionUpdatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var inbox = await dbContext.InboxMessages.SingleAsync(x => x.IdempotencyKey == idempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
    }
}
