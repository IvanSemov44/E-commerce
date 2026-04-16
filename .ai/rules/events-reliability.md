# Domain Events and Reliability Rules

## Scope

Applies to: domain events, integration events, event handlers, outbox pattern, inbox pattern, saga coordination, cross-context consistency, health checks.

---

## Hard Rules

1. **Events are dispatched after save, never before.** Events are collected during aggregate mutation and dispatched after `SaveChangesAsync` succeeds. Dispatching before persistence means the event could describe a state change that never committed.

2. **Domain event publishing is bound to this context's UnitOfWork commit.** Event dispatch occurs from this context's `UnitOfWork.CommitAsync`, not from AppDb's pipeline or another context's save path. A context that is structurally correct but wired to the wrong commit boundary silently loses events.

3. **Event handlers run inside the save transaction — keep them fast and non-throwing for infrastructure work.**
   - Handlers doing infrastructure work (email, external HTTP, cache eviction) must swallow exceptions and log — never re-throw. A re-throw rolls back the aggregate save.
   - Handlers doing secondary DB writes (audit log) may throw — the rollback is correct if the secondary write fails.
   - Heavy work (sending emails, calling Stripe) belongs in a background job triggered by the event, not synchronously inside the handler.

4. **No direct cross-context business writes.** A context cannot write directly to another context's tables. Cross-context state changes go through outbox messages, integration events, or sagas.

5. **Outbox table ownership is explicit.** Which context owns the outbox table is documented. A context writing to another context's outbox without an explicit contract is a hidden cross-context write.

6. **Idempotency and duplicate handling are verified.** Every consumer of an integration event or outbox message handles duplicate delivery without side effects.

7. **Dead-letter and replay path are verified.** Failed messages have a defined dead-letter destination. The replay procedure is documented and tested.

8. **Per-context connection has an explicit health check.** Each DbContext with its own connection string has a corresponding health check registered. A broken connection fails the readiness probe — it is not invisible until runtime.

9. **Schema isolation prevents naming collisions.** If contexts share a database, each uses a dedicated schema prefix (e.g. `catalog.`, `ordering.`). Migration output must respect the chosen isolation strategy.

10. **Event naming uses past tense.** Events are facts: `OrderPlaced`, `StockReduced`, `ProductCreated`. Not `PlaceOrder`, `ReduceStock`.

11. **Event handlers are independent.** Each handler does one thing. If `OrderPlacedEvent` needs to reduce stock AND send email, those are two separate handlers.

12. **Handlers don't throw to callers (infrastructure path).** If a domain event handler fails on an infrastructure operation, it logs and retries or compensates. It does not propagate the exception to the command handler that raised the event.

---

## Allowed Patterns

- Outbox pattern for reliable cross-context message delivery.
- Inbox pattern for idempotent message consumption.
- Saga / process manager for multi-step cross-context workflows.
- Background job triggered by a domain event for heavy infrastructure work.

---

## Forbidden Patterns

- Dispatching domain events before `SaveChangesAsync` completes.
- Wiring event dispatch to a different context's UoW commit.
- Direct write to another context's aggregate table from an event handler.
- Synchronous external HTTP call or email send inside a domain event handler (must be backgrounded).
- Missing idempotency key on any integration event consumer.

---

## Required Tests

- Integration test: commit → event dispatched → consumer effect observed.
- Integration test: duplicate message delivery → idempotency confirmed (effect applied exactly once).
- Integration test: health check for this context's connection fails when connection is broken.
- Unit test: domain event handler logs and does not re-throw on infrastructure failure.

---

## Required Evidence (before merge)

- Event dispatch wired to this context's UoW commit path (not AppDb or another context).
- Outbox table ownership documented.
- Idempotency handling present and tested.
- Dead-letter destination defined.
- Health check registered for this context's connection string.

---

## Definition of Done

All hard rules pass. Required tests are green. Required evidence is attached in the PR.
