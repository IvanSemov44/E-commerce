# Phase 8, Step 3: Outbox Pattern Implementation

**Prerequisite**: Step 2 (separate DbContexts) complete.

Implement the **Outbox Pattern** to guarantee at-least-once integration event delivery. Events are written to an `OutboxMessages` table in the same transaction as the aggregate save, then a background job publishes them to the message broker.

---

## Why Outbox Pattern

**Problem**: You save the order, then try to publish `OrderPlaced` event to RabbitMQ. But RabbitMQ is down. The order is saved, the event is lost forever.

**Solution**: Save the order AND the event in the **same transaction**:
1. `Order.Create()` → saves aggregate
2. `OutboxMessage` → save event to local DB table in same transaction
3. Background job polls `OutboxMessages` table
4. Publishes to broker
5. Marks as `Processed`

---

## Task 1: Create OutboxMessage Table

**File: `ECommerce.Infrastructure/Data/OutboxMessage.cs`**

```csharp
namespace ECommerce.Infrastructure.Data;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!; // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool Processed { get; set; }
}
```

**File: `ECommerce.Infrastructure/Data/OutboxDbContext.cs`**

```csharp
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public class OutboxDbContext : DbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("outbox");
        
        modelBuilder.Entity<OutboxMessage>()
            .HasKey(m => m.Id);
        
        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(m => m.IdempotencyKey).IsUnique();
        
        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(m => m.Processed);
    }
}
```

**EF Migration**:
```bash
dotnet ef migrations add "Add_Outbox_Table" -p ECommerce.Infrastructure -s ECommerce.API -c OutboxDbContext
dotnet ef database update -p ECommerce.Infrastructure -s ECommerce.API -c OutboxDbContext
```

---

## Task 2: Capture Events in Outbox

Modify domain event publishing to **also** write to Outbox:

**File: `ECommerce.Infrastructure/Data/OutboxPublisher.cs`**

```csharp
using ECommerce.Contracts;
using ECommerce.SharedKernel;
using System.Text.Json;

namespace ECommerce.Infrastructure.Data;

public interface IOutboxPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct) where T : IntegrationEvent;
}

public class OutboxPublisher : IOutboxPublisher
{
    private readonly OutboxDbContext _db;

    public OutboxPublisher(OutboxDbContext db) => _db = db;

    public async Task PublishAsync<T>(T @event, CancellationToken ct) where T : IntegrationEvent
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = @event.CorrelationId,
            EventType = typeof(T).FullName!,
            EventData = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
            Processed = false
        };

        await _db.OutboxMessages.AddAsync(message, ct);
        await _db.SaveChangesAsync(ct);
    }
}
```

**Register in DI**:
```csharp
builder.Services.AddScoped<IOutboxPublisher, OutboxPublisher>();
```

**Use in command handlers**:
```csharp
// In PlaceOrderCommandHandler
var order = Order.Create(...); // Creates domain events
await _repository.UpsertAsync(order, cancellationToken);

// Publish to Outbox (same transaction)
foreach (var evt in order.DomainEvents.OfType<OrderPlacedEvent>())
{
    var integrationEvent = new OrderPlacedIntegrationEvent(
        evt.OrderId, evt.CustomerId, order.Items.Select(i => i.ProductId).ToArray(), evt.TotalAmount);
    await _outboxPublisher.PublishAsync(integrationEvent, cancellationToken);
}

await _unitOfWork.SaveChangesAsync(cancellationToken);
```

---

## Task 3: Background Job to Publish Events

**File: `ECommerce.Infrastructure/BackgroundJobs/PublishOutboxMessagesJob.cs`**

```csharp
using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.Infrastructure.BackgroundJobs;

public class PublishOutboxMessagesJob
{
    private readonly OutboxDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PublishOutboxMessagesJob> _logger;

    public PublishOutboxMessagesJob(
        OutboxDbContext db,
        IPublishEndpoint publishEndpoint,
        ILogger<PublishOutboxMessagesJob> logger)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var messages = await _db.OutboxMessages
            .Where(m => !m.Processed)
            .OrderBy(m => m.CreatedAt)
            .Take(100) // Batch size
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                // Deserialize and publish
                var eventType = Type.GetType(message.EventType);
                if (eventType is null)
                {
                    _logger.LogWarning("Event type not found: {EventType}", message.EventType);
                    message.Processed = true;
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.EventData, eventType) as IntegrationEvent;
                if (@event is not null)
                {
                    await _publishEndpoint.Publish(@event, ct);
                    message.PublishedAt = DateTime.UtcNow;
                    message.Processed = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message: {Id}", message.Id);
                // Don't mark as processed; will retry on next job run
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

**Schedule in Program.cs** (using Quartz or BackgroundTaskQueue):
```csharp
// Every 5 seconds, run the outbox job
services.AddHostedService<OutboxPublisherHostedService>();
```

---

## Acceptance Criteria

- [ ] `OutboxMessage` table created
- [ ] `OutboxDbContext` registered
- [ ] `IOutboxPublisher` implemented and injected
- [ ] Domain events written to Outbox in same transaction as aggregate save
- [ ] Background job reads Outbox and publishes to broker
- [ ] Processed messages marked as such
- [ ] Idempotency keys prevent duplicate processing
- [ ] Failed publishes are retried (not marked processed on error)
