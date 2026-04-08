# Phase 10 Ownership Matrix

Status: Draft for PR 1
Owner: @ivans
Created: 2026-04-08

## Purpose

Single source of truth for persistence ownership during Phase 10.

Rule:
- Every business table has exactly one write owner context.
- Cross-context reads must use projection/query boundary, not direct business-table coupling.

## Columns

- `Object`: table/view/materialized view name
- `Type`: table | view | materialized_view
- `WriteOwnerContext`: one bounded context only (or technical context for ops data)
- `AllowedReaders`: contexts allowed to read
- `AccessMode`: projection | query-api | direct-read (temporary only)
- `DirectCrossContextAccessAllowed`: no by default
- `Bridge`: none | temporary
- `RemovalPhase`: phase when temporary bridge must be removed
- `RollbackImpact`: low | medium | high
- `Notes`: rationale and constraints

## Matrix

| Object | Type | WriteOwnerContext | AllowedReaders | AccessMode | DirectCrossContextAccessAllowed | Bridge | RemovalPhase | RollbackImpact | Notes |
|---|---|---|---|---|---|---|---|---|---|
| integration.outbox_messages | table | TechnicalContext | All (indirect) | projection | no | none | n/a | medium | Integration reliability table |
| integration.inbox_messages | table | TechnicalContext | TechnicalContext | direct-read | no | none | n/a | medium | Idempotency and dedup tracking |
| integration.dead_letter_messages | table | TechnicalContext | TechnicalContext, Operations | direct-read | no | none | n/a | medium | Retry/replay operations |
| integration.order_fulfillment_saga_state | table | TechnicalContext | Ordering, Operations | projection | no | none | n/a | high | Process state, not business aggregate state |
| public.ReviewProductProjections | table | Reviews | Reviews | direct-read | no | none | n/a | medium | Reviews local projection fed by catalog product projection events; startup idempotent backfill is implemented for baseline rows |
| public.products | table | Catalog | Shopping, Promotions, Dashboard | projection | no | temporary | 10 | high | Catalog-owned write model source; downstream contexts should consume through local projections |
| public.orders | table | Ordering | Dashboard, Payments, Inventory, Promotions | projection | no | temporary | 10 | high | Dashboard reads must remain projection-only |
| public.users | table | Identity | Ordering, Reviews, Dashboard | projection | no | temporary | 10 | high | Identity-owned write model |
| public.inventory_items | table | Inventory | Shopping, Ordering, Dashboard | projection | no | temporary | 10 | high | Stock read model feeds consumers |

## PR 1 completion rule

PR 1 is complete only when:
1. Every active table/view touched by current runtime paths is represented.
2. Every `Bridge=temporary` row has a concrete removal path and test gate.
3. No row has ambiguous ownership.
