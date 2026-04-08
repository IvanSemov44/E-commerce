# Prompt: Backend Characterization Test

Use this prompt BEFORE changing or migrating any existing behavior. Written first, code changed second.

---

```
You are writing characterization tests for the [CONTEXT] bounded context BEFORE a migration or refactor.

## Purpose
These tests document the current observable behavior of the existing code.
They must PASS now, before any changes are made.
After the migration, the same tests must still pass — proving no regression.

## IMPORTANT SEQUENCE
1. Write these tests → run → ALL PASS (documenting current behavior)
2. Make the code change
3. Run again → ALL still PASS → migration is safe
If any test fails after step 1, fix the test — the behavior being documented is wrong.
If any test fails after step 3, fix the production code — the migration introduced a regression.

## Conventions (non-negotiable)

LAYER: Characterization (Layer 4)
PROJECT: src/backend/ECommerce.Tests/
FILE: src/backend/ECommerce.Tests/Integration/[Context]CharacterizationTests.cs
NAMING: HTTP_VERB_Scenario_ExpectedOutcome
CLASS: [Context]CharacterizationTests

## Test class boilerplate (copy exactly)

[TestClass]
public class [Context]CharacterizationTests
{
    private static TestWebApplicationFactory _factory = null!;
    private static HttpClient _adminClient = null!;
    private static HttpClient _customerClient = null!;
    private static HttpClient _anonClient = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new TestWebApplicationFactory();
        _adminClient    = _factory.CreateAdminClient();
        _customerClient = _factory.CreateAuthenticatedClient();
        _anonClient     = _factory.CreateUnauthenticatedClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _adminClient.Dispose();
        _customerClient.Dispose();
        _anonClient.Dispose();
        await _factory.DisposeAsync();
    }

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private static StringContent Serialize(object dto) =>
        new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

    private static async Task<T?> Deserialize<T>(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }
}

## Required matrix for EVERY endpoint in scope

Write all of these for each write endpoint:
  POST_ValidRequest_Returns<Code>WithBody
  POST_MissingRequiredField_Returns400
  POST_InvalidValue_Returns400
  POST_<BusinessRuleViolation>_Returns422WithErrorCode   ← MOST IMPORTANT — assert the ErrorCode
  POST_Unauthenticated_Returns401
  POST_<WrongRole>_Returns403
  POST_NotFound_Returns404 (if applicable)

Write all of these for each read endpoint:
  GET_ExistingResource_Returns200WithCorrectShape
  GET_NonExistentId_Returns404
  GET_Unauthenticated_Returns401 (if protected)

## Endpoints in scope
[LIST EVERY ENDPOINT TO COVER HERE]
Example:
- POST /api/products
- PUT /api/products/{id}
- DELETE /api/products/{id}
- GET /api/products/{id}
- GET /api/products

## Critical assertion rules

1. ALWAYS assert ErrorCode on 422:
   Assert.AreEqual("CATALOG_SKU_ALREADY_EXISTS", api!.ErrorCode);

2. ALWAYS assert response body, not just status code.

3. ALWAYS use TestContext.CancellationToken in every HTTP call.

4. CREATE test data inside the test — do not rely on pre-seeded data for write tests.

## After writing
Run: dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "[Context]CharacterizationTests"
ALL tests must PASS before you make any code changes.

---

## Context

[DESCRIBE WHAT IS ABOUT TO BE CHANGED — e.g., "Migrating ProductsService to CQRS handlers"]

## Endpoints to cover

[LIST THE ENDPOINTS]

## Current controller code

[PASTE THE CURRENT CONTROLLER]

## Error codes in use

[PASTE THE ERROR CODES FILE]
```
