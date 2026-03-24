# Phase 8: Assembly Extraction & Integration Events

**Status**: Future — not started. All 7 bounded context migrations (Phases 1–7) must be complete first.

**Learn**: Physical bounded context separation, integration events vs domain events, cross-context communication patterns.

---

## What Phase 8 Is

By the end of Phase 7, you have logically separated bounded contexts (separate Domain/Application/Infrastructure projects per context), but they still share **one database** and communicate via **in-process MediatR domain events**.

Phase 8 physically extracts them: separate connection strings, separate DbContexts, and cross-context events become **integration events** (messages that survive process boundaries, typically via a message broker like MassTransit/RabbitMQ or Azure Service Bus).

This phase is optional in the short term but is the correct architectural end state.

---

## Why This Is the Hardest Phase

| Problem | Why It's Hard |
|---------|---------------|
| Cross-context DB queries break | You can no longer JOIN across context tables in SQL |
| Foreign key constraints disappear | Order.ProductId has no FK to Catalog DB |
| In-process events become async | `ProductUpdatedEvent` was synchronous; integration events are eventually consistent |
| Message broker introduces new failure modes | At-least-once delivery, idempotency, poison messages |
| Schema ownership must be decided | Who owns the Products table? Catalog only. Ordering must copy what it needs. |
| Data migration required | Separate schemas within one DB first, then optionally separate databases |

**Do not start Phase 8 until you have a solid understanding of distributed systems consistency and have read about the Outbox Pattern.**

---

## Phase 8 Steps (Overview)

### Step 1: Separate DbContexts (same database, separate schemas)

Split the single shared `AppDbContext` into one context per bounded context:
- `CatalogDbContext` — owns Products, Categories
- `IdentityDbContext` — owns Users
- `InventoryDbContext` — owns InventoryItems
- `ShoppingDbContext` — owns Carts, Wishlists
- `PromotionsDbContext` — owns PromoCodes
- `ReviewsDbContext` — owns Reviews
- `OrderingDbContext` — owns Orders

Each context only maps the tables it owns. Use PostgreSQL schemas to separate them at the DB level (`catalog.products`, `ordering.orders`, etc.) while staying in one database. This is the safe intermediate step.

**Breaking change to resolve**: Any cross-context LINQ join (e.g., loading Order with Product details) must be replaced. Options:
- Denormalize: copy `ProductName` + `Price` into `OrderItem` at the time of ordering (correct DDD approach)
- Read model sync: maintain a local read-model copy in the Ordering DB via integration events

### Step 2: Introduce Integration Events

Domain events are in-process and synchronous. Once contexts are in separate processes/DBs, they cannot share the same process-level MediatR.

Replace cross-context domain event handlers with integration events:

```
BEFORE (in-process, Phase 3–7):
    Inventory.StockReducedEvent  →  MediatR Publish  →  Ordering.UpdateShipmentHandler

AFTER (cross-process, Phase 8):
    Inventory publishes StockReducedIntegrationEvent to message broker
    Ordering subscribes to StockReducedIntegrationEvent from message broker
    Ordering's handler is called when the message arrives (eventually consistent)
```

Integration event contract:
```csharp
// Lives in a shared Contracts project, versioned carefully
public record StockReducedIntegrationEvent(
    Guid ProductId,
    int QuantityReduced,
    int RemainingStock,
    DateTime OccurredAt
);
```

### Step 3: Implement the Outbox Pattern

The biggest risk with integration events: you save the aggregate and then try to publish the event to the broker — but the broker call fails. The aggregate is saved, the event is lost.

The Outbox Pattern solves this:
1. In the same DB transaction as the aggregate save, write the event to an `OutboxMessages` table
2. A background job reads `OutboxMessages` and publishes to the broker
3. Once published, mark the message as processed

This guarantees at-least-once delivery. Consumers must be idempotent (handle duplicate events).

### Step 4: Handle Read-Model Cross-Context Needs

Some queries need data from multiple contexts (e.g., "Order with Product name and User email"). Options:

| Option | How | When to use |
|--------|-----|-------------|
| API composition | API calls two services and merges | Simple reads, low volume |
| Denormalization | Copy needed data at write time | Data doesn't change often |
| Local read model | Each context maintains a local projection of cross-context data | High read volume |
| Reporting database | Separate read-optimized DB fed by events | Analytics, complex joins |

---

## Known Risks and Open Questions

### Risks

1. **Circular event chains** — Context A publishes event → Context B handles it and publishes another event → Context A handles that and publishes again → infinite loop. Requires careful event design and idempotency keys.

2. **Eventual consistency user experience** — After `PlaceOrder`, stock is reduced eventually (not immediately). The UI may show stale stock counts briefly. Need to decide: is this acceptable? (Usually yes for most e-commerce scenarios.)

3. **Schema evolution** — Integration event contracts are public API. Once published and consumed by another service, changing them is a breaking change. Need a versioning strategy (e.g., `StockReducedIntegrationEventV2`).

4. **Message ordering** — Message brokers don't guarantee ordering across partitions. If two events about the same product arrive out of order, consumers must handle this.

5. **Poison messages** — An event that always causes the consumer to throw will retry indefinitely. Need a dead-letter queue strategy.

### Open Questions (Decide Before Starting Phase 8)

- [ ] **Message broker choice**: MassTransit (abstraction over RabbitMQ/SB) or direct RabbitMQ/Azure Service Bus?
- [ ] **Outbox library**: Custom implementation, MassTransit's inbox/outbox, or NServiceBus?
- [ ] **Separate databases or schemas?** Schemas (PostgreSQL schemas) are the safer first step. Separate databases add operational complexity.
- [ ] **Which contexts actually need to be separate processes?** Catalog + Inventory are high-traffic. Others may never need it. Don't extract everything — extract what provides value.
- [ ] **Saga/process manager for Order placement**: OrderPlaced must reliably trigger: ReduceInventory + SendEmail + ClearCart + CreateShipment. In Phase 7 this was synchronous. In Phase 8 it needs a Saga (e.g., MassTransit Saga, or the Process Manager pattern).

---

## Pre-Requisites for Phase 8

Before starting:

- [ ] All 7 bounded context migrations complete
- [ ] Solid understanding of: eventual consistency, idempotency, at-least-once delivery
- [ ] Read: "Building Microservices" (Newman) Chapter 4-5, or "Implementing Domain-Driven Design" (Vernon) Chapter 8
- [ ] Decision made on message broker
- [ ] Decision made on whether Outbox is needed (answer: almost certainly yes)
- [ ] Load test baseline captured — before extracting, know your current p95 response times

---

## What You Learn in Phase 8

By completing Phase 8, you will understand:

1. **Physical vs logical separation** — the difference between a bounded context (logical) and a microservice (physical deployment unit)
2. **Integration events vs domain events** — two different beasts, used at different scopes
3. **The Outbox Pattern** — how to guarantee event delivery without distributed transactions
4. **Saga pattern** — how to coordinate multi-step business flows across separate contexts
5. **The cost of distribution** — why you don't extract to microservices until you need to

These are principal-level distributed systems skills. They're also on your learning roadmap (see memory/principal-level-learning-roadmap.md).
