using ECommerce.Payments.Domain.Aggregates.Payment;
using ECommerce.Payments.Domain.Enums;
using ECommerce.Payments.Domain.Errors;
using ECommerce.Payments.Domain.Events;

namespace ECommerce.Payments.Tests.Domain;

[TestClass]
public class PaymentAggregateTests
{
    private static Payment CreateProcessingPayment()
        => Payment.Initiate(Guid.NewGuid(), "credit_card", 100m);

    // ── Initiate ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Initiate_SetsStatusToProcessing()
    {
        var payment = CreateProcessingPayment();

        payment.Status.ShouldBe(PaymentStatus.Processing);
    }

    [TestMethod]
    public void Initiate_StoresProperties()
    {
        var orderId = Guid.NewGuid();
        var payment = Payment.Initiate(orderId, "paypal", 250m, "EUR");

        payment.OrderId.ShouldBe(orderId);
        payment.PaymentMethod.ShouldBe("paypal");
        payment.Amount.ShouldBe(250m);
        payment.Currency.ShouldBe("EUR");
    }

    [TestMethod]
    public void Initiate_DefaultCurrencyIsUsd()
    {
        var payment = Payment.Initiate(Guid.NewGuid(), "stripe", 50m);

        payment.Currency.ShouldBe("USD");
    }

    // ── MarkPaid ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void MarkPaid_FromProcessing_Succeeds()
    {
        var payment = CreateProcessingPayment();

        var result = payment.MarkPaid("pi_123", "txn_456");

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Paid);
        payment.PaymentIntentId.ShouldBe("pi_123");
        payment.TransactionId.ShouldBe("txn_456");
        payment.ProcessedAt.ShouldNotBeNull();
    }

    [TestMethod]
    public void MarkPaid_RaisesPaymentProcessedEvent()
    {
        var payment = CreateProcessingPayment();

        payment.MarkPaid("pi_123", "txn_456");

        var events = payment.DomainEvents;
        events.ShouldHaveSingleItem();
        events.Single().ShouldBeOfType<PaymentProcessedEvent>();
    }

    [TestMethod]
    public void MarkPaid_FromPaid_ReturnsInvalidTransition()
    {
        var payment = CreateProcessingPayment();
        payment.MarkPaid("pi_1", "txn_1");

        var result = payment.MarkPaid("pi_2", "txn_2");

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentErrors.InvalidTransition);
    }

    [TestMethod]
    public void MarkPaid_FromFailed_ReturnsInvalidTransition()
    {
        var payment = CreateProcessingPayment();
        payment.MarkFailed("declined");

        var result = payment.MarkPaid("pi_1", "txn_1");

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentErrors.InvalidTransition);
    }

    // ── MarkFailed ───────────────────────────────────────────────────────────

    [TestMethod]
    public void MarkFailed_FromProcessing_Succeeds()
    {
        var payment = CreateProcessingPayment();

        var result = payment.MarkFailed("Card declined");

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Failed);
        payment.FailureReason.ShouldBe("Card declined");
        payment.ProcessedAt.ShouldNotBeNull();
    }

    [TestMethod]
    public void MarkFailed_RaisesPaymentFailedEvent()
    {
        var payment = CreateProcessingPayment();

        payment.MarkFailed("Insufficient funds");

        var events = payment.DomainEvents;
        events.ShouldHaveSingleItem();
        events.Single().ShouldBeOfType<PaymentFailedEvent>();
    }

    [TestMethod]
    public void MarkFailed_FromPaid_ReturnsInvalidTransition()
    {
        var payment = CreateProcessingPayment();
        payment.MarkPaid("pi_1", "txn_1");

        var result = payment.MarkFailed("some reason");

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentErrors.InvalidTransition);
    }

    // ── Refund ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Refund_FromPaid_Succeeds()
    {
        var payment = CreateProcessingPayment();
        payment.MarkPaid("pi_1", "txn_1");

        var result = payment.Refund();

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Refunded);
    }

    [TestMethod]
    public void Refund_RaisesPaymentRefundedEvent()
    {
        var payment = CreateProcessingPayment();
        payment.MarkPaid("pi_1", "txn_1");

        payment.Refund();

        var events = payment.DomainEvents;
        var refundedEvent = events.OfType<PaymentRefundedEvent>().Single();
        refundedEvent.Amount.ShouldBe(payment.Amount);
    }

    [TestMethod]
    public void Refund_FromProcessing_ReturnsInvalidRefund()
    {
        var payment = CreateProcessingPayment();

        var result = payment.Refund();

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentErrors.InvalidRefund);
    }

    [TestMethod]
    public void Refund_FromFailed_ReturnsInvalidRefund()
    {
        var payment = CreateProcessingPayment();
        payment.MarkFailed("declined");

        var result = payment.Refund();

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentErrors.InvalidRefund);
    }
}
