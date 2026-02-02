# ⚠️ TEST FLAKINESS ALERT

## Current Status
- **Last Committed:** 437/489 passing (89.4%)
- **Current Flaky Range:** 432-435 passing
- **Regression:** -2 to -5 tests variable per run

## Root Cause

Test suite has **parallel test execution flakiness**. The issue:

1. **Database Seeding Issue**: Each test host creates its own in-memory EF Core database
2. **Parallel Execution**: Tests run in parallel and call `ConfigureWebHost()` 
3. **Race Condition**: Multiple tests simultaneously call `db.Database.EnsureDeleted()` and `db.Database.EnsureCreated()`
4. **Non-deterministic Failures**: Different tests fail randomly on each run

## Evidence

```
Run 1: 435/489 passing
Run 2: 432/489 passing  
Run 3: 434/489 passing
Run 4: 432/489 passing
```

Test counts vary by 3-5 tests depending on which ones hit the race condition.

## Solution

Two options:

### Option A: Disable Parallel Test Execution
Add to `ECommerce.Tests/ECommerce.Tests.csproj`:
```xml
<RunTestsInParallel>false</RunTestsInParallel>
```

This will make tests slower but deterministic.

### Option B: Fix In-Memory DB Isolation
Create a unique database per test class instead of per factory instance:
```csharp
private static readonly object _dbLock = new object();
// Use lock to ensure only one test configures DB at a time
```

## Impact on Next Session

- **Don't rely on exact test count** - variance of ±5 tests is normal
- **Focus on pass/fail patterns** not absolute numbers
- **Highest priority: Disable parallel execution** for deterministic results

## Recommended Action

Before continuing with Priority 1-3 implementation:
1. Add `<RunTestsInParallel>false</RunTestsInParallel>` to csproj
2. Run full test suite 3x to confirm deterministic results
3. Then proceed with JWT implementation

This will give us honest metrics to measure progress against.

---

**Created:** February 3, 2026  
**Status:** ⚠️ BLOCKING - Flaky tests prevent accurate progress tracking
