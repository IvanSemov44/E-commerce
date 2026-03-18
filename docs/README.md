# Documentation Index

## Two doc systems — what goes where

| System | Purpose | Audience |
|--------|---------|----------|
| `docs/` | Visual diagrams, architecture reference, API reference, onboarding | Everyone — team, new devs, stakeholders |
| `.ai/` | Step-by-step workflows, patterns, checklists, anti-patterns | Developers actively writing code (and Claude) |

If you're **onboarding** → start here in `docs/`.
If you're **implementing a feature** → go to `.ai/workflows/adding-feature.md`.
If you're **debugging** → go to `.ai/workflows/troubleshooting.md`.

---

## docs/ — all files

### Start here

| File | What's inside |
|------|--------------|
| [onboarding.md](onboarding.md) | Run the project locally in 10 minutes |
| [environments.md](environments.md) | Every environment variable for backend + frontend |

### Understand the system

| File | What's inside |
|------|--------------|
| [architecture.md](architecture.md) | Clean Architecture layers, frontend state diagram, system context map |
| [database.md](database.md) | Full ERD (14 tables), concurrency model, delete behaviors, indexes |
| [data-flow.md](data-flow.md) | Checkout sequence, auth/token refresh, order state machine, cart sync |

### Reference

| File | What's inside |
|------|--------------|
| [api-reference.md](api-reference.md) | All 82+ endpoints, auth level (public/user/admin), query params |
| [error-codes-reference.md](error-codes-reference.md) | All 40+ error codes, HTTP status mapping, frontend handling |
| [security.md](security.md) | JWT model, RBAC, CSRF, rate limiting, security headers, prod checklist |

### Engineering practices

| File | What's inside |
|------|--------------|
| [testing.md](testing.md) | Test pyramid, how to write unit + integration + E2E tests, coverage goals |
| [performance.md](performance.md) | N+1 prevention, projection, caching, RTK Query optimisation |
| [monitoring.md](monitoring.md) | Serilog logging, health checks, alerts, key metrics |

### Project status

| File | What's inside |
|------|--------------|
| [feature-status.md](feature-status.md) | Completion matrix per domain, test distribution, known gaps |
| [senior-dev-next.md](senior-dev-next.md) | Effort/impact quadrant, prioritised action list, Day 1 plan |

### Architecture Decision Records

| File | Decision |
|------|---------|
| [adr/001-clean-architecture.md](adr/001-clean-architecture.md) | Why Clean Architecture over N-Layer or Vertical Slice |
| [adr/002-result-pattern.md](adr/002-result-pattern.md) | Why `Result<T>` instead of exceptions for business failures |
| [adr/003-rtk-query.md](adr/003-rtk-query.md) | Why RTK Query over React Query, SWR, or manual fetch |
| [adr/004-postgresql.md](adr/004-postgresql.md) | Why PostgreSQL over SQL Server or MySQL |

---

## .ai/ — all files

### Workflows (how-to guides)

| File | When to use |
|------|------------|
| `.ai/workflows/adding-feature.md` | Adding any new feature end-to-end |
| `.ai/workflows/database-migrations.md` | Changing the database schema |
| `.ai/workflows/testing-strategy.md` | Deciding what and how to test |
| `.ai/workflows/deployment.md` | Deploying to any environment |
| `.ai/workflows/troubleshooting.md` | Debugging a problem |
| `.ai/workflows/post-modification-checks.md` | **Run after every code change** |
| `.ai/workflows/code-review.md` | Reviewing a pull request |

### Backend patterns

| File | What it covers |
|------|---------------|
| `.ai/backend/error-handling.md` | `Result<T>` pattern, `ErrorCodes`, `GlobalExceptionMiddleware` |

### Reference

| File | What it covers |
|------|---------------|
| `.ai/reference/common-mistakes.md` | 12+ documented anti-patterns to avoid |

### Tracking

| File | What it covers |
|------|---------------|
| `.ai/tracking/technical-debt.md` | Known debt, priority, owner |
| `.ai/tracking/future-improvements-orchestration.md` | Roadmap items with effort estimates |
