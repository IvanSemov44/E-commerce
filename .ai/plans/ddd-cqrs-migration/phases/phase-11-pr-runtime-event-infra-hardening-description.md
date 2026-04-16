# Phase 11 PR Description: Runtime Event Infrastructure Hardening

Target commit:
- `f0cfc4b`

## Summary

This PR hardens startup behavior for separated persistence boundaries by ensuring integration event infrastructure and reviews projection bootstrap are initialized safely before runtime backfill executes.

## Why

After moving to per-context connection strings, runtime logs showed startup warnings for missing reviews projection relations in some environments. Event tables were healthy, but startup needed deterministic bootstrap behavior so the app can run cleanly on fresh or drifted databases.

## What changed

1. Updated startup initializer:
- File: `src/backend/ECommerce.API/Services/AppDbContextInitializationService.cs`
- Added integration persistence schema bootstrap via `IntegrationPersistenceDbContext.Database.EnsureCreatedAsync(...)` in non-test environments.

2. Updated reviews projection backfill service:
- File: `src/backend/ECommerce.API/Services/ReviewsProductProjectionBackfillService.cs`
- Added idempotent SQL bootstrap for `ReviewProductProjections` via `CREATE TABLE IF NOT EXISTS ...`.
- Added non-relational provider guard so integration tests using InMemory provider continue to pass.

3. Removed obsolete HTTP scratch file:
- File deleted: `src/backend/ECommerce.API/ECommerce.API.http`

## Architecture and boundary notes

1. No new cross-context business writes were introduced.
2. Event reliability remains within dedicated integration persistence (`integration` schema tables).
3. Startup sequencing remains: schema/init -> seed -> projection backfill.

## Validation

1. Focused tests passed:
- `Phase8MessageBrokerIntegrationTests`
- `ReviewsProductProjectionBackfillCharacterizationTests`

2. Runtime Docker verification passed:
- API `/health/ready` returns `200`.
- API startup log contains successful message:
  - `Reviews projection backfill completed. CatalogProducts=28, Inserted=28, Updated=0`

3. Event infrastructure table verification in PostgreSQL:
- `integration.outbox_messages`
- `integration.inbox_messages`
- `integration.dead_letter_messages`
- `integration.order_fulfillment_saga_states`

## Risk and rollback

Risk level: Low to medium
- Startup behavior changed for schema bootstrap path.

Rollback path:
1. Revert this PR commit.
2. Redeploy API.
3. Re-verify startup logs and `/health/ready`.

## Follow-up

1. Keep runtime monitoring on dead-letter growth and saga timeout compensation volume.
2. Continue Phase 11 context split closure and PR checklist evidence capture.
