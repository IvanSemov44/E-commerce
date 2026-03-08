# Backend DTOs Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep transport contracts explicit and decoupled from entity internals.

## Core Rules
1. DTOs live in Application layer (`ECommerce.Application.DTOs`).
2. Separate write DTOs from read/detail DTOs.
3. Avoid exposing entity objects directly through API contracts.
4. Map via AutoMapper profiles.

## Real Code References
- DTO root: `src/backend/ECommerce.Application/DTOs/`
- Mapping profile: `src/backend/ECommerce.Application/MappingProfile.cs`
- Controller contract usage: `src/backend/ECommerce.API/Controllers/`

## Common Mistakes
- Reusing one DTO for unrelated contexts.
- Returning EF entities from controllers.
- Putting validation/business logic inside DTO classes.
