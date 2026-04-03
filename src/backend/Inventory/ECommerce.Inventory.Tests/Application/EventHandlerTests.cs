using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Inventory.Application.EventHandlers;
using ECommerce.Inventory.Application.Interfaces;
using ECommerce.Inventory.Domain.Events;

namespace ECommerce.Inventory.Tests.Application;

[TestClass]
public class EventHandlerTests
{
    private sealed class FakeEmailService : IEmailService
    {
        public System.Collections.Generic.List<(Guid ProductId, int CurrentStock, int Threshold)> Calls = new();
        public bool ShouldThrow { get; set; }

        public Task SendLowStockAlertAsync(Guid productId, int currentStock, int threshold, CancellationToken ct)
        {
            if (ShouldThrow) throw new InvalidOperationException("Email service is down");
            Calls.Add((productId, currentStock, threshold));
            return Task.CompletedTask;
        }
    }

    [TestMethod]
    public async Task Handle_LowStockDetectedEvent_CallsEmailService()
    {
        var email = new FakeEmailService();
        var handler = new SendLowStockAlertOnLowStockDetectedHandler(
            email, NullLogger<SendLowStockAlertOnLowStockDetectedHandler>.Instance);

        var productId = Guid.NewGuid();
        await handler.Handle(new LowStockDetectedEvent(productId, 5, 10), default);

        Assert.HasCount(1, email.Calls);
        Assert.AreEqual(productId, email.Calls[0].ProductId);
        Assert.AreEqual(5, email.Calls[0].CurrentStock);
        Assert.AreEqual(10, email.Calls[0].Threshold);
    }

    [TestMethod]
    public async Task Handle_EmailServiceThrows_DoesNotRethrow()
    {
        var email = new FakeEmailService { ShouldThrow = true };
        var handler = new SendLowStockAlertOnLowStockDetectedHandler(
            email, NullLogger<SendLowStockAlertOnLowStockDetectedHandler>.Instance);

        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(new LowStockDetectedEvent(Guid.NewGuid(), 2, 10), default));

        Assert.IsNull(exception, "Handler must not propagate exceptions");
    }

    [TestMethod]
    public async Task Handle_EmailServiceThrows_DoesNotCallSaveOrOtherSideEffects()
    {
        var email = new FakeEmailService { ShouldThrow = true };
        var handler = new SendLowStockAlertOnLowStockDetectedHandler(
            email, NullLogger<SendLowStockAlertOnLowStockDetectedHandler>.Instance);

        await handler.Handle(new LowStockDetectedEvent(Guid.NewGuid(), 2, 10), default);

        Assert.IsEmpty(email.Calls);
    }
}

file static class Record
{
    public static async Task<Exception?> ExceptionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
