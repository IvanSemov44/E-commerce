# Phase 2, Step 7: Re-Run E2E Tests After Cutover

**Prerequisite**: Step 4 (Cutover) complete. Backend running on `http://localhost:5000`.

The `api-auth.spec.ts` was written in **step 0b** and passed against the OLD service. Now re-run the same file against the NEW MediatR handlers to confirm nothing regressed against a real PostgreSQL database.

---

## Run

```bash
# Backend must be running with real PostgreSQL (not InMemory)
cd src/frontend/storefront
npx playwright test api-auth.spec.ts --reporter=list
```

---

## What to check

All tests from step 0b must still pass. Pay special attention to:

| Test | Why it matters |
|------|----------------|
| Nonexistent email login → 401 (not 404) | Security invariant — must survive migration |
| `forgot-password` → always 200 | Security invariant — must survive migration |
| `EMAIL_TAKEN` error code | Error code must be identical before and after migration |
| `INVALID_CREDENTIALS` error code | Same — any code change breaks frontend error handling |
| `GET /auth/me` with valid token → 200 + `email` field | JWT generation must work in new `JwtTokenService` |
| `GET /profile` with valid token → 200 | UserRepository reconstitution works end-to-end |

---

## If a test fails after cutover

1. Check if the error code changed (e.g. `INVALID_CREDENTIALS` → something else) — update the controller mapping in step 4
2. Check if the response shape changed (e.g. token field renamed) — align the DTO
3. Check EF reconstitution — if `GET /profile` fails, `UserRepository.MapToDomain()` likely has a bug
4. Run the characterization tests (step 0) to narrow down whether it's HTTP contract or DB layer

---

## Acceptance Criteria

- [ ] All tests from `api-auth.spec.ts` pass against the NEW handlers
- [ ] Zero regressions compared to the step 0b baseline
- [ ] Security invariants verified against real PostgreSQL
