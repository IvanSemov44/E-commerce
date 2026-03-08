# Backend Entities Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Define domain entity conventions to keep domain model consistent and persistence-safe.

## Core Rules
1. Entities belong to Core domain (`ECommerce.Core.Entities`).
2. Keep domain invariants explicit through methods/constructors when possible.
3. Avoid leaking transport/API concerns into entities.
4. Use navigation properties intentionally for aggregate boundaries.

## Real Code References
- Entity folder: `src/backend/ECommerce.Core/Entities/`
- Base/common domain contracts: `src/backend/ECommerce.Core/Common/`
- DbContext mappings: `src/backend/ECommerce.Infrastructure/Data/`

## Common Mistakes
- Adding DTO/UI fields directly to entities.
- Pushing business rules into controller or mapper layers.
- Overexposing mutable setters without need.
