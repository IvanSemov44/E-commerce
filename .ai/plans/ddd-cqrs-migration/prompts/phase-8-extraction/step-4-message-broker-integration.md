# Phase 8, Step 4: Message Broker Integration (MassTransit)

**Prerequisite**: Step 3 (Outbox) complete.

Wire up **MassTransit** as the abstraction over RabbitMQ/Azure Service Bus for publishing and consuming integration events.

---

## Task 1: Install MassTransit

```bash
cd src/backend
dotnet add ECommerce.API package MassTransit
dotnet add ECommerce.API package MassTransit.RabbitMQ
# OR for Azure Service Bus:
# dotnet add ECommerce.API package MassTransit.AzureServiceBus
```

---

## Task 2: Configure MassTransit in Program.cs

```csharp
builder.Services.AddMassTransit(x =>
{
    // Register all consumers (event handlers) from Infrastructure assembly
    x.AddConsumers(typeof(ECommerce.Infrastructure.DependencyInjection).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");
        cfg.ConfigureEndpoints(context);
    });
});

// OR Azure Service Bus:
// x.UsingAzureServiceBus((context, cfg) =>
// {
//     cfg.Host("Endpoint=sb://myservicebus.servicebus.windows.net/;...");
//     cfg.ConfigureEndpoints(context);
// });
```

---

## Task 3: Create Integration Event Consumers

**File: `ECommerce.Infrastructure/EventConsumers/InventoryReservationConsumer.cs`**

```csharp
using ECommerce.Contracts;
using ECommerce.Inventory.Domain.Interfaces;
using ECommerce.SharedKernel;
using MassTransit;

namespace ECommerce.Infrastructure.EventConsumers;

/// <summary>
/// Consumes OrderPlacedIntegrationEvent from message broker.
/// Attempts to reserve inventory. Publishes InventoryReservedIntegrationEvent on success.
/// </summary>
public class InventoryReservationConsumer : IConsumer<OrderPlacedIntegrationEvent>
{
    private readonly IInventoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryReservationConsumer> _logger;

    public InventoryReservationConsumer(
        IInventoryRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<InventoryReservationConsumer> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Reserving inventory for order {OrderId}", message.OrderId);

        try
        {
            // Attempt to reserve each product
            foreach (var productId in message.ProductIds)
            {
                var item = await _repository.GetByProductIdAsync(productId);
                if (item is null || item.AvailableQuantity < 1)
                {
                    // Publish failure event
                    await context.Publish(new InventoryReservationFailedIntegrationEvent(
                        message.OrderId,
                        productId,
                        "Insufficient inventory"));
                    return;
                }

                item.Reserve(1);
                await _repository.UpsertAsync(item);
            }

            await _unitOfWork.SaveChangesAsync();

            // Publish success event
            await context.Publish(new InventoryReservedIntegrationEvent(
                message.OrderId,
                message.ProductIds,
                message.ProductIds.Select(_ => 1).ToArray()));

            _logger.LogInformation("Inventory reserved for order {OrderId}", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving inventory for order {OrderId}", message.OrderId);
            throw;
        }
    }
}
```

Register in DI:
```csharp
// In DependencyInjection.cs
builder.Services.AddMassTransitConsumer<InventoryReservationConsumer>();
```

---

## Task 4: Local Testing with TestHarness

**File: `src/backend/ECommerce.Tests/Integration/Phase8MessageBrokerTests.cs`**

```csharp
using ECommerce.Contracts;
using ECommerce.Infrastructure.EventConsumers;
using MassTransit.Testing;
using Xunit;

namespace ECommerce.Tests.Integration;

public class Phase8MessageBrokerTests
{
    [Fact]
    public async Task OrderPlaced_InventoryReserved_ConsumerHandlesEvent()
    {
        // Create test harness
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => new InventoryReservationConsumer(
            new FakeInventoryRepository(),
            new FakeUnitOfWork(),
            LoggerFactory.Create(x => x.AddConsole()).CreateLogger<InventoryReservationConsumer>()));

        await harness.Start();

        try
        {
            // Send OrderPlacedIntegrationEvent
            var evt = new OrderPlacedIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new[] { Guid.NewGuid() },
                100m);

            await harness.Bus.Publish(evt);

            // Assert consumer received and processed
            Assert.True(await consumerHarness.Consumed.Any<OrderPlacedIntegrationEvent>());

            // Assert InventoryReservedIntegrationEvent was published
            Assert.True(await harness.Published.Any<InventoryReservedIntegrationEvent>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
```

---

## Acceptance Criteria

- [ ] MassTransit installed and configured
- [ ] Message broker running (RabbitMQ or Azure Service Bus)
- [ ] Integration event consumers created and registered
- [ ] Consumers call domain logic and publish follow-up events
- [ ] In-memory tests verify consumer behavior
- [ ] Error handling for failed event consumption
- [ ] Dead-letter queue configured for poison messages
