using ECommerce.Payments.Application.Commands.ProcessPayment;
using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Errors;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Payments.Tests.Application.Handlers;

[TestClass]
public class ProcessPaymentCommandHandlerTests
{
    private Mock<IPaymentRepository> _paymentRepo = null!;
    private Mock<IPaymentOrderQuery> _orderQuery = null!;
    private Mock<IPaymentGateway> _gateway = null!;
    private Mock<IPaymentStore> _paymentStore = null!;
    private Mock<IIdempotencyStore> _idempotency = null!;
    private ProcessPaymentCommandHandler _handler = null!;

    private static readonly Guid _orderId = Guid.NewGuid();
    private static readonly string _idempotencyKey = Guid.NewGuid().ToString();

    [TestInitialize]
    public void Setup()
    {
        _paymentRepo = new Mock<IPaymentRepository>();
        _orderQuery = new Mock<IPaymentOrderQuery>();
        _gateway = new Mock<IPaymentGateway>();
        _paymentStore = new Mock<IPaymentStore>();
        _idempotency = new Mock<IIdempotencyStore>();

        _handler = new ProcessPaymentCommandHandler(
            _paymentRepo.Object,
            _orderQuery.Object,
            _gateway.Object,
            _paymentStore.Object,
            _idempotency.Object,
            NullLogger<ProcessPaymentCommandHandler>.Instance);

        // Default: idempotency key acquired (not a replay)
        _idempotency
            .Setup(s => s.StartAsync<PaymentResponseDto>(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyStartResult<PaymentResponseDto>(IdempotencyStartStatus.Acquired));

        _idempotency
            .Setup(s => s.CompleteAsync(It.IsAny<string>(), It.IsAny<PaymentResponseDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _idempotency
            .Setup(s => s.AbandonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentStore
            .Setup(s => s.StorePaymentAsync(It.IsAny<string>(), It.IsAny<PaymentDetailsDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentRepo
            .Setup(r => r.AddAsync(It.IsAny<ECommerce.Payments.Domain.Aggregates.Payment.Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ProcessPaymentCommand BuildCommand(
        Guid? orderId = null,
        string method = "credit_card",
        decimal amount = 100m,
        string? key = null)
        => new(
            new ProcessPaymentDto
            {
                OrderId = orderId ?? _orderId,
                PaymentMethod = method,
                Amount = amount,
                CardToken = "tok_test"
            },
            key ?? _idempotencyKey);

    // ── Happy path ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_HappyPath_ReturnsSuccess()
    {
        _orderQuery
            .Setup(q => q.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrderSnapshot(_orderId, 100m, Guid.NewGuid()));

        _gateway
            .Setup(g => g.ChargeAsync("credit_card", 100m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayChargeResult(true, "pi_123", "txn_456", "mock", null));

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().Success.ShouldBeTrue();
        result.GetDataOrThrow().PaymentIntentId.ShouldBe("pi_123");
    }

    [TestMethod]
    public async Task Handle_HappyPath_SavesPaymentEntity()
    {
        _orderQuery
            .Setup(q => q.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrderSnapshot(_orderId, 100m, Guid.NewGuid()));

        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayChargeResult(true, "pi_1", "txn_1", "mock", null));

        await _handler.Handle(BuildCommand(), CancellationToken.None);

        _paymentRepo.Verify(r => r.AddAsync(It.IsAny<ECommerce.Payments.Domain.Aggregates.Payment.Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Charge fails ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_ChargeFails_ReturnsOkWithFailedStatus()
    {
        _orderQuery
            .Setup(q => q.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrderSnapshot(_orderId, 100m, Guid.NewGuid()));

        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayChargeResult(false, string.Empty, string.Empty, "mock", "Insufficient funds"));

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().Success.ShouldBeFalse();
        result.GetDataOrThrow().Status.ShouldBe("failed");
    }

    [TestMethod]
    public async Task Handle_ChargeFails_StillSavesPaymentEntity()
    {
        _orderQuery
            .Setup(q => q.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrderSnapshot(_orderId, 100m, Guid.NewGuid()));

        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayChargeResult(false, string.Empty, string.Empty, "mock", "Card expired"));

        await _handler.Handle(BuildCommand(), CancellationToken.None);

        _paymentRepo.Verify(r => r.AddAsync(It.IsAny<ECommerce.Payments.Domain.Aggregates.Payment.Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Order not found ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_OrderNotFound_ReturnsOrderNotFoundError()
    {
        _orderQuery
            .Setup(q => q.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentOrderSnapshot?)null);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentsApplicationErrors.OrderNotFound);
    }

    [TestMethod]
    public async Task Handle_OrderNotFound_AbandonsIdempotencyKey()
    {
        _orderQuery
            .Setup(q => q.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentOrderSnapshot?)null);

        await _handler.Handle(BuildCommand(), CancellationToken.None);

        _idempotency.Verify(s => s.AbandonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Amount mismatch ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_AmountMismatch_ReturnsError()
    {
        _orderQuery
            .Setup(q => q.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrderSnapshot(_orderId, 200m, Guid.NewGuid())); // order is 200m

        var result = await _handler.Handle(BuildCommand(amount: 100m), CancellationToken.None); // command says 100m

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentsApplicationErrors.PaymentAmountMismatch);
    }

    // ── Idempotency ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_IdempotencyReplay_ReturnsCachedResponse()
    {
        var cached = new PaymentResponseDto { Success = true, PaymentIntentId = "pi_cached", Status = "completed", Amount = 100m };
        _idempotency
            .Setup(s => s.StartAsync<PaymentResponseDto>(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyStartResult<PaymentResponseDto>(IdempotencyStartStatus.Replay, cached));

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().PaymentIntentId.ShouldBe("pi_cached");
        _gateway.Verify(g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_IdempotencyInProgress_ReturnsConflictError()
    {
        _idempotency
            .Setup(s => s.StartAsync<PaymentResponseDto>(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyStartResult<PaymentResponseDto>(IdempotencyStartStatus.InProgress));

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentsApplicationErrors.IdempotencyInProgress);
    }

    // ── Validation ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_InvalidIdempotencyKey_ReturnsError()
    {
        var result = await _handler.Handle(BuildCommand(key: "not-a-uuid"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentsApplicationErrors.InvalidIdempotencyKey);
    }

    [TestMethod]
    public async Task Handle_UnsupportedPaymentMethod_ReturnsError()
    {
        _orderQuery
            .Setup(q => q.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrderSnapshot(_orderId, 100m, Guid.NewGuid()));

        var result = await _handler.Handle(BuildCommand(method: "bitcoin"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().ShouldBe(PaymentsApplicationErrors.UnsupportedPaymentMethod);
    }
}
