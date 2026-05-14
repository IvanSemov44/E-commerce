using System.Text.Json;
using ECommerce.Contracts;
using ECommerce.Shopping.Infrastructure.Integration;
using ECommerce.Shopping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.Tests.Unit;

[TestClass]
public class ShoppingOutboxDispatcherReliabilityTests
{
    [TestMethod]
    public async Task Dispatch_WhenPublishFails_SchedulesRetry()
    {
        var bus = new TestIntegrationEventBus(failuresBeforeSuccess: 1);
        using var harness = CreateHarness(bus, "shopping-outbox-retry", new ShoppingOutboxDispatcherOptions
        {
            MaxRetryAttempts = 5,
            BaseRetryDelaySeconds = 2,
            MaxRetryDelaySeconds = 30,
            BatchSize = 25
        });

        harness.DbContext.OutboxMessages.Add(CreateOutboxMessage(CreateCartEvent()));
        await harness.DbContext.SaveChangesAsync();

        var before = DateTime.UtcNow;
        await harness.Dispatcher.DispatchOnceAsync(CancellationToken.None);

        var outbox = await harness.DbContext.OutboxMessages.SingleAsync();
        outbox.RetryCount.ShouldBe(1);
        outbox.ProcessedAt.ShouldBeNull();
        outbox.IsDeadLettered.ShouldBeFalse();
        outbox.NextAttemptAt.ShouldNotBeNull();
        outbox.NextAttemptAt!.Value.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(2));
    }

    [TestMethod]
    public async Task Dispatch_WhenRetriesExhausted_MovesToDeadLetter()
    {
        var bus = new TestIntegrationEventBus(failuresBeforeSuccess: int.MaxValue);
        using var harness = CreateHarness(bus, "shopping-outbox-dead-letter", new ShoppingOutboxDispatcherOptions
        {
            MaxRetryAttempts = 3,
            BaseRetryDelaySeconds = 1,
            MaxRetryDelaySeconds = 10,
            BatchSize = 25
        });

        var message = CreateOutboxMessage(CreateCartEvent());
        message.RetryCount = 2;
        harness.DbContext.OutboxMessages.Add(message);
        await harness.DbContext.SaveChangesAsync();

        await harness.Dispatcher.DispatchOnceAsync(CancellationToken.None);

        var outbox = await harness.DbContext.OutboxMessages.SingleAsync();
        var deadLetter = await harness.DbContext.DeadLetterMessages.SingleAsync();

        outbox.RetryCount.ShouldBe(3);
        outbox.IsDeadLettered.ShouldBeTrue();
        outbox.DeadLetteredAt.ShouldNotBeNull();
        outbox.NextAttemptAt.ShouldBeNull();

        deadLetter.OutboxMessageId.ShouldBe(outbox.Id);
        deadLetter.IdempotencyKey.ShouldBe(outbox.IdempotencyKey);
        deadLetter.RetryCount.ShouldBe(3);
        deadLetter.LastError.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Dispatch_WhenPublishSucceeds_MarksProcessed()
    {
        var bus = new TestIntegrationEventBus(failuresBeforeSuccess: 0);
        using var harness = CreateHarness(bus, "shopping-outbox-success", new ShoppingOutboxDispatcherOptions
        {
            MaxRetryAttempts = 3,
            BaseRetryDelaySeconds = 1,
            MaxRetryDelaySeconds = 10,
            BatchSize = 25
        });

        harness.DbContext.OutboxMessages.Add(CreateOutboxMessage(CreateCartEvent()));
        await harness.DbContext.SaveChangesAsync();

        await harness.Dispatcher.DispatchOnceAsync(CancellationToken.None);

        var outbox = await harness.DbContext.OutboxMessages.SingleAsync();

        bus.PublishCount.ShouldBe(1);
        outbox.ProcessedAt.ShouldNotBeNull();
        outbox.RetryCount.ShouldBe(0);
        outbox.LastError.ShouldBeNull();
        outbox.IsDeadLettered.ShouldBeFalse();
    }

    private static DispatcherHarness CreateHarness(
        IIntegrationEventBus integrationEventBus,
        string databasePrefix,
        ShoppingOutboxDispatcherOptions options)
    {
        var dbOptions = new DbContextOptionsBuilder<ShoppingDbContext>()
            .UseInMemoryDatabase($"{databasePrefix}-{Guid.NewGuid():N}")
            .Options;

        var dbContext = new ShoppingDbContext(dbOptions);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddSingleton(integrationEventBus);

        var rootProvider = services.BuildServiceProvider();
        var scopeFactory = new FixedServiceScopeFactory(rootProvider);

        var dispatcher = new ShoppingOutboxDispatcherHostedService(
            scopeFactory,
            Options.Create(options),
            NullLogger<ShoppingOutboxDispatcherHostedService>.Instance);

        return new DispatcherHarness(dbContext, rootProvider, dispatcher);
    }

    private static CartItemAddedIntegrationEvent CreateCartEvent()
        => new(Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

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
        ShoppingDbContext dbContext,
        ServiceProvider rootProvider,
        ShoppingOutboxDispatcherHostedService dispatcher) : IDisposable
    {
        public ShoppingDbContext DbContext { get; } = dbContext;
        public ShoppingOutboxDispatcherHostedService Dispatcher { get; } = dispatcher;

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
        public void Dispose() { }
    }
}
