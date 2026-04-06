using System.Text.Json;
using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OutboxDispatcherReliabilityTests
{
    [TestMethod]
    public async Task Dispatch_WhenPublishFails_SchedulesRetryWithBackoff()
    {
        var bus = new TestIntegrationEventBus(failuresBeforeSuccess: 1);
        using var harness = CreateHarness(bus, "outbox-retry-schedule", new OutboxDispatcherOptions
        {
            MaxRetryAttempts = 5,
            BaseRetryDelaySeconds = 2,
            MaxRetryDelaySeconds = 30,
            BatchSize = 25
        });

        var integrationEvent = CreateProductEvent();
        harness.DbContext.OutboxMessages.Add(CreateOutboxMessage(integrationEvent));
        await harness.DbContext.SaveChangesAsync();

        var before = DateTime.UtcNow;
        await harness.Dispatcher.DispatchOnceAsync(CancellationToken.None);

        var outbox = await harness.DbContext.OutboxMessages.SingleAsync();
        Assert.AreEqual(1, outbox.RetryCount);
        Assert.IsNull(outbox.ProcessedAt);
        Assert.IsFalse(outbox.IsDeadLettered);
        Assert.IsNotNull(outbox.NextAttemptAt);
        Assert.IsTrue(outbox.NextAttemptAt >= before.AddSeconds(2));
    }

    [TestMethod]
    public async Task Dispatch_WhenRetriesExhausted_MovesMessageToDeadLetter()
    {
        var bus = new TestIntegrationEventBus(failuresBeforeSuccess: int.MaxValue);
        using var harness = CreateHarness(bus, "outbox-dead-letter", new OutboxDispatcherOptions
        {
            MaxRetryAttempts = 3,
            BaseRetryDelaySeconds = 1,
            MaxRetryDelaySeconds = 10,
            BatchSize = 25
        });

        var message = CreateOutboxMessage(CreateProductEvent());
        message.RetryCount = 2;

        harness.DbContext.OutboxMessages.Add(message);
        await harness.DbContext.SaveChangesAsync();

        await harness.Dispatcher.DispatchOnceAsync(CancellationToken.None);

        var outbox = await harness.DbContext.OutboxMessages.SingleAsync();
        var deadLetter = await harness.DbContext.DeadLetterMessages.SingleAsync();

        Assert.AreEqual(3, outbox.RetryCount);
        Assert.IsTrue(outbox.IsDeadLettered);
        Assert.IsNotNull(outbox.DeadLetteredAt);
        Assert.IsNull(outbox.NextAttemptAt);

        Assert.AreEqual(outbox.Id, deadLetter.OutboxMessageId);
        Assert.AreEqual(outbox.IdempotencyKey, deadLetter.IdempotencyKey);
        Assert.AreEqual(3, deadLetter.RetryCount);
        Assert.IsNotNull(deadLetter.LastError);
    }

    [TestMethod]
    public async Task Dispatch_WhenPublishSucceeds_MarksMessageProcessed()
    {
        var bus = new TestIntegrationEventBus(failuresBeforeSuccess: 0);
        using var harness = CreateHarness(bus, "outbox-success", new OutboxDispatcherOptions
        {
            MaxRetryAttempts = 3,
            BaseRetryDelaySeconds = 1,
            MaxRetryDelaySeconds = 10,
            BatchSize = 25
        });

        harness.DbContext.OutboxMessages.Add(CreateOutboxMessage(CreateProductEvent()));
        await harness.DbContext.SaveChangesAsync();

        await harness.Dispatcher.DispatchOnceAsync(CancellationToken.None);

        var outbox = await harness.DbContext.OutboxMessages.SingleAsync();

        Assert.AreEqual(1, bus.PublishCount);
        Assert.IsNotNull(outbox.ProcessedAt);
        Assert.AreEqual(0, outbox.RetryCount);
        Assert.IsNull(outbox.LastError);
        Assert.IsFalse(outbox.IsDeadLettered);
    }

    private static DispatcherHarness CreateHarness(
        IIntegrationEventBus integrationEventBus,
        string databasePrefix,
        OutboxDispatcherOptions options)
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"{databasePrefix}-{Guid.NewGuid():N}")
            .Options;

        var dbContext = new AppDbContext(dbOptions);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddSingleton(integrationEventBus);

        var rootProvider = services.BuildServiceProvider();
        var scopeFactory = new FixedServiceScopeFactory(rootProvider);

        var dispatcher = new OutboxDispatcherHostedService(
            scopeFactory,
            Options.Create(options),
            NullLogger<OutboxDispatcherHostedService>.Instance);

        return new DispatcherHarness(dbContext, rootProvider, dispatcher);
    }

    private static ProductProjectionUpdatedIntegrationEvent CreateProductEvent()
        => new(Guid.NewGuid(), "Test Product", 9.99m, false, DateTime.UtcNow);

    private static OutboxMessage CreateOutboxMessage(IntegrationEvent integrationEvent)
        => new()
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = integrationEvent.IdempotencyKey,
            EventType = integrationEvent.GetType().AssemblyQualifiedName
                       ?? integrationEvent.GetType().FullName
                       ?? integrationEvent.GetType().Name,
            EventData = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            CreatedAt = DateTime.UtcNow
        };

    private sealed class DispatcherHarness(
        AppDbContext dbContext,
        ServiceProvider rootProvider,
        OutboxDispatcherHostedService dispatcher) : IDisposable
    {
        public AppDbContext DbContext { get; } = dbContext;

        public OutboxDispatcherHostedService Dispatcher { get; } = dispatcher;

        public void Dispose()
        {
            DbContext.Dispose();
            rootProvider.Dispose();
        }
    }

    private sealed class TestIntegrationEventBus(int failuresBeforeSuccess) : IIntegrationEventBus
    {
        private int _remainingFailures = failuresBeforeSuccess;

        public int PublishCount { get; private set; }

        public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            _ = integrationEvent;
            _ = cancellationToken;
            PublishCount++;

            if (_remainingFailures > 0)
            {
                _remainingFailures--;
                throw new InvalidOperationException("simulated publish failure");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FixedServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FixedServiceScope(serviceProvider);
    }

    private sealed class FixedServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public void Dispose()
        {
        }
    }
}
