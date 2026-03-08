using FluentValidation.TestHelper;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Validators.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Validators;

[TestClass]
public class PaymentValidatorsTests
{
    private ProcessPaymentDtoValidator _processValidator = null!;
    private RefundPaymentDtoValidator _refundValidator = null!;
    private PaymentWebhookDtoValidator _webhookValidator = null!;

    [TestInitialize]
    public void Setup()
    {
        _processValidator = new ProcessPaymentDtoValidator();
        _refundValidator = new RefundPaymentDtoValidator();
        _webhookValidator = new PaymentWebhookDtoValidator();
    }

    [TestMethod]
    public void ProcessPayment_Should_Have_Errors_On_Invalid_Dto()
    {
        var dto = new ProcessPaymentDto { OrderId = Guid.Empty, PaymentMethod = "", Amount = 0 };
        var result = _processValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [TestMethod]
    public void ProcessPayment_Should_Require_CardToken_For_CreditCard()
    {
        var dto = new ProcessPaymentDto { OrderId = Guid.NewGuid(), PaymentMethod = "credit_card", Amount = 10.0M, CardToken = "" };
        var result = _processValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CardToken);
    }

    [TestMethod]
    public void ProcessPayment_Should_Pass_For_Valid_Paypal()
    {
        var dto = new ProcessPaymentDto { OrderId = Guid.NewGuid(), PaymentMethod = "paypal", Amount = 5.5M, PayPalEmail = "buyer@example.com" };
        var result = _processValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Refund_Should_Have_Error_When_Invalid()
    {
        var dto = new RefundPaymentDto { OrderId = Guid.Empty, Amount = -5M };
        var result = _refundValidator.TestValidate(dto);
        // OrderId is set from route parameter by controller, not validated here
        // Only Amount is validated (must be positive)
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [TestMethod]
    public void Refund_Should_Pass_For_Valid()
    {
        var dto = new RefundPaymentDto { OrderId = Guid.NewGuid(), Amount = 10M, Reason = "Customer requested refund" };
        var result = _refundValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void PaymentWebhook_Should_Have_Errors_On_Invalid_Dto()
    {
        var dto = new PaymentWebhookDto
        {
            EventType = string.Empty,
            PaymentIntentId = null,
            Amount = -1,
            Status = null,
            Currency = "US",
            Timestamp = 0
        };

        var result = _webhookValidator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.EventType);
        result.ShouldHaveValidationErrorFor(x => x.PaymentIntentId);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
        result.ShouldHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    [TestMethod]
    public void PaymentWebhook_Should_Pass_For_Valid_Dto()
    {
        var dto = new PaymentWebhookDto
        {
            EventType = "payment_intent.succeeded",
            PaymentIntentId = "pi_123456789",
            Amount = 125.50m,
            Status = "succeeded",
            Currency = "USD",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var result = _webhookValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
