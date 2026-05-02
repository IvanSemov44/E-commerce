using ECommerce.Contracts;
using ECommerce.Payments.Domain.Events;
using ECommerce.Payments.Infrastructure.EventHandlers;
using ECommerce.Payments.Infrastructure.Integration;

namespace ECommerce.Payments.Tests.Infrastructure.EventHandlers;

[TestClass]
public class PaymentProcessedEventHandlerTests
{
    private Mock<IPaymentsOutboxEventWriter> _outboxWriter = null!;
    private PaymentProcessedEventHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _outboxWriter = new Mock<IPaymentsOutboxEventWriter>();
        _outboxWriter
            .Setup(w => w.EnqueueAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new PaymentProcessedEventHandler(_outboxWriter.Object);
    }

    [TestMethod]
    public async Task Handle_EnqueuesPaymentProcessedIntegrationEvent()
    {
        var notification = new PaymentProcessedEvent(Guid.NewGuid(), Guid.NewGuid(), 150m, "credit_card");

        await _handler.Handle(notification, CancellationToken.None);

        _outboxWriter.Verify(
            w => w.EnqueueAsync(It.IsAny<PaymentProcessedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_EnqueuedEventCarriesCorrectData()
    {
        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        const decimal amount = 75.50m;
        const string method = "paypal";

        PaymentProcessedIntegrationEvent? captured = null;
        _outboxWriter
            .Setup(w => w.EnqueueAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEvent, CancellationToken>((e, _) => captured = e as PaymentProcessedIntegrationEvent)
            .Returns(Task.CompletedTask);

        var notification = new PaymentProcessedEvent(paymentId, orderId, amount, method);

        await _handler.Handle(notification, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.PaymentId.ShouldBe(paymentId);
        captured.OrderId.ShouldBe(orderId);
        captured.Amount.ShouldBe(amount);
        captured.PaymentMethod.ShouldBe(method);
    }

    [TestMethod]
    public async Task Handle_CorrelationIdIsSetToOrderId()
    {
        var orderId = Guid.NewGuid();

        PaymentProcessedIntegrationEvent? captured = null;
        _outboxWriter
            .Setup(w => w.EnqueueAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEvent, CancellationToken>((e, _) => captured = e as PaymentProcessedIntegrationEvent)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new PaymentProcessedEvent(Guid.NewGuid(), orderId, 50m, "stripe"), CancellationToken.None);

        captured!.CorrelationId.ShouldBe(orderId);
    }
}
