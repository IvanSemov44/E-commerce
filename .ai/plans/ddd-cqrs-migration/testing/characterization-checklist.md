# Characterization Test Checklist

Write these tests BEFORE the migration. Run them. They must pass.
After migration, run them again. They must still pass.

---

## The 5 Scenarios — Every Write Endpoint Needs All 5

For every `POST`, `PUT`, `PATCH`, `DELETE` endpoint:

| # | Scenario | Expected Status | What to Assert in Body |
|---|---------|----------------|------------------------|
| 1 | Happy path | 200 or 201 | `success == true`, `data` not null, key fields correct |
| 2 | Not found | 404 | `success == false` |
| 3 | Validation failure | 400 | `success == false`, `errors` not empty |
| 4 | Business rule violation | 422 | `success == false`, **`errorCode` matches `ErrorCodes.X.Y`** |
| 5 | Unauthenticated | 401 | — |
| 5 | Wrong role | 403 | — |

For `GET` endpoints: happy path + not found (where applicable) + auth if protected.

---

## The Most Common Mistake

```csharp
// ❌ WRONG — only checks status code, misses everything else
Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);

// ✅ CORRECT — also verifies the error code the client receives
Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
var body = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
Assert.AreEqual("CATALOG_SKU_ALREADY_EXISTS", body?.ErrorCode);
```

**Always assert the `errorCode` on 422 responses.** That is the contract the frontend depends on.
After migration, if the handler returns a different error code, the test catches it.

---

## Response Shape Reference

Every API response follows `ApiResponse<T>`:
```json
{
  "success": true,
  "data": { ... },
  "errorCode": null,
  "errors": []
}
```

On failure:
```json
{
  "success": false,
  "data": null,
  "errorCode": "CATALOG_SKU_ALREADY_EXISTS",
  "errors": ["A product with this SKU already exists."]
}
```

---

## MSTest Template for One Endpoint

```csharp
#region POST /api/products — Characterization

[TestMethod]
public async Task CreateProduct_HappyPath_Returns201WithData()
{
    using var client = _factory.CreateAdminClient();
    var dto = new { Name = "Test Product", Price = 29.99m, /* ... */ };
    var content = Serialize(dto);

    var response = await client.PostAsync("/api/products", content);
    var body = await Deserialize<ApiResponse<ProductDetailDto>>(response);

    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    Assert.IsTrue(body.Success);
    Assert.IsNotNull(body.Data);
    Assert.AreNotEqual(Guid.Empty, body.Data.Id);
    Assert.AreEqual("Test Product", body.Data.Name);    // verify data matches input
}

[TestMethod]
public async Task CreateProduct_DuplicateSku_Returns422WithErrorCode()
{
    using var client = _factory.CreateAdminClient();
    var dto = new { Name = "First", Sku = "DUPE-001", Price = 10m, /* ... */ };

    await client.PostAsync("/api/products", Serialize(dto));                    // first — succeeds
    var response = await client.PostAsync("/api/products", Serialize(dto));     // second — fails
    var body = await Deserialize<ApiResponse<object>>(response);

    Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    Assert.IsFalse(body.Success);
    Assert.AreEqual(ErrorCodes.Catalog.SkuAlreadyExists, body.ErrorCode);      // exact error code
}

[TestMethod]
public async Task CreateProduct_MissingName_Returns400()
{
    using var client = _factory.CreateAdminClient();
    var dto = new { Price = 29.99m }; // Name missing

    var response = await client.PostAsync("/api/products", Serialize(dto));
    var body = await Deserialize<ApiResponse<object>>(response);

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.IsFalse(body.Success);
}

[TestMethod]
public async Task CreateProduct_Unauthenticated_Returns401()
{
    using var client = _factory.CreateUnauthenticatedClient();
    var response = await client.PostAsync("/api/products", Serialize(ValidDto()));

    Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}

[TestMethod]
public async Task CreateProduct_CustomerRole_Returns403()
{
    using var client = _factory.CreateAuthenticatedClient(); // Customer role
    var response = await client.PostAsync("/api/products", Serialize(ValidDto()));

    Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
}

#endregion
```

---

## Serialize / Deserialize Helpers

Add these helpers to each test class to reduce boilerplate:

```csharp
private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

private static StringContent Serialize(object dto) =>
    new(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

private static async Task<T> Deserialize<T>(HttpResponseMessage response)
{
    var content = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<T>(content, _jsonOptions)!;
}
```

---

## Per-Endpoint Coverage Matrix

Fill this in when writing tests for each context.
Mark ✅ when test written and passing against OLD service.
Mark 🔄 when passing against NEW handlers after migration.

### Catalog — Products

| Endpoint | Happy | Not Found | Validation | 422 + ErrorCode | 401 | 403 |
|---------|-------|-----------|------------|----------------|-----|-----|
| GET /products | ✅ | — | — | — | — | — |
| GET /products/{id} | ✅ | ✅ | — | — | — | — |
| GET /products/slug/{slug} | ✅ | ✅ | — | — | — | — |
| GET /products/featured | ✅ | — | — | — | — | — |
| POST /products | ✅ | — | ✅ | ❌ | ❌ | ✅ |
| PUT /products/{id} | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| DELETE /products/{id} | ❌ | ✅ | — | — | ❌ | ✅ |

**Missing tests to add before Phase 1 migration:**
- POST: duplicate SKU → 422 with `ErrorCodes.Catalog.SkuAlreadyExists`
- POST: unauthenticated → 401
- PUT: validation failure → 400
- PUT: unauthenticated → 401, wrong role → 403
- DELETE: happy path → 200/204
- DELETE: unauthenticated → 401

### Catalog — Categories

| Endpoint | Happy | Not Found | Validation | 422 + ErrorCode | 401 | 403 |
|---------|-------|-----------|------------|----------------|-----|-----|
| GET /categories | — | — | — | — | — | — |
| GET /categories/{id} | — | — | — | — | — | — |
| GET /categories/slug/{slug} | — | — | — | — | — | — |
| POST /categories | — | — | — | — | — | — |
| PUT /categories/{id} | — | — | — | — | — | — |
| DELETE /categories/{id} | — | — | — | — | — | — |

---

## What NOT to Do

- **Don't rely on seed data GUIDs** for write tests — create data within the test itself
- **Don't only check status code** — always verify the response body for 422 responses
- **Don't share state between tests** — each test must work independently
- **Don't test implementation details** — test the HTTP contract, not which method was called internally
