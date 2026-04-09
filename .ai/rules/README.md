# Architecture Rules Index

Rules are split by layer. Each file covers one layer concern: what is allowed, what is forbidden, and what evidence is required before merge.

## Layer Files

| File | Covers | Blocking? |
|------|--------|-----------|
| [dbcontext.md](dbcontext.md) | DbContext model, EF mappings, migrations, interceptors, schema | All rules are blockers |
| [repository.md](repository.md) | Repository interface contracts, query shape, aggregate loading | All rules are blockers |
| [service-handlers.md](service-handlers.md) | Command handlers, domain service orchestration, UoW commit | All rules are blockers |
| [queries.md](queries.md) | Query handlers, read paths, projections, caching | All rules are blockers |
| [controllers.md](controllers.md) | Controller shape, validation pipeline, authorization guards | All rules are blockers |
| [events-reliability.md](events-reliability.md) | Domain events, outbox, cross-context consistency, health checks | All rules are blockers |
| [testing-gates.md](testing-gates.md) | Test layer assignments, required test types, evidence gate | Gate controls merge |

## Blocker vs Advisory

**Blocker** — PR cannot merge until this passes. Reviewer must explicitly confirm.

**Advisory** — Best practice. Violation must be documented with justification in the PR. Does not block merge.

All rules in these files are **blockers** unless explicitly marked `[advisory]`.

## How to Use

- When implementing a feature: read the layer file for the layer you are touching.
- When reviewing a PR: check the layer file for each layer the PR touches, plus [testing-gates.md](testing-gates.md).
- When running an AI prompt: reference the exact layer file in the prompt context, not the full rules.md.
- When verifying a bounded context cutover: run both checklists in `.ai/plans/ddd-cqrs-migration/checklists/`.

## Decision Map

| Situation | Which file to read |
|-----------|--------------------|
| Adding or changing an EF mapping | dbcontext.md |
| Adding a repository method | repository.md |
| Writing a command handler | service-handlers.md |
| Writing a query handler | queries.md |
| Adding a controller action | controllers.md |
| Adding a domain event or outbox message | events-reliability.md |
| Deciding which test type to write | testing-gates.md |
| Cutting over a bounded context DbContext | checklists/dbcontext-readiness-checklist.md + checklists/cross-cutting-persistence-reliability-checklist.md |
