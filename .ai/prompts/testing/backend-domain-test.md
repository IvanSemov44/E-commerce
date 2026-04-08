# Prompt: Backend Domain Test

Use this prompt when adding or changing a domain aggregate or value object. Paste the production code at the bottom, send to Claude.

---

```
You are writing MSTest domain unit tests for a bounded context in this DDD/CQRS e-commerce repository.

## Conventions (non-negotiable — read before writing a single line)

LAYER: Domain (Layer 1)
PROJECT: src/backend/<BC>/ECommerce.<BC>.Tests/Domain/
DEPS: Pure C# only. No EF Core. No Moq. No DI. No MediatR.
NAMING: MethodName_Scenario_ExpectedOutcome (e.g. Create_EmptyName_ReturnsFailure)
CLASS: <AggregateName>Tests or <ValueObjectName>Tests

## Structure every test class must have

1. A private static factory helper method: `CreateValid<AggregateName>()` with sensible defaults.
   Override only what the specific test cares about using named parameters.

2. Tests grouped by #region matching each public method:
   #region Create ... #endregion
   #region UpdatePrice ... #endregion

3. Every [TestMethod] body has exactly three labelled sections:
   // Arrange
   // Act
   // Assert
   (Keep the label even if the section is empty)

4. Explicit types — not var — except for anonymous objects:
   Result<Product> result = Product.Create(...);   // GOOD
   var result = Product.Create(...);                // BAD
   var dto = new { Name = "X" };                   // OK (anonymous)

## What to generate

For EVERY public aggregate/VO method:
  - 1 success test: verify returned value AND state change
  - 1 domain event test per method that raises events: verify event type + key property
  - 1 failure test per Result.Fail branch: verify IsSuccess == false AND error code

For value objects specifically:
  - Test every distinct validation rule (empty, null, too long, wrong format, negative, etc.)

## Assertion pattern for failures (mandatory — do not use Assert.ThrowsException)

Result<Product> result = Product.Create("", 10m);
Assert.IsFalse(result.IsSuccess);
Assert.AreEqual("CATALOG_PRODUCT_NAME_EMPTY", result.GetErrorOrThrow().Code);

## Domain event assertion pattern

Product product = result.GetDataOrThrow();
Assert.AreEqual(1, product.DomainEvents.Count);
ProductCreatedEvent evt = product.DomainEvents.OfType<ProductCreatedEvent>().Single();
Assert.AreEqual("Widget", evt.Name);

## What NOT to generate
- Tests that use Assert.ThrowsException — use Result pattern instead
- Tests that import EF Core, MediatR, or any DI container
- Tests that assert on properties not related to the method under test
- Tests for private methods

## After writing
Run: dotnet test src/backend/<BC>/ECommerce.<BC>.Tests/ECommerce.<BC>.Tests.csproj
All tests must PASS.

---

## Code to test

[PASTE THE AGGREGATE OR VALUE OBJECT CLASS HERE]

[ALSO PASTE the error codes file if separate, e.g. CatalogErrors.cs]
```
