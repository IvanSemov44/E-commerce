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

    [TestInitialize]
    public void Setup()
    {
        _processValidator = new ProcessPaymentDtoValidator();
        _refundValidator = new RefundPaymentDtoValidator();
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
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [TestMethod]
    public void Refund_Should_Pass_For_Valid()
    {
        var dto = new RefundPaymentDto { OrderId = Guid.NewGuid(), Amount = 10M, Reason = "Customer requested refund" };
        var result = _refundValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
