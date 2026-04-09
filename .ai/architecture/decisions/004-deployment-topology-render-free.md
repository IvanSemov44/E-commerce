# ADR-004: Deployment Topology on Render Free (Single API + Single Postgres)

Date: 2026-04-09
Status: accepted
Deciders: @ivans

## Context
The project is a modular monolith that is being prepared for bounded-context database independence and possible future extraction. Current hosting target is Render Free, where operational limits make multi-service and multi-database production topology expensive and complex.

At the same time, architecture work requires strict context boundaries, explicit connection keys, and event-driven consistency patterns so that later split remains possible without major redesign.

## Decision
Use the following production topology for the current stage:

1. One deployed backend web service (single API process).
2. One managed PostgreSQL database instance.
3. Keep per-context connection keys in configuration, but allow them to point to the same physical database for now.
4. Enforce logical boundaries in code and schema ownership (no direct cross-context business writes).
5. Keep outbox/inbox/saga reliability patterns as the cross-context consistency mechanism.
6. Defer physical multi-database cutover until explicit operational triggers are met.

## Consequences
- Positive: lowest operational complexity and cost on Render Free.
- Positive: preserves migration path to per-context physical databases later.
- Positive: avoids forcing premature microservice split.
- Negative: weaker blast-radius isolation than full multi-database topology.
- Negative: shared-database operational contention remains possible.
- Neutral: requires disciplined ownership boundaries and checklist enforcement.

## Trigger Conditions for Future Multi-Database Split
Move from single physical database to per-context databases when one or more of these are true:

1. One context requires independent scale profile or maintenance window.
2. Blast-radius isolation becomes a hard reliability requirement.
3. Independent release cadence is blocked by shared database change risk.
4. Compliance or governance requires stricter physical separation.
5. Hosting plan and budget support additional managed databases.

## Alternatives Considered
- Option A: Immediate multi-database split on Render Free.
  - Rejected for now due to operational overhead and platform constraints.
- Option B: Full microservices decomposition now.
  - Rejected for now as out of scope and too costly for current stage.
- Option C: Single API + single DB with strict boundaries and readiness gates.
  - Accepted as the best current trade-off.

## References
- `render.yaml`
- `docker-compose.yml`
- `.github/workflows/ci.yml`
- `.ai/plans/ddd-cqrs-migration/phases/dbcontext-readiness-checklist.md`
- `.ai/plans/ddd-cqrs-migration/checklists/cross-cutting-persistence-reliability-checklist.md`
- `.ai/plans/ddd-cqrs-migration/phases/phase-11-per-context-database-independence.md`
