# Phase 0, Step 2: Install and Configure MediatR

**Prerequisite**: Step 1 (SharedKernel) must be complete and building.

---

## Prompt for Descriptor (Kilo Code)

Copy this into a new Kilo Code chat:

```
# Role: Descriptor for DDD/CQRS Migration — Phase 0, Step 2

You are the Descriptor. Step 1 (SharedKernel) is done. Now guide me through Step 2:
installing and configuring MediatR in the API project.

## Read These Files First

1. `.ai/plans/ddd-cqrs-migration/phases/phase-0-foundation.md` — focus on Step 2
2. `.ai/plans/ddd-cqrs-migration/rules.md` — especially CQRS rules 18–24
3. `.ai/plans/ddd-cqrs-migration/prompts/roles.md` — your role

## Your Task

Guide me through Step 2 only. After I confirm it is done, stop — Step 3 has its own prompt file.

For this step:
1. Explain what MediatR is doing in this architecture (mediator pattern, pipeline, DI)
2. Give me the exact NuGet commands
3. Show me the Program.cs changes needed
4. Define verification criteria

## Constraints
- The app must compile and run after this step
- No commands or queries are created yet — just the infrastructure wiring
- MediatR is registered but the pipeline behaviors are empty for now (behaviors come in Step 3)
```

---

## Prompt for Programmer

```
# Task: Install and Configure MediatR — Phase 0 Step 2

## Context

SharedKernel is complete. Now we wire MediatR into the API project so commands and queries
can be dispatched. No handlers exist yet — we are only setting up the infrastructure.

## Instructions

### 1. Install NuGet packages

```bash
cd src/backend

# MediatR in API (for ISender/IMediator injection in controllers)
dotnet add ECommerce.API/ECommerce.API.csproj package MediatR

# Add project reference: API → SharedKernel
dotnet add ECommerce.API/ECommerce.API.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
```

Note: SharedKernel already has MediatR (added in Step 1 for INotification). Do NOT add it again.

### 2. Update Program.cs

Find the existing service registrations section and add:

```csharp
// ── MediatR ──────────────────────────────────────────────────────────────────
// Phase 0: register from API assembly only (no handlers here yet).
// Each bounded context's Application assembly will be added as we migrate:
//   Phase 1: cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly)
//   Phase 2: cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly)
//   ...and so on through Phase 7
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
```

### 3. Verify MediatR is injectable

Add a temporary health check or startup log to confirm MediatR resolves from DI.
You can do this by injecting IMediator into a minimal test endpoint in Program.cs
and removing it after confirming:

```csharp
// Temporary — remove after confirming DI works
app.MapGet("/health/mediatr", (IMediator mediator) => Results.Ok("MediatR OK"));
```

### 4. Build and run

```bash
dotnet build
dotnet run --project ECommerce.API/ECommerce.API.csproj
# Hit /health/mediatr — should return 200
```

Remove the temporary endpoint after confirming.

## Rules (from rules.md)
- Rule 20: One handler per request
- Rule 24: Commands carry data, not services

## Acceptance Criteria
- [ ] `MediatR` NuGet package installed in ECommerce.API
- [ ] `ECommerce.API` references `ECommerce.SharedKernel`
- [ ] `AddMediatR(...)` registered in Program.cs with comment explaining multi-assembly plan
- [ ] `dotnet build` succeeds for entire solution
- [ ] App runs without errors
- [ ] No existing functionality broken
```

---

## After Step 2 Completes

Move to `prompts/phase-0/step-3-pipeline-behaviors.md`.

The Tester should verify:
1. `IMediator` resolves from DI without exception
2. `Program.cs` has the multi-assembly comment so future context assemblies won't be forgotten
3. No existing service registrations were removed or broken
4. Solution builds cleanly
