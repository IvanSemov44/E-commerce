# Phase 5, Step 6: Handler Unit Tests

**Prerequisite**: Steps 2 (Application) and 5 (Domain Tests project created) complete.

Write fast, in-memory unit tests for all command and query handlers using fake repositories. No EF, no HTTP, no database.

---

## Task 1: Shared Fakes

`ECommerce.Promotions.Tests/Handlers/Fakes.cs`

```csharp
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.SharedKernel;

namespace ECommerce.Promotions.Tests.Handlers;

// ── Repository fake ─────────────────────────────────────────────────

public class FakePromoCodeRepository : IPromoCodeRepository
{
    private readonly Dictionary<Guid, PromoCode> _store = new();

    public void Seed(PromoCode promo) => _store[promo.Id] = promo;

    public Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(p => p.Code.Value == normalizedCode));

    public Task<(List<PromoCode> Items, int TotalCount)> GetActiveAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var active = _store.Values.Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt).ToList();
        var items  = active.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, active.Count));
    }

    public Task<(List<PromoCode> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default)
    {
        var q = _store.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Code.Value.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (isActive.HasValue)
            q = q.Where(p => p.IsActive == isActive.Value);
        var all   = q.OrderByDescending(p => p.CreatedAt).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, all.Count));
    }

    public Task UpsertAsync(PromoCode promo, CancellationToken ct = default)
    {
        _store[promo.Id] = promo;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PromoCode promo, CancellationToken ct = default)
    {
        _store.Remove(promo.Id);
        return Task.CompletedTask;
    }

    public int Count => _store.Count;
    public bool Contains(Guid id) => _store.ContainsKey(id);
}

// ── UnitOfWork fake ─────────────────────────────────────────────────

public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}
```

---

## Task 2: Helper — build a valid PromoCode aggregate

`ECommerce.Promotions.Tests/Handlers/PromoCodeFactory.cs`

```csharp
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Tests.Handlers;

public static class PromoCodeFactory
{
    public static PromoCode Active(string code = "SAVE20", decimal percent = 20) =>
        PromoCode.Create(
            PromoCodeString.Create(code).Value!,
            DiscountValue.Percentage(percent).Value!,
            validPeriod: null);

    public static PromoCode WithMaxUses(string code, int maxUses, int usedCount = 0)
    {
        var promo = Active(code);
        typeof(PromoCode).GetProperty("MaxUses")!.SetValue(promo, maxUses);
        typeof(PromoCode).GetProperty("UsedCount")!.SetValue(promo, usedCount);
        return promo;
    }
}
```

---

## Task 3: Command Handler Tests

`ECommerce.Promotions.Tests/Handlers/CommandHandlerTests.cs`

```csharp
using ECommerce.Promotions.Application.Commands;
using ECommerce.Promotions.Domain.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Handlers;

[TestClass]
public class CommandHandlerTests
{
    // ── CreatePromoCodeCommand ───────────────────────

    [TestMethod]
    public async Task CreatePromoCode_ValidData_ReturnsDetailDto()
    {
        var repo    = new FakePromoCodeRepository();
        var uow     = new FakeUnitOfWork();
        var handler = new CreatePromoCodeCommandHandler(repo, uow);

        var cmd = new CreatePromoCodeCommand(
            Code: "SUMMER10", DiscountType: "Percentage", DiscountValue: 10,
            MinOrderAmount: null, MaxDiscountAmount: null, MaxUses: null,
            StartDate: null, EndDate: null, IsActive: true);

        var result = await handler.Handle(cmd, default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SUMMER10", result.Value!.Code);
        Assert.IsTrue(result.Value.IsActive);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task CreatePromoCode_NormalizesCodeToUpper()
    {
        var repo    = new FakePromoCodeRepository();
        var uow     = new FakeUnitOfWork();
        var handler = new CreatePromoCodeCommandHandler(repo, uow);

        var cmd = new CreatePromoCodeCommand(
            Code: "summer10", DiscountType: "Percentage", DiscountValue: 10,
            MinOrderAmount: null, MaxDiscountAmount: null, MaxUses: null,
            StartDate: null, EndDate: null, IsActive: true);

        var result = await handler.Handle(cmd, default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SUMMER10", result.Value!.Code);
    }

    [TestMethod]
    public async Task CreatePromoCode_DuplicateCode_ReturnsDuplicateCodeError()
    {
        var repo  = new FakePromoCodeRepository();
        var uow   = new FakeUnitOfWork();
        repo.Seed(PromoCodeFactory.Active("SAVE20"));
        var handler = new CreatePromoCodeCommandHandler(repo, uow);

        var cmd = new CreatePromoCodeCommand(
            Code: "SAVE20", DiscountType: "Percentage", DiscountValue: 10,
            MinOrderAmount: null, MaxDiscountAmount: null, MaxUses: null,
            StartDate: null, EndDate: null, IsActive: true);

        var result = await handler.Handle(cmd, default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("DUPLICATE_PROMO_CODE", result.Error!.Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task CreatePromoCode_InvalidDiscountType_Returns400Error()
    {
        var repo    = new FakePromoCodeRepository();
        var uow     = new FakeUnitOfWork();
        var handler = new CreatePromoCodeCommandHandler(repo, uow);

        var cmd = new CreatePromoCodeCommand(
            Code: "TEST123", DiscountType: "NotAType", DiscountValue: 10,
            MinOrderAmount: null, MaxDiscountAmount: null, MaxUses: null,
            StartDate: null, EndDate: null, IsActive: true);

        var result = await handler.Handle(cmd, default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task CreatePromoCode_WithValidDates_Succeeds()
    {
        var repo    = new FakePromoCodeRepository();
        var uow     = new FakeUnitOfWork();
        var handler = new CreatePromoCodeCommandHandler(repo, uow);

        var cmd = new CreatePromoCodeCommand(
            Code: "XMAS25", DiscountType: "Fixed", DiscountValue: 25,
            MinOrderAmount: 100, MaxDiscountAmount: null, MaxUses: 500,
            StartDate: DateTime.UtcNow.AddDays(-1), EndDate: DateTime.UtcNow.AddDays(30),
            IsActive: true);

        var result = await handler.Handle(cmd, default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100m, result.Value!.MinOrderAmount);
        Assert.AreEqual(500,  result.Value.MaxUses);
    }

    // ── UpdatePromoCodeCommand ───────────────────────

    [TestMethod]
    public async Task UpdatePromoCode_SetIsActiveFalse_UpdatesAggregate()
    {
        var repo  = new FakePromoCodeRepository();
        var uow   = new FakeUnitOfWork();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new UpdatePromoCodeCommandHandler(repo, uow);

        var cmd = new UpdatePromoCodeCommand(
            promo.Id, Code: null, DiscountType: null, DiscountValue: null,
            MinOrderAmount: null, ClearMinOrderAmount: false,
            MaxDiscountAmount: null, ClearMaxDiscountAmount: false,
            MaxUses: null, ClearMaxUses: false,
            StartDate: null, EndDate: null, ClearDates: false,
            IsActive: false);

        var result = await handler.Handle(cmd, default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.Value!.IsActive);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task UpdatePromoCode_UnknownId_ReturnsPromoNotFound()
    {
        var repo    = new FakePromoCodeRepository();
        var uow     = new FakeUnitOfWork();
        var handler = new UpdatePromoCodeCommandHandler(repo, uow);

        var cmd = new UpdatePromoCodeCommand(
            Guid.NewGuid(), null, null, null, null, false, null, false, null, false,
            null, null, false, null);

        var result = await handler.Handle(cmd, default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_CODE_NOT_FOUND", result.Error!.Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    [TestMethod]
    public async Task UpdatePromoCode_DuplicateNewCode_ReturnsDuplicateError()
    {
        var repo  = new FakePromoCodeRepository();
        var uow   = new FakeUnitOfWork();
        var promoA = PromoCodeFactory.Active("CODEA");
        var promoB = PromoCodeFactory.Active("CODEB");
        repo.Seed(promoA);
        repo.Seed(promoB);
        var handler = new UpdatePromoCodeCommandHandler(repo, uow);

        // Try to rename promoB to CODEA (which already exists)
        var cmd = new UpdatePromoCodeCommand(
            promoB.Id, Code: "CODEA", null, null, null, false, null, false, null, false,
            null, null, false, null);

        var result = await handler.Handle(cmd, default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("DUPLICATE_PROMO_CODE", result.Error!.Code);
    }

    // ── DeactivatePromoCodeCommand ───────────────────

    [TestMethod]
    public async Task DeactivatePromoCode_ExistingCode_SetsIsActiveFalse()
    {
        var repo  = new FakePromoCodeRepository();
        var uow   = new FakeUnitOfWork();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new DeactivatePromoCodeCommandHandler(repo, uow);

        var result = await handler.Handle(new DeactivatePromoCodeCommand(promo.Id), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(repo.GetByIdAsync(promo.Id).Result!.IsActive);
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task DeactivatePromoCode_UnknownId_ReturnsPromoNotFound()
    {
        var repo    = new FakePromoCodeRepository();
        var uow     = new FakeUnitOfWork();
        var handler = new DeactivatePromoCodeCommandHandler(repo, uow);

        var result = await handler.Handle(new DeactivatePromoCodeCommand(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_CODE_NOT_FOUND", result.Error!.Code);
        Assert.AreEqual(0, uow.SaveCount);
    }

    // ── DeletePromoCodeCommand ───────────────────────

    [TestMethod]
    public async Task DeletePromoCode_ExistingCode_RemovesFromRepository()
    {
        var repo  = new FakePromoCodeRepository();
        var uow   = new FakeUnitOfWork();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new DeletePromoCodeCommandHandler(repo, uow);

        var result = await handler.Handle(new DeletePromoCodeCommand(promo.Id), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(repo.Contains(promo.Id));
        Assert.AreEqual(1, uow.SaveCount);
    }

    [TestMethod]
    public async Task DeletePromoCode_UnknownId_ReturnsPromoNotFound()
    {
        var repo    = new FakePromoCodeRepository();
        var uow     = new FakeUnitOfWork();
        var handler = new DeletePromoCodeCommandHandler(repo, uow);

        var result = await handler.Handle(new DeletePromoCodeCommand(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_CODE_NOT_FOUND", result.Error!.Code);
        Assert.AreEqual(0, uow.SaveCount);
    }
}
```

---

## Task 4: Query Handler Tests

`ECommerce.Promotions.Tests/Handlers/QueryHandlerTests.cs`

```csharp
using ECommerce.Promotions.Application.Queries;
using ECommerce.Promotions.Domain.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Handlers;

[TestClass]
public class QueryHandlerTests
{
    // ── GetPromoCodeByIdQuery ────────────────────────

    [TestMethod]
    public async Task GetPromoCodeById_Found_ReturnsDetailDto()
    {
        var repo  = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new GetPromoCodeByIdQueryHandler(repo);

        var result = await handler.Handle(new GetPromoCodeByIdQuery(promo.Id), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SAVE20", result.Value!.Code);
        Assert.AreEqual(promo.Id, result.Value.Id);
    }

    [TestMethod]
    public async Task GetPromoCodeById_NotFound_ReturnsPromoNotFoundError()
    {
        var repo    = new FakePromoCodeRepository();
        var handler = new GetPromoCodeByIdQueryHandler(repo);

        var result = await handler.Handle(new GetPromoCodeByIdQuery(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PROMO_CODE_NOT_FOUND", result.Error!.Code);
    }

    // ── GetPromoCodesQuery ───────────────────────────

    [TestMethod]
    public async Task GetPromoCodes_ReturnsPaginatedResult()
    {
        var repo = new FakePromoCodeRepository();
        repo.Seed(PromoCodeFactory.Active("CODE1"));
        repo.Seed(PromoCodeFactory.Active("CODE2"));
        repo.Seed(PromoCodeFactory.Active("CODE3"));
        var handler = new GetPromoCodesQueryHandler(repo);

        var result = await handler.Handle(new GetPromoCodesQuery(1, 10, null, null), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(3, result.Value!.TotalCount);
        Assert.AreEqual(3, result.Value.Items.Count);
    }

    [TestMethod]
    public async Task GetPromoCodes_IsActiveFilter_ReturnsOnlyActive()
    {
        var repo  = new FakePromoCodeRepository();
        var active   = PromoCodeFactory.Active("ACTIVE1");
        var inactive = PromoCodeFactory.Active("INACTIVE1");
        inactive.Deactivate();
        repo.Seed(active);
        repo.Seed(inactive);
        var handler = new GetPromoCodesQueryHandler(repo);

        var result = await handler.Handle(new GetPromoCodesQuery(1, 10, null, isActive: true), default);

        Assert.AreEqual(1, result.Value!.TotalCount);
        Assert.AreEqual("ACTIVE1", result.Value.Items[0].Code);
    }

    [TestMethod]
    public async Task GetPromoCodes_SearchFilter_ReturnsMatchingCodes()
    {
        var repo = new FakePromoCodeRepository();
        repo.Seed(PromoCodeFactory.Active("SUMMER20"));
        repo.Seed(PromoCodeFactory.Active("WINTER10"));
        var handler = new GetPromoCodesQueryHandler(repo);

        var result = await handler.Handle(new GetPromoCodesQuery(1, 10, "SUMMER", null), default);

        Assert.AreEqual(1, result.Value!.TotalCount);
        Assert.AreEqual("SUMMER20", result.Value.Items[0].Code);
    }

    // ── GetActivePromoCodesQuery ─────────────────────

    [TestMethod]
    public async Task GetActivePromoCodes_OnlyReturnsActiveCodes()
    {
        var repo     = new FakePromoCodeRepository();
        var active   = PromoCodeFactory.Active("ACTIVE1");
        var inactive = PromoCodeFactory.Active("INACTIVE1");
        inactive.Deactivate();
        repo.Seed(active);
        repo.Seed(inactive);
        var handler = new GetActivePromoCodesQueryHandler(repo);

        var result = await handler.Handle(new GetActivePromoCodesQuery(1, 10), default);

        Assert.AreEqual(1, result.Value!.TotalCount);
        Assert.AreEqual("ACTIVE1", result.Value.Items[0].Code);
    }

    // ── ValidatePromoCodeQuery ───────────────────────

    [TestMethod]
    public async Task ValidatePromoCode_ValidCode_ReturnsIsValidTrue()
    {
        var repo  = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20", 20);
        repo.Seed(promo);
        var handler = new ValidatePromoCodeQueryHandler(repo, new DiscountCalculator());

        var result = await handler.Handle(new ValidatePromoCodeQuery("SAVE20", 100m), default);

        Assert.IsTrue(result.IsSuccess);            // query itself succeeds
        Assert.IsTrue(result.Value!.IsValid);       // the validation result is also valid
        Assert.AreEqual(20m, result.Value.DiscountAmount); // 20% of 100
    }

    [TestMethod]
    public async Task ValidatePromoCode_UnknownCode_ResultIsSuccessWithIsValidFalse()
    {
        // Critical: the query must NEVER return a failed Result for business reasons.
        // Unknown/invalid codes return Result.Ok with IsValid=false inside.
        var repo    = new FakePromoCodeRepository();
        var handler = new ValidatePromoCodeQueryHandler(repo, new DiscountCalculator());

        var result = await handler.Handle(new ValidatePromoCodeQuery("FAKECODE", 100m), default);

        Assert.IsTrue(result.IsSuccess, "Query must succeed even for unknown codes");
        Assert.IsFalse(result.Value!.IsValid);
        Assert.AreEqual(0m, result.Value.DiscountAmount);
    }

    [TestMethod]
    public async Task ValidatePromoCode_InactiveCode_ResultIsSuccessWithIsValidFalse()
    {
        var repo  = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        promo.Deactivate();
        repo.Seed(promo);
        var handler = new ValidatePromoCodeQueryHandler(repo, new DiscountCalculator());

        var result = await handler.Handle(new ValidatePromoCodeQuery("SAVE20", 100m), default);

        Assert.IsTrue(result.IsSuccess, "Query must succeed even for inactive codes");
        Assert.IsFalse(result.Value!.IsValid);
    }

    [TestMethod]
    public async Task ValidatePromoCode_BelowMinOrder_ResultIsSuccessWithIsValidFalse()
    {
        var repo  = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("MINORDER");
        // Manually set MinimumOrderAmount via reflection
        typeof(ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode)
            .GetProperty("MinimumOrderAmount")!
            .SetValue(promo, 50m);
        repo.Seed(promo);
        var handler = new ValidatePromoCodeQueryHandler(repo, new DiscountCalculator());

        var result = await handler.Handle(new ValidatePromoCodeQuery("MINORDER", 30m), default);

        Assert.IsTrue(result.IsSuccess, "Query must succeed even if minimum order not met");
        Assert.IsFalse(result.Value!.IsValid);
    }

    [TestMethod]
    public async Task ValidatePromoCode_CaseInsensitiveLookup_MatchesStoredCode()
    {
        var repo  = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new ValidatePromoCodeQueryHandler(repo, new DiscountCalculator());

        // "save20" in lowercase — handler normalizes before lookup
        var result = await handler.Handle(new ValidatePromoCodeQuery("save20", 100m), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Value!.IsValid);
    }
}
```

---

## Task 5: Run

```bash
cd src/backend
dotnet test ECommerce.Promotions.Tests --logger "console;verbosity=normal"
```

---

## Acceptance Criteria

- [ ] All handler tests pass
- [ ] `ValidatePromoCodeQuery` NEVER returns a failed result — unknown/invalid codes return `IsValid=false` inside a successful result (tested explicitly)
- [ ] `CreatePromoCode` saves once and returns the created DTO
- [ ] `CreatePromoCode` duplicate code → `DUPLICATE_PROMO_CODE` with 0 saves
- [ ] `UpdatePromoCode` unknown id → `PROMO_CODE_NOT_FOUND` with 0 saves
- [ ] `DeactivatePromoCode` sets `IsActive=false` via domain method
- [ ] `DeletePromoCode` removes from repository
- [ ] `GetActivePromoCodes` only returns IsActive=true codes
- [ ] `GetPromoCodes` search and isActive filters work correctly
