# ADR 004 — PostgreSQL as the Primary Database

**Status:** Accepted
**Date:** 2025
**Deciders:** Core team

---

## Context

The e-commerce platform needs a relational database to store orders, users, products, and inventory. The choice affects hosting costs, operational complexity, feature availability, and EF Core compatibility.

## Decision

Use **PostgreSQL** as the primary database, accessed via **Entity Framework Core** with the `Npgsql.EntityFrameworkCore.PostgreSQL` provider.

Key PostgreSQL features we rely on:

| Feature | How we use it |
|---------|--------------|
| `bytea` row version | Optimistic concurrency on Orders, Cart, Products, PromoCodes, Users |
| UUID primary keys | All entities use `uuid` PKs (no integer sequences to manage) |
| DECIMAL(10,2) | Monetary columns — exact precision, no floating point rounding |
| Composite unique indexes | `(CartId, ProductId)`, `(UserId, ProductId)` prevent duplicate rows |
| Transactions | `UnitOfWork.BeginTransactionAsync()` for multi-step order creation |
| Connection resilience | Polly retry policies wrap all DB calls |

## Alternatives considered

| Option | Why rejected |
|--------|-------------|
| SQL Server | License cost; Linux container is heavier; no advantage for this stack |
| MySQL / MariaDB | Weaker support for row-level concurrency tokens in EF Core; less precise decimal handling |
| SQLite | Fine for dev/test only; no concurrent write support for production |
| MongoDB | Our data is highly relational (Order → OrderItems → Products → Categories); document model would fight the domain |

## Consequences

**Good:**
- Open source, zero license cost
- First-class EF Core support via Npgsql
- `RowVersion` concurrency works correctly on PostgreSQL with `bytea`
- Docker image is small and well-maintained
- pgAdmin available for visual inspection during development

**Watch out for:**
- EF Core migrations must be run explicitly (`dotnet ef database update`) — they don't auto-run in production
- `DateTime` columns: always use UTC (`DateTime.UtcNow`), not local time — PostgreSQL stores without timezone
- String comparisons are case-sensitive by default — use `.ToLower()` or `ILIKE` for email lookups
- `decimal` columns need explicit precision in EF config (`HasPrecision(10, 2)`) or EF will warn

## EF Core migration workflow

See `.ai/workflows/database-migrations.md` for the full migration checklist.

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/backend/ECommerce.Infrastructure --startup-project src/backend/ECommerce.API

# Apply to local DB
dotnet ef database update --project src/backend/ECommerce.Infrastructure --startup-project src/backend/ECommerce.API
```
