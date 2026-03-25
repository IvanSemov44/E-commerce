# Tester Prompt Templates

Copy the relevant prompt, fill in the `[PHASE]` and `[CONTEXT]` placeholders, send to the tester.

---

## Prompt 1: Characterization Tests (BEFORE migration)

```
You are writing characterization tests for the [CONTEXT] context BEFORE the DDD migration.

## Purpose
These tests document the current behavior of the OLD service.
They run against the existing code and must PASS now.
After migration to DDD handlers, these same tests must still pass — proving no regression.

## Framework
- MSTest ([TestClass], [TestMethod], [TestInitialize], [TestCleanup])
- FluentAssertions or MSTest Assert
- Project: src/backend/ECommerce.Tests/Integration/
- File: src/backend/ECommerce.Tests/Integration/[Context]CharacterizationTests.cs

## Auth helpers available
- _factory.CreateAdminClient()        — authenticated as Admin
- _factory.CreateAuthenticatedClient() — authenticated as Customer
- _factory.CreateUnauthenticatedClient() — no auth token

## TestContext (add to class — required for CancellationToken)
public TestContext TestContext { get; set; } = null!;

## Serialize / Deserialize helpers (add to class)
private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
private static StringContent Serialize(object dto) =>
    new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
private static async Task<T> Deserialize<T>(HttpResponseMessage response)
{
    string content = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<T>(content, _jsonOptions)!;
}

## Endpoints to cover
[LIST THE ENDPOINTS HERE — e.g.]
- POST /api/products
- PUT /api/products/{id}
- DELETE /api/products/{id}
- GET /api/products/{id}
- GET /api/products/slug/{slug}

## For every write endpoint (POST/PUT/DELETE), write tests for:
1. Happy path → assert status code AND response body (success, data fields, id)
2. Not found → assert 404
3. Validation failure (missing required field or invalid value) → assert 400, success==false
4. Business rule violation (duplicate, invalid state) → assert 422, success==false, AND errorCode matches ErrorCodes.X.Y
5. Unauthenticated → assert 401
6. Wrong role → assert 403

## For every read endpoint (GET), write tests for:
1. Happy path → assert 200, data not null, correct shape
2. Not found (if endpoint takes an ID or slug) → assert 404
3. Auth if endpoint is protected → 401/403

## Critical rule
On 422 responses, ALWAYS assert the errorCode. Example:
    Assert.AreEqual("CATALOG_SKU_ALREADY_EXISTS", body.ErrorCode);
This is the most important assertion — it proves the business rule error code is preserved after migration.

## Code style — apply to every test method
1. **AAA comments** — every [TestMethod] body must have `// Arrange`, `// Act`, `// Assert`.
   For tests with prerequisite setup steps, label them: `// Arrange — create prerequisite X`.
   Do NOT add AAA comments to [TestInitialize] or [TestCleanup].

2. **CancellationToken** — pass `TestContext.CancellationToken` to every HTTP call:
   - `client.GetAsync(url, TestContext.CancellationToken)`
   - `client.PostAsync(url, body, TestContext.CancellationToken)`
   - `client.PutAsync(url, body, TestContext.CancellationToken)`
   - DELETE: `client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url), TestContext.CancellationToken)`

3. **Explicit types** — no implicit `var` except on anonymous object initializers:
   - `HttpResponseMessage res = await client.GetAsync(...);`
   - `ApiResponse<JsonElement>? api = await Deserialize<JsonElement>(res);`
   - `Guid id = Guid.Parse(...);`
   - `string body = await res.Content.ReadAsStringAsync();`
   - Anonymous objects (`var create = new { Name = "X" }`) are the only exception.

4. **One file per context** — Products and Categories must be in separate files/classes.

## Do NOT
- Only check status codes — always verify response body
- Rely on hardcoded seed GUIDs for write tests — create data within the test
- Share state between tests

## After writing
Run: dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "[Context]CharacterizationTests"
All tests must PASS before migration begins.
```

---

## Prompt 2: Domain Unit Tests (AFTER aggregate is built)

```
You are writing domain unit tests for the [CONTEXT] bounded context.

## Framework
- MSTest ([TestClass], [TestMethod])
- No database, no HTTP, no mocks — pure C# only
- Project: src/backend/ECommerce.Tests/Unit/ (or new ECommerce.[Context].Tests/)

## What to test

### Aggregate factory methods
- Valid input → aggregate created with correct state
- Valid input → correct domain event raised
- Each invalid input → returns `Result` with `IsSuccess == false` and correct `Error.Code`

### Aggregate domain methods (UpdatePrice, Deactivate, AddImage, etc.)
- Success path → state changed correctly, returns `Result.Ok()` or `Result<T>.Ok(value)`
- Success path → correct domain event raised (if any)
- Invariant violated → returns `Result` with `IsSuccess == false` and correct `Error.Code`

### Value object validation
- Valid inputs → `IsSuccess == true`, value is correct
- Each invalid input (empty, null, negative, too long, wrong format) → `IsSuccess == false`, `Error.Code` matches `{Context}Errors.*`

## Assertion pattern for failures

```csharp
// Arrange
// Act
Result<ProductName> result = ProductName.Create("");

// Assert
Assert.IsFalse(result.IsSuccess);
Assert.AreEqual("PRODUCT_NAME_EMPTY", result.GetErrorOrThrow().Code);
```

For aggregate methods that return non-generic `Result`:
```csharp
// Act
Result result = product.Deactivate();

// Assert
Assert.IsFalse(result.IsSuccess);
Assert.AreEqual("PRODUCT_DISCONTINUED", result.GetErrorOrThrow().Code);
```

## Pattern for each test
[TestMethod]
public void [Method]_[Scenario]_[ExpectedOutcome]()
{
    // Arrange — set up inputs
    // Act — call the method
    // Assert — verify state, events, or exception
}

## What NOT to test
- EF Core configuration or mappings
- Properties (don't test that Name == "X" after setting Name = "X")
- Infrastructure code
- Anything requiring a database

## After writing
Run: dotnet test --filter "[Context]"
All tests must PASS.
```

---

## Prompt 3: Handler Unit Tests (AFTER handlers are built)

```
You are writing handler unit tests for the [CONTEXT] bounded context.

## Framework
- MSTest ([TestClass], [TestMethod])
- Moq for mocking repositories and UnitOfWork
- Project: src/backend/ECommerce.Tests/Unit/

## What to test for each command handler
1. Valid command → repository.AddAsync called once
2. Valid command → unitOfWork.SaveChangesAsync called once
3. Valid command → returns Result.Ok with correct data
4. Resource not found → returns Result.Fail with correct ErrorCode
5. Business rule violated → returns Result.Fail with correct ErrorCode

## What to test for each query handler
1. Valid query → returns Result.Ok with correct data shape
2. Resource not found → returns Result.Fail with 404 ErrorCode
3. Verify .AsNoTracking() is used (queries must not track entities)

## Mock setup pattern
private readonly Mock<I[Context]Repository> _repoMock = new();
private readonly Mock<IUnitOfWork> _uowMock = new();
private readonly [Handler] _handler;

public [HandlerTests]()
{
    _handler = new [Handler](_repoMock.Object, _uowMock.Object);
}

## What NOT to test
- Domain invariants — that is the domain unit test's job
- The actual SQL generated
- EF Core behavior

## After writing
Run: dotnet test --filter "[Context]"
All tests must PASS.
```

---

## Prompt 4: Verify Characterization Tests After Migration

```
The [CONTEXT] context has been migrated from the old service to DDD handlers.

Run the existing characterization tests to verify no regression:

dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "[Context]CharacterizationTests"

Report:
- Total: X
- Passed: X
- Failed: X
- For each failure: test name + actual vs expected

If any test fails, the migration introduced a regression. Do NOT proceed.
Report the failures so the programmer can fix the handler before the old service is deleted.
```
