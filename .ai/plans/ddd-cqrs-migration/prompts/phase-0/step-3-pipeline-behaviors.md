# Phase 0, Step 3: Create Pipeline Behaviors

**Prerequisite**: Step 2 (MediatR configured in Program.cs) must be complete.

---

## Prompt for Descriptor (Kilo Code)

```
# Role: Descriptor for DDD/CQRS Migration — Phase 0, Step 3

You are the Descriptor. Steps 1 and 2 are done. Now guide me through Step 3:
creating the 4 pipeline behaviors and wiring them into the MediatR pipeline.

## Read These Files First

1. `.ai/plans/ddd-cqrs-migration/phases/phase-0-foundation.md` — Step 3 section
2. `.ai/plans/ddd-cqrs-migration/theory/02-cqrs-and-mediatr.md` — pipeline behavior theory
3. `.ai/plans/ddd-cqrs-migration/rules.md` — especially rules 18–24 (CQRS) and 36–39 (Authorization)

## Your Task

Guide me through Step 3 only. Cover all 4 behaviors in order:
1. LoggingBehavior
2. PerformanceBehavior
3. ValidationBehavior
4. TransactionBehavior

For each:
1. Explain what it does and where it sits in the pipeline
2. Provide the implementation prompt
3. Explain the order significance (why Logging is outermost, Transaction is innermost)

After all 4 are registered, verify the pipeline order in Program.cs.

## Constraints
- ITransactionalCommand must be imported from ECommerce.SharedKernel.Interfaces, NOT defined inline
- ValidationBehavior throws ValidationException (caught by GlobalExceptionHandler) — not Result.Fail
- The pipeline order is: Logging → Performance → Validation → Transaction → Handler
- App must compile and all existing endpoints must still work
```

---

## Prompt for Programmer

```
# Task: Create 4 MediatR Pipeline Behaviors — Phase 0 Step 3

## Context

MediatR is installed. Now create 4 pipeline behaviors that act as middleware
for every request. They run in this order for every command/query:

Logging → Performance → Validation → Transaction → Handler → (reverse order back)

Location for all behavior files: `src/backend/ECommerce.API/Behaviors/`

## Instructions

### File 1: `LoggingBehavior.cs`

```csharp
using MediatR;
using System.Diagnostics;

namespace ECommerce.API.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("→ Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation(
            "← Handled {RequestName} in {ElapsedMs}ms",
            requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

### File 2: `PerformanceBehavior.cs`

```csharp
using MediatR;
using System.Diagnostics;

namespace ECommerce.API.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int WarningThresholdMs = 500;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
        {
            _logger.LogWarning(
                "Slow handler: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms). Request: {@Request}",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                WarningThresholdMs,
                request);
        }

        return response;
    }
}
```

### File 3: `ValidationBehavior.cs`

```csharp
using FluentValidation;
using MediatR;

namespace ECommerce.API.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            // Throws ValidationException — caught by GlobalExceptionHandler → 400 response.
            // This is different from Result.Fail(): exceptions are for malformed input,
            // Result.Fail() is for valid input the domain rejected for business reasons.
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

### File 4: `TransactionBehavior.cs`

```csharp
using MediatR;
using ECommerce.SharedKernel.Interfaces; // ITransactionalCommand and IUnitOfWork live here

namespace ECommerce.API.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only commands that opt in with ITransactionalCommand get a transaction
        if (request is not ITransactionalCommand)
            return await next();

        // Don't nest transactions
        if (_unitOfWork.HasActiveTransaction)
            return await next();

        var requestName = typeof(TRequest).Name;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("Transaction started for {RequestName}", requestName);

            var response = await next();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _logger.LogDebug("Transaction committed for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction rolled back for {RequestName}", requestName);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

### Update Program.cs

Replace the AddMediatR block from Step 2 with:

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // Pipeline order — outer to inner (each wraps all that follow it):
    //   Logging → Performance → Validation → Transaction → Handler
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});
```

### Build and verify

```bash
dotnet build
dotnet run --project ECommerce.API/ECommerce.API.csproj
```

## Rules to Follow
- ITransactionalCommand comes from `ECommerce.SharedKernel.Interfaces` — do NOT define it here
- Behaviors location: `ECommerce.API/Behaviors/`
- TransactionBehavior only activates for `ITransactionalCommand` requests
- ValidationBehavior throws ValidationException (not returns Result.Fail)

## Acceptance Criteria
- [ ] 4 behavior files created in `src/backend/ECommerce.API/Behaviors/`
- [ ] All 4 registered in Program.cs in order: Logging, Performance, Validation, Transaction
- [ ] `ITransactionalCommand` imported from `ECommerce.SharedKernel.Interfaces`
- [ ] `dotnet build` succeeds
- [ ] App runs, existing endpoints still work
- [ ] No compile error about missing ITransactionalCommand definition
```

---

## After Step 3 Completes

Move to `prompts/phase-0/step-4-domain-event-dispatcher.md`.

The Tester should verify:
1. All 4 files exist in `ECommerce.API/Behaviors/`
2. `TransactionBehavior.cs` has no local `interface ITransactionalCommand` — it uses the import
3. Pipeline order in Program.cs is Logging → Performance → Validation → Transaction
4. Build succeeds, app runs
5. `grep -r "interface ITransactionalCommand" src/backend/ECommerce.API` returns no results
