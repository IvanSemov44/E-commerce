# Phase 8, Step 5: Saga Pattern for Orchestration

**Prerequisite**: Step 4 (message broker) complete.

Implement the **Saga Pattern** to orchestrate multi-step business flows across contexts (e.g., PlaceOrder → ReserveInventory → SendEmail → ClearCart).

---

## The Problem

In Phase 7, PlaceOrder was synchronous:
```
PlaceOrder 
  → InventoryReducedEvent handled synchronously
  → EmailSentEvent handled synchronously
  → CartClearedEvent handled synchronously
  → Response returned (all done)
```

In Phase 8, these become async:
```
PlaceOrder 
  → Returns immediately (events queued to Outbox)
  → InventoryReservedEvent published by broker
  → EmailSentEvent published by broker
  → CartClearedEvent published by broker
  → But: what if InventoryReserved fails? We need to compensate (cancel order, restore cart)
```

**Saga Pattern** coordinates this workflow and handles failures.

---

## Task 1: Define Saga State

**File: `ECommerce.Infrastructure/Sagas/PlaceOrderSaga.cs`**

```csharp
using ECommerce.Contracts;
using MassTransit;

namespace ECommerce.Infrastructure.Sagas;

/// <summary>
/// Saga: Coordinates the multi-step flow of placing an order.
/// 
/// Flow:
/// 1. OrderPlaced event received
/// 2. Send ReserveInventory command
/// 3. Wait for InventoryReserved or InventoryReservationFailed event
/// 4. If failed: send CancelOrder command and complete
/// 5. If succeeded: send SendEmail command
/// 6. Wait for EmailSent event
/// 7. Complete saga
/// </summary>
public class PlaceOrderSaga : Saga<PlaceOrderSagaState>
{
    protected override void ConfigureStateMachine(IStateMachineConfigurator<PlaceOrderSagaState> cfg)
    {
        cfg.InstanceState(x => x.CurrentState);

        cfg.Event<OrderPlacedIntegrationEvent>((context, evt) => 
            evt.Data.OrderId == context.Saga.OrderId);

        cfg.Event<InventoryReservedIntegrationEvent>((context, evt) => 
            evt.Data.OrderId == context.Saga.OrderId);

        cfg.Event<InventoryReservationFailedIntegrationEvent>((context, evt) => 
            evt.Data.OrderId == context.Saga.OrderId);

        // Initial state: waiting for OrderPlaced
        cfg.Initially()
            .When(x => x.OrderPlaced)
            .Then(x =>
            {
                x.Saga.OrderId = x.Message.OrderId;
                x.Saga.CustomerId = x.Message.CustomerId;
            })
            .TransitionTo(cfg.State(x => x.ReservingInventory))
            // Send ReserveInventory command here
            .Send(x => new ReserveInventoryCommand(x.Message.OrderId, x.Message.ProductIds));

        // Waiting for inventory result
        cfg.State(x => x.ReservingInventory)
            .When(x => x.InventoryReserved)
            .Then(x => x.Saga.InventoryReserved = true)
            .TransitionTo(cfg.State(x => x.SendingEmail))
            // Send SendEmail command
            .Send(x => new SendOrderEmailCommand(x.Saga.OrderId, x.Saga.CustomerId))
            
            .When(x => x.InventoryReservationFailed)
            .Then(x => x.Saga.ReservationFailed = true)
            .TransitionTo(cfg.Final());

        // Waiting for email sent
        cfg.State(x => x.SendingEmail)
            .When(x => x.EmailSent)
            .TransitionTo(cfg.Final());
    }
}

public class PlaceOrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public bool InventoryReserved { get; set; }
    public bool ReservationFailed { get; set; }
    public bool EmailSent { get; set; }
}
```

---

## Task 2: Map Saga to Database

```csharp
// In OrderingDbContext
public DbSet<PlaceOrderSagaState> PlaceOrderSagaStates { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<PlaceOrderSagaState>()
        .ToTable("place_order_saga_states", schema: "ordering");
}
```

---

## Task 3: Handle Failures with Compensation

If `InventoryReservationFailed` event received:

```csharp
cfg.State(x => x.ReservingInventory)
    .When(x => x.InventoryReservationFailed)
    .Then(async (context) =>
    {
        // Compensating transaction: Cancel the order
        await context.Send(new CancelOrderCommand(context.Saga.OrderId));
        // Optionally: Restore cart items
    })
    .TransitionTo(cfg.Final());
```

---

## Acceptance Criteria

- [ ] Saga state machine defined for PlaceOrder flow
- [ ] Saga states and transitions correct
- [ ] Saga persisted to database
- [ ] Compensation logic implemented for failures
- [ ] Idempotency: receiving same event twice doesn't break saga
- [ ] Timeout handling (if InventoryReserved never comes, cancel order after X minutes)
- [ ] Saga integration tests passing
