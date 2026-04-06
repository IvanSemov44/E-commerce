using ECommerce.Contracts;
using ECommerce.Infrastructure.Integration;
using MassTransit;
using MediatR;
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
}
