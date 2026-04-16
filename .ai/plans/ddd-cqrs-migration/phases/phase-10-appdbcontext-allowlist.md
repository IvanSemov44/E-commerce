# Phase 10 AppDbContext Entity Allowlist

Status: Draft for PR 1
Owner: @ivans
Created: 2026-04-08

## Purpose

Define exactly what `AppDbContext` is allowed to own after Phase 10 decisions are locked.

Core rule:
- `AppDbContext` may own only technical/cross-cutting persistence entities.
- Business entities owned by bounded contexts are forbidden in `AppDbContext`.

## Allowed Entities (technical context scope)

| Entity/Table | Why allowed | Write owner | Consumer pattern |
|---|---|---|---|
| OutboxMessage / integration.outbox_messages | Reliable integration event delivery | TechnicalContext | Outbox dispatcher |
| InboxMessage / integration.inbox_messages | Idempotent message processing | TechnicalContext | Inbox idempotency processor |
| DeadLetterMessage / integration.dead_letter_messages | Failed message retention + replay | TechnicalContext | Dead-letter replay service |
| OrderFulfillmentSagaState / integration.order_fulfillment_saga_state | Process coordination state | TechnicalContext | Saga orchestrator |

## Forbidden in AppDbContext

These must be owned by bounded-context DbContexts:

- Catalog business entities (products, categories, product images)
- Identity business entities (users, addresses, auth domain state)
- Inventory business entities (stock write model)
- Shopping business entities (cart, wishlist)
- Ordering business entities (orders, order items, status write model)
- Promotions business entities (promo code write model)
- Reviews business entities (review write model)

## Temporary exceptions

Any temporary usage requires an entry in:
- `.ai/plans/ddd-cqrs-migration/phases/phase-10-temporary-bridge-register.md`

Required exception fields:
- owner
- reason
- removal phase
- test gate

## Enforcement checks (per PR)

1. No newly introduced business DbSet/entity mapping in `AppDbContext`.
2. Any existing forbidden usage must be tracked as temporary bridge.
3. Build and targeted tests pass.
