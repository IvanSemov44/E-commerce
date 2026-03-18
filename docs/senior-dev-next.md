# What a Senior Dev Would Do Next

> Snapshot: March 2026. Project is feature-complete for MVP but not production-ready.

---

## Priority Matrix

```mermaid
quadrantChart
    title Effort vs Impact
    x-axis Low Effort --> High Effort
    y-axis Low Impact --> High Impact
    quadrant-1 Do it now
    quadrant-2 Plan carefully
    quadrant-3 Do if time allows
    quadrant-4 Avoid / defer

    ProducesResponseType coverage: [0.2, 0.85]
    Email provider decision: [0.1, 0.4]
    LoadingFallback split: [0.15, 0.5]
    Wishlist & Profile API: [0.3, 0.6]
    Real Stripe integration: [0.75, 0.95]
    Query N+1 guardrails: [0.4, 0.7]
    Frontend admin consolidation: [0.7, 0.65]
    Background jobs (Hangfire): [0.8, 0.45]
    API versioning: [0.85, 0.35]
```

---

## Action List

### 🔴 Blocker — Do Before Any Go-Live

| # | Task | Why it matters | Effort | Key files |
|---|------|---------------|--------|-----------|
| 1 | **Real Stripe integration** | `PaymentService` is fully mocked — zero real revenue possible | 3–5 days | `src/backend/ECommerce.Application/Services/PaymentService.cs`, `src/frontend/storefront/src/features/checkout/` |

---

### 🟠 High — Do This Sprint

| # | Task | Why it matters | Effort | Key files |
|---|------|---------------|--------|-----------|
| 2 | **Complete `[ProducesResponseType]` on all endpoints** | ~87 missing. Swagger/OpenAPI contract is broken; client code-gen fails | 1–2 days | All 12 controllers in `src/backend/ECommerce.API/Controllers/` |
| 3 | **Decide & clean up email provider** | Both `SendGridEmailService` and `SmtpEmailService` are registered — ambiguous in prod | 2 hrs | `src/backend/ECommerce.API/Extensions/ServiceCollectionExtensions.cs` |

---

### 🟡 Medium — Next Sprint

| # | Task | Why it matters | Effort | Key files |
|---|------|---------------|--------|-----------|
| 4 | **Flesh out Profile & Wishlist API (frontend)** | `profileApi.ts` and `wishlistApi.ts` are stubs — pages are shallow | 1 day | `src/frontend/storefront/src/features/profile/`, `src/frontend/storefront/src/features/wishlist/` |
| 5 | **Split `LoadingFallback`** | Suspense route lazy-load skeleton ≠ AppShell initialization spinner — mixing them causes layout flicker | 0.5 day | `src/frontend/storefront/src/components/` (AppShell area) |
| 6 | **Query N+1 guardrails** | Product pages load images, categories, reviews — risk of N+1 queries under load | 1 day | `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs`, `ReviewRepository.cs` |

---

### 🟢 Low — Backlog

| # | Task | Why it matters | Effort |
|---|------|---------------|--------|
| 7 | Frontend admin/storefront consolidation | Shared RTK Query base, shared UI components, shared types | 2–4 days |
| 8 | External call resilience audit | Verify Polly retry/circuit-breaker wraps Stripe + SendGrid calls | 1 day |
| 9 | Background jobs decision (Hangfire vs. minimal) | Needed for: order timeout, email retry, stock alerts | 1 day planning |
| 10 | API versioning strategy | Required before any breaking API change | 0.5 day planning |

---

## What a Senior Dev Would Do on Day 1

```
Morning:
  1. git log --oneline -20           ← understand recent momentum
  2. Read .ai/tracking/technical-debt.md
  3. Run the full test suite          ← establish green baseline
  4. Open Swagger UI                  ← feel the missing ProducesResponseType pain

Afternoon:
  5. Create Stripe account + sandbox keys
  6. Spike: replace PaymentService mock with real Stripe.net SDK call
  7. Write one integration test for payment success + payment failure
  8. Document the pattern in .ai/backend/payment-integration.md
```

---

## Current Scores

```mermaid
radar
    title Project Health (out of 10)
    "Architecture" : 9
    "Test Coverage" : 8
    "API Contract" : 5
    "Documentation" : 8
    "Production Readiness" : 4
    "Code Consistency" : 8
    "Performance Hardening" : 6
```

The single biggest gap between "good codebase" and "production-ready" is **real payment processing**. Everything else is polish.
