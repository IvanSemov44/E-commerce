# DDD-CQRS Migration Checklists

Use these checklists together:

1. `dbcontext-readiness-checklist.md`
   - Core per-context persistence readiness (model, DI, migrations, verification).

2. `cross-cutting-persistence-reliability-checklist.md`
   - Cross-cutting boundary and reliability checks (filters, interceptors, event commit wiring, outbox ownership, health checks, schema isolation).

Recommended order:
1. Complete DbContext readiness checklist.
2. Complete cross-cutting reliability checklist.
3. Attach evidence links in PR notes.
