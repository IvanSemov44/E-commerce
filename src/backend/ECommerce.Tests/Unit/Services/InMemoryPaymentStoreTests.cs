using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Services;
using FluentAssertions;

namespace ECommerce.Tests.Unit.Services;

/// <summary>
/// Unit tests for InMemoryPaymentStore.
/// Tests thread-safe in-memory payment storage operations.
/// </summary>
[TestClass]
public class InMemoryPaymentStoreTests
{
    private InMemoryPaymentStore _store = null!;

    [TestInitialize]
    public void Setup()
    {
        _store = new InMemoryPaymentStore();
    }

    #region StorePaymentAsync Tests

    [TestMethod]
    public async Task StorePaymentAsync_WithValidData_StoresPayment()
    {
        // Arrange
        var paymentId = "payment-123";
        var details = CreatePaymentDetails();

        // Act
        await _store.StorePaymentAsync(paymentId, details);

        // Assert
        var result = await _store.GetPaymentAsync(paymentId);
        result.Should().NotBeNull();
        result!.PaymentIntentId.Should().Be(details.PaymentIntentId);
    }

    [TestMethod]
    public async Task StorePaymentAsync_OverwritesExistingPayment()
    {
        // Arrange
        var paymentId = "payment-123";
        var originalDetails = CreatePaymentDetails(orderId: Guid.NewGuid());
        var updatedDetails = CreatePaymentDetails(orderId: Guid.NewGuid());

        // Act
        await _store.StorePaymentAsync(paymentId, originalDetails);
        await _store.StorePaymentAsync(paymentId, updatedDetails);

        // Assert
        var result = await _store.GetPaymentAsync(paymentId);
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(updatedDetails.OrderId);
    }

    [TestMethod]
    public async Task StorePaymentAsync_WithMultiplePayments_StoresAll()
    {
        // Arrange
        var payment1 = ("payment-1", CreatePaymentDetails(orderId: Guid.NewGuid()));
        var payment2 = ("payment-2", CreatePaymentDetails(orderId: Guid.NewGuid()));
        var payment3 = ("payment-3", CreatePaymentDetails(orderId: Guid.NewGuid()));

        // Act
        await _store.StorePaymentAsync(payment1.Item1, payment1.Item2);
        await _store.StorePaymentAsync(payment2.Item1, payment2.Item2);
        await _store.StorePaymentAsync(payment3.Item1, payment3.Item2);

        // Assert
        (await _store.GetPaymentAsync("payment-1")).Should().NotBeNull();
        (await _store.GetPaymentAsync("payment-2")).Should().NotBeNull();
        (await _store.GetPaymentAsync("payment-3")).Should().NotBeNull();
    }

    #endregion

    #region GetPaymentAsync Tests

    [TestMethod]
    public async Task GetPaymentAsync_WithExistingPayment_ReturnsPayment()
    {
        // Arrange
        var paymentId = "payment-123";
        var details = CreatePaymentDetails();
        await _store.StorePaymentAsync(paymentId, details);

        // Act
        var result = await _store.GetPaymentAsync(paymentId);

        // Assert
        result.Should().NotBeNull();
        result!.PaymentIntentId.Should().Be(details.PaymentIntentId);
        result.OrderId.Should().Be(details.OrderId);
        result.Amount.Should().Be(details.Amount);
        result.Currency.Should().Be(details.Currency);
        result.Status.Should().Be(details.Status);
    }

    [TestMethod]
    public async Task GetPaymentAsync_WithNonExistingPayment_ReturnsNull()
    {
        // Arrange
        var paymentId = "non-existing-payment";

        // Act
        var result = await _store.GetPaymentAsync(paymentId);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetPaymentAsync_AfterRemoval_ReturnsNull()
    {
        // Arrange
        var paymentId = "payment-123";
        var details = CreatePaymentDetails();
        await _store.StorePaymentAsync(paymentId, details);
        await _store.RemovePaymentAsync(paymentId);

        // Act
        var result = await _store.GetPaymentAsync(paymentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RemovePaymentAsync Tests

    [TestMethod]
    public async Task RemovePaymentAsync_WithExistingPayment_RemovesPayment()
    {
        // Arrange
        var paymentId = "payment-123";
        var details = CreatePaymentDetails();
        await _store.StorePaymentAsync(paymentId, details);

        // Act
        await _store.RemovePaymentAsync(paymentId);

        // Assert
        var result = await _store.GetPaymentAsync(paymentId);
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task RemovePaymentAsync_WithNonExistingPayment_DoesNotThrow()
    {
        // Arrange
        var paymentId = "non-existing-payment";

        // Act
        var action = async () => await _store.RemovePaymentAsync(paymentId);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task RemovePaymentAsync_DoesNotAffectOtherPayments()
    {
        // Arrange
        var payment1 = ("payment-1", CreatePaymentDetails(orderId: Guid.NewGuid()));
        var payment2 = ("payment-2", CreatePaymentDetails(orderId: Guid.NewGuid()));
        await _store.StorePaymentAsync(payment1.Item1, payment1.Item2);
        await _store.StorePaymentAsync(payment2.Item1, payment2.Item2);

        // Act
        await _store.RemovePaymentAsync(payment1.Item1);

        // Assert
        (await _store.GetPaymentAsync(payment1.Item1)).Should().BeNull();
        (await _store.GetPaymentAsync(payment2.Item1)).Should().NotBeNull();
    }

    #endregion

    #region Thread Safety Tests

    [TestMethod]
    public async Task StorePaymentAsync_ConcurrentWrites_HandlesThreadSafety()
    {
        // Arrange
        var tasks = new List<Task>();
        var paymentCount = 100;

        // Act - Concurrent writes
        for (int i = 0; i < paymentCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await _store.StorePaymentAsync($"payment-{index}", CreatePaymentDetails(orderId: Guid.NewGuid()));
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All payments should be stored
        for (int i = 0; i < paymentCount; i++)
        {
            var result = await _store.GetPaymentAsync($"payment-{i}");
            result.Should().NotBeNull($"payment-{i} should exist");
        }
    }

    [TestMethod]
    public async Task GetPaymentAsync_ConcurrentReads_HandlesThreadSafety()
    {
        // Arrange
        var paymentId = "payment-concurrent-read";
        var details = CreatePaymentDetails();
        await _store.StorePaymentAsync(paymentId, details);

        var tasks = new List<Task<PaymentDetailsDto?>>();
        var readCount = 50;

        // Act - Concurrent reads
        for (int i = 0; i < readCount; i++)
        {
            tasks.Add(Task.Run(() => _store.GetPaymentAsync(paymentId)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All reads should return the same payment
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r!.PaymentIntentId.Should().Be(details.PaymentIntentId);
        });
    }

    [TestMethod]
    public async Task MixedOperations_Concurrent_HandlesThreadSafety()
    {
        // Arrange
        var tasks = new List<Task>();
        var operationCount = 100;

        // Act - Mix of store, get, and remove operations
        for (int i = 0; i < operationCount; i++)
        {
            var index = i;
            var operation = index % 3;

            tasks.Add(Task.Run(async () =>
            {
                var paymentId = $"payment-{index % 20}"; // Reuse some IDs to create conflicts

                switch (operation)
                {
                    case 0:
                        await _store.StorePaymentAsync(paymentId, CreatePaymentDetails(orderId: Guid.NewGuid()));
                        break;
                    case 1:
                        await _store.GetPaymentAsync(paymentId);
                        break;
                    case 2:
                        await _store.RemovePaymentAsync(paymentId);
                        break;
                }
            }));
        }

        // Act & Assert - Should not throw any exceptions
        var action = async () => await Task.WhenAll(tasks);
        await action.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private static PaymentDetailsDto CreatePaymentDetails(Guid? orderId = null)
    {
        return new PaymentDetailsDto
        {
            PaymentIntentId = Guid.NewGuid().ToString(),
            OrderId = orderId ?? Guid.NewGuid(),
            Amount = 99.99m,
            Currency = "USD",
            Status = "Pending",
            PaymentMethod = "CreditCard",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
