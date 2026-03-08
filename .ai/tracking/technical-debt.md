# Tracking: Technical Debt

Updated: 2026-03-08
Owner: @ivans

## Purpose
Centralized list of active technical debt items that affect reliability, maintainability, or AI guidance quality.

## Active Debt

### 1) API response documentation coverage
- Status: Open
- Priority: Medium
- Summary: Many controller endpoints are missing full `[ProducesResponseType]` coverage.
- Current estimate: ~87 missing attributes, ~35% compliance.
- Main impact: Incomplete Swagger/OpenAPI contracts.
- Primary references:
  - `.ai/tracking/technical-debt.md`
  - `.ai/workflows/adding-feature.md`
  - `src/backend/ECommerce.API/Controllers/`

### 2) Legacy guide overlap/duplication
- Status: In progress
- Priority: Medium
- Summary: Legacy docs still exist while `.ai/` canonical docs are being adopted.
- Mitigation in progress: legacy warning banners + adapter files.
- Primary references:
  - `.ai/README.md`
  - `.ai/standards/documentation.md`
  - `.ai/workflows/adding-feature.md`
  - `.ai/reference/common-mistakes.md`

### 3) Backend alignment follow-ups
- Status: Tracked
- Priority: High
- Summary: Remaining service/error-handling and resilience follow-ups identified during reviews.
- Examples to track:
  - Exception-vs-Result consistency in selected service paths.
  - External-call resilience/circuit-breaker verification.
  - Concurrency/idempotency hardening where needed.
- Primary references:
  - `.ai/backend/error-handling.md`
  - `.ai/reference/common-mistakes.md`
  - `src/backend/ECommerce.Application/Services/`

## How to Use
- Keep this file short and prioritized.
- Move completed items to commit/PR notes, not permanent backlog text.
- If an item becomes implementation-ready, link an issue/PR and next action.
