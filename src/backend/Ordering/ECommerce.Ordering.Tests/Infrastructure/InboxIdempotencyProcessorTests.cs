using ECommerce.Contracts;
using ECommerce.Infrastructure.Integration;
using ECommerce.Ordering.Infrastructure.Integration;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Ordering.Tests.Infrastructure;

public sealed class FakePublisher : IPublisher
{
    public List<object> Published { get; } = new();

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        Published.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        Published.Add(notification!);
        return Task.CompletedTask;
    }
}

[TestClass]
public class InboxIdempotencyProcessorTests
{
    private static IntegrationEventDispatcher CreateDispatcher(OrderingDbContext dbContext, IPublisher publisher) =>
        new(new InboxIdempotencyProcessor(dbContext), publisher, NullLogger<IntegrationEventDispatcher>.Instance);

    private static DbContextOptions<OrderingDbContext> InMemoryOptions() =>
        new DbContextOptionsBuilder<OrderingDbContext>()
            .UseInMemoryDatabase($"inbox-test-{Guid.NewGuid():N}")
            .Options;

    [TestMethod]
    public async Task ProductProjectionEvent_ForwardsToPublisher()
    {
        var message = new ProductProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(), "Harness Product", 12.34m, false, DateTime.UtcNow);

        await using var db = new OrderingDbContext(InMemoryOptions());
        var publisher = new FakePublisher();

        await CreateDispatcher(db, publisher).DispatchAsync(message, CancellationToken.None);

        publisher.Published.Count.ShouldBe(1);
        publisher.Published[0].ShouldBe(message);
    }

    [TestMethod]
    public async Task ProductProjectionEvent_DuplicateDelivery_PublishedOnce()
    {
        var message = new ProductProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(), "Harness Product", 12.34m, false, DateTime.UtcNow)
        { IdempotencyKey = Guid.NewGuid() };

        await using var db = new OrderingDbContext(InMemoryOptions());
        var publisher = new FakePublisher();
        var dispatcher = CreateDispatcher(db, publisher);

        await dispatcher.DispatchAsync(message, CancellationToken.None);
        await dispatcher.DispatchAsync(message, CancellationToken.None);

        publisher.Published.Count.ShouldBe(1);
        var inbox = await db.InboxMessages.SingleAsync(x => x.IdempotencyKey == message.IdempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
    }

    [TestMethod]
    public async Task InventoryProjectionEvent_DuplicateDelivery_PublishedOnce()
    {
        var message = new InventoryStockProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(), 5, "reduce", DateTime.UtcNow)
        { IdempotencyKey = Guid.NewGuid() };

        await using var db = new OrderingDbContext(InMemoryOptions());
        var publisher = new FakePublisher();
        var dispatcher = CreateDispatcher(db, publisher);

        await dispatcher.DispatchAsync(message, CancellationToken.None);
        await dispatcher.DispatchAsync(message, CancellationToken.None);

        publisher.Published.Count.ShouldBe(1);
        var inbox = await db.InboxMessages.SingleAsync(x => x.IdempotencyKey == message.IdempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
    }

    [TestMethod]
    public async Task InboxProcessor_WhenHandlerFails_StoresAttemptAndError()
    {
        var message = new PromoCodeProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(), "SAVE20", 20m, true, false, DateTime.UtcNow)
        { IdempotencyKey = Guid.NewGuid() };

        await using var db = new OrderingDbContext(InMemoryOptions());
        var processor = new InboxIdempotencyProcessor(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => processor.ExecuteAsync(message, _ => throw new InvalidOperationException("boom"), CancellationToken.None));

        var inbox = await db.InboxMessages.SingleAsync(x => x.IdempotencyKey == message.IdempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNull(inbox.ProcessedAt);
        Assert.AreEqual("boom", inbox.LastError);
    }

    [TestMethod]
    public async Task InboxProcessor_AfterFailure_RetrySucceedsAndMarksProcessed()
    {
        var message = new PromoCodeProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(), "SAVE20", 20m, true, false, DateTime.UtcNow)
        { IdempotencyKey = Guid.NewGuid() };

        await using var db = new OrderingDbContext(InMemoryOptions());
        var processor = new InboxIdempotencyProcessor(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => processor.ExecuteAsync(message, _ => throw new InvalidOperationException("transient"), CancellationToken.None));

        await processor.ExecuteAsync(message, _ => Task.CompletedTask, CancellationToken.None);

        var inbox = await db.InboxMessages.SingleAsync(x => x.IdempotencyKey == message.IdempotencyKey);
        Assert.AreEqual(2, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
        Assert.IsNull(inbox.LastError);
    }

    [TestMethod]
    public async Task AddressProjectionEvent_DuplicateDelivery_PublishedOnce()
    {
        var message = new AddressProjectionUpdatedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Main Street", "City", "US", "12345", false, DateTime.UtcNow)
        { IdempotencyKey = Guid.NewGuid() };

        await using var db = new OrderingDbContext(InMemoryOptions());
        var publisher = new FakePublisher();
        var dispatcher = CreateDispatcher(db, publisher);

        await dispatcher.DispatchAsync(message, CancellationToken.None);
        await dispatcher.DispatchAsync(message, CancellationToken.None);

        publisher.Published.Count.ShouldBe(1);
        var inbox = await db.InboxMessages.SingleAsync(x => x.IdempotencyKey == message.IdempotencyKey);
        Assert.AreEqual(1, inbox.AttemptCount);
        Assert.IsNotNull(inbox.ProcessedAt);
    }
}
