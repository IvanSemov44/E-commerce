# Prompt: Backend Integration Endpoint Test

Use this prompt when adding or changing a controller endpoint. Paste the controller and DTO classes.

---

```
You are writing MSTest integration tests for an HTTP endpoint in this DDD/CQRS e-commerce repository.

## STEP 1 — Extract before generating (mandatory)

Before writing a single test, read the pasted controller and list:
- Every endpoint method with its HTTP verb + route (e.g. POST /api/products)
- Every [Authorize] or [RequireRole] attribute present
- Every validation attribute or validator referenced
- Every error code string the handler can return (check pasted ErrorCodes)

If you cannot find an item in the pasted code, write "MISSING: [item]" and stop.

## Conventions (non-negotiable)

LAYER: Integration (Layer 3)
PROJECT: src/backend/ECommerce.Tests/
FILE: src/backend/ECommerce.Tests/Integration/<Context>ControllerTests.cs
DEPS: TestWebApplicationFactory (SQLite EF Core + ConditionalTestAuthHandler)
NAMING: HTTP_VERB_Scenario_ExpectedOutcome (e.g. POST_ValidProduct_Returns201WithId)
CLASS: <Context>ControllerTests
ASSERTIONS: Shouldly for all assertions

## Test class boilerplate (copy exactly)

[TestClass]
public class <Context>ControllerTests
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

## Test data uniqueness rule (critical)

All tests in a class share the same SQLite database. Any field with a UNIQUE constraint
(SKU, email, slug, code, etc.) MUST use a random suffix to avoid collisions:

// WRONG — hardcoded strings collide between tests
var body = new { Sku = "SKU-001", Name = "Widget" };

// RIGHT — unique per test run
string sku = $"SKU-{Guid.NewGuid():N}";
var body = new { Sku = sku, Name = $"Widget-{sku}" };

Always use Guid.NewGuid() for any field that has a unique database constraint.

## Required test matrix per endpoint (found in Step 1)

For WRITE endpoints (POST / PUT / PATCH / DELETE):
  1. <VERB>_ValidRequest_Returns<Code>         → status + Success==true + key response fields
  2. <VERB>_MissingRequiredField_Returns400    → 400 + Success==false
  3. <VERB>_InvalidFieldValue_Returns400       → 400 + Success==false
  4. <VERB>_<BusinessRule>Violated_Returns422  → 422 + Success==false + EXACT ErrorCode string
  5. <VERB>_NonExistentResource_Returns404     → 404
  6. <VERB>_Unauthenticated_Returns401         → 401
  7. <VERB>_<WrongRole>_Returns403             → 403

For READ endpoints (GET):
  1. GET_ExistingResource_Returns200WithCorrectShape
  2. GET_NonExistentId_Returns404
  3. GET_Unauthenticated_Returns401 (if protected)

## Critical assertion rules

1. ALWAYS use TestContext.CancellationToken in every HTTP call:
   await _adminClient.PostAsync(url, body, TestContext.CancellationToken)

2. ALWAYS assert the ErrorCode on 422 responses (found in Step 1):
   api!.ErrorCode.ShouldBe("CATALOG_SKU_ALREADY_EXISTS");

3. ALWAYS assert the response body, not just the status code.
   Use Shouldly: api.ShouldNotBeNull(); api.Success.ShouldBeTrue();

4. CREATE test data inside the test using unique identifiers (see uniqueness rule above).

5. Use explicit types, not var:
   HttpResponseMessage response = await _adminClient.PostAsync(...);
   ApiResponse<ProductDto>? api = await Deserialize<ApiResponse<ProductDto>>(response);

## ApiResponse<T> shape
{
    bool Success;
    T? Data;
    string? ErrorCode;
    string? Message;
}

## NEVER do these
- Do NOT use Assert.AreEqual — use Shouldly
- Do NOT hardcode SKU/email/code strings without a Guid suffix
- Do NOT mix multiple controllers in one test class
- Do NOT rely on data seeded by other tests
- Do NOT add XML doc comments
- Do NOT invent ErrorCode strings — only use codes found in Step 1

## After writing
Run: dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "<Context>ControllerTests"
All tests must PASS.

---

## STEP 1 output (fill this before generating)

Endpoints found: [LIST verb + route]
Auth/role requirements: [LIST]
ErrorCode strings: [LIST exact strings from ErrorCodes file]

---

## Code to test

[PASTE THE CONTROLLER CLASS HERE]

[PASTE THE REQUEST/RESPONSE DTO CLASSES HERE]

[PASTE THE ERROR CODES RELEVANT TO THIS CONTROLLER HERE]
```
