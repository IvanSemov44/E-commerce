# ADR 001 — Clean Architecture

**Status:** Accepted
**Date:** 2025
**Deciders:** Core team

---

## Context

We needed a structural pattern for the .NET backend that would:
- Scale as the team grows
- Keep business logic testable without spinning up a database or HTTP stack
- Prevent infrastructure concerns (EF Core, HTTP, email) from leaking into domain logic
- Make onboarding predictable — a new dev should know where to put new code without asking

## Decision

Adopt **Clean Architecture** with four explicit layers:

```
Core  ←  Application  ←  Infrastructure
Core  ←  Application  ←  API
```

**Core** — pure domain. No NuGet dependencies except .NET BCL. Entities, interfaces, `Result<T>`, `ErrorCodes`.

**Application** — orchestration. Depends only on Core. Services, DTOs, validators, AutoMapper profile. Zero EF Core, zero HTTP.

**Infrastructure** — persistence. Implements Core interfaces (repositories, UnitOfWork). Only layer that knows about EF Core, PostgreSQL, SendGrid, Polly.

**API** — delivery. Thin controllers that call Application services, return `ApiResponse<T>`. No business logic.

## Alternatives considered

| Option | Why rejected |
|--------|-------------|
| Vertical Slice Architecture | Better for CQRS-heavy apps; we don't use MediatR and wanted simpler service injection |
| Traditional N-Layer (DAL/BLL/UI) | Direction of dependencies goes wrong — BLL ends up depending on DAL abstractions; harder to unit test |
| Minimal APIs only | Good for microservices; too flat for a monolith with 17 services and 82+ endpoints |

## Consequences

**Good:**
- Services are unit-testable with a mocked `IUnitOfWork` — no real DB needed
- Infrastructure can be swapped (e.g., switch from EF Core to Dapper) without touching Application
- New developers know exactly where code goes

**Watch out for:**
- Mapping overhead (AutoMapper `MappingProfile.cs`) — keep profiles lean
- Avoid letting controllers grow fat; if a controller method exceeds ~15 lines, move logic to a service
- Do not add EF Core references to Application layer — that breaks the dependency rule

## Enforcement

The architecture test project (`ECommerce.Tests/Architecture/`) has NetArchTest rules that fail the build if any layer violates the dependency direction.
