# Prompt: Backend Integration Endpoint Test

Use this prompt when adding or changing a controller endpoint. Paste the controller and DTO classes.

---

```
You are writing MSTest integration tests for an HTTP endpoint in this DDD/CQRS e-commerce repository.

## Conventions (non-negotiable)

LAYER: Integration (Layer 3)
PROJECT: src/backend/ECommerce.Tests/
FILE: src/backend/ECommerce.Tests/Integration/<Context>ControllerTests.cs
DEPS: TestWebApplicationFactory (InMemory EF Core + ConditionalTestAuthHandler)
NAMING: HTTP_VERB_Scenario_ExpectedOutcome (e.g. POST_ValidProduct_Returns201WithId)
CLASS: <Context>ControllerTests

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

## Required test matrix per endpoint

For WRITE endpoints (POST / PUT / PATCH / DELETE):
  1. <VERB>_ValidRequest_Returns<Code>                → assert status + success==true + key response fields
  2. <VERB>_MissingRequiredField_Returns400           → assert 400 + success==false
  3. <VERB>_InvalidFieldValue_Returns400              → assert 400 + success==false
  4. <VERB>_<BusinessRuleViolation>_Returns422        → assert 422 + success==false + ASSERT ERROR CODE
  5. <VERB>_NonExistentResource_Returns404            → assert 404
  6. <VERB>_Unauthenticated_Returns401                → assert 401
  7. <VERB>_<WrongRole>_Returns403                    → assert 403

For READ endpoints (GET):
  1. GET_ExistingResource_Returns200WithCorrectShape  → assert 200 + all key response fields
  2. GET_NonExistentId_Returns404                     → assert 404
  3. GET_Unauthenticated_Returns401 (if protected)    → assert 401

## Critical rules

1. ALWAYS use TestContext.CancellationToken in every HTTP call:
   await _adminClient.PostAsync(url, body, TestContext.CancellationToken)
   await _adminClient.GetAsync(url, TestContext.CancellationToken)
   For DELETE: await _adminClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url), TestContext.CancellationToken)

2. ALWAYS assert the ErrorCode on 422 responses:
   Assert.AreEqual("CATALOG_SKU_ALREADY_EXISTS", api!.ErrorCode);

3. ALWAYS assert the response body, not just the status code.

4. CREATE test data inside the test — do not rely on seed data for write test prerequisites.

5. Use explicit types, not var (except anonymous objects):
   HttpResponseMessage response = await _adminClient.PostAsync(...);
   ApiResponse<ProductDto>? api = await Deserialize<ApiResponse<ProductDto>>(response);

6. One test class per controller — do not mix products and categories in the same class.

## ApiResponse<T> shape (what Deserialize returns)
{
    bool Success;
    T? Data;
    string? ErrorCode;
    string? Message;
}

## After writing
Run: dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "<Context>ControllerTests"
All tests must PASS.

---

## Code to test

[PASTE THE CONTROLLER CLASS HERE]

[PASTE THE REQUEST/RESPONSE DTO CLASSES HERE]

[PASTE THE ERROR CODES RELEVANT TO THIS CONTROLLER HERE]
```
