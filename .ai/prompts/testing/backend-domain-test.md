# Prompt: Backend Domain Test

Use this prompt when adding or changing a domain aggregate or value object. Paste the production code at the bottom, send to Claude.

---

```
You are writing MSTest domain unit tests for a bounded context in this DDD/CQRS e-commerce repository.

## STEP 1 — Extract before generating (mandatory)

Before writing a single test, read the pasted code and explicitly list:
- Every public method on the aggregate or value object
- Every Result.Fail() call with its exact error code string (copy verbatim from the code)
- Every domain event raised (class name + which method raises it)

If you cannot find an item in the pasted code, write "MISSING: [item]" and do not invent it.
Generate tests ONLY for items you found in the extraction step.

## Conventions (non-negotiable — read before writing a single line)

LAYER: Domain (Layer 1)
PROJECT: src/backend/<BC>/ECommerce.<BC>.Tests/Domain/
DEPS: Pure C# only. No EF Core. No Moq. No DI. No MediatR.
NAMING: Subject_Scenario_ExpectedOutcome inside nested [TestClass] per method
CLASS: <AggregateName>Tests with nested [TestClass] public class <MethodName> { }
ASSERTIONS: Shouldly — result.IsSuccess.ShouldBeFalse(), not Assert.IsFalse

## Structure every test class must have

1. A private static factory helper: CreateValid<AggregateName>() with sensible defaults.
   Each test overrides only what it cares about.

2. Tests grouped by nested [TestClass] matching each public method — NOT #region.

3. Every [TestMethod] body has exactly three labelled sections:
   // Arrange
   // Act
   // Assert

4. Explicit types — not var — except for anonymous objects.

## What to generate

For EVERY public aggregate/VO method found in Step 1:
  - 1 success test: verify returned value AND state change AND event raised (if any)
  - 1 failure test per Result.Fail branch found in Step 1: verify IsSuccess == false AND exact error code
  - Use [DataTestMethod] + [DataRow] when multiple inputs map to the same error code

## Assertion patterns (Shouldly — mandatory)

// Failure
Result<Product> result = Product.Create("", 10m);
result.IsSuccess.ShouldBeFalse();
result.GetErrorOrThrow().Code.ShouldBe("CATALOG_PRODUCT_NAME_EMPTY");

// Success + state
Result<Product> result = Product.Create("Widget", 10m);
result.IsSuccess.ShouldBeTrue();
Product product = result.GetDataOrThrow();
product.Name.Value.ShouldBe("Widget");

// Domain events
product.DomainEvents.ShouldHaveSingleItem();
ProductCreatedEvent evt = product.DomainEvents.OfType<ProductCreatedEvent>().Single();
evt.Name.ShouldBe("Widget");

// Exceptions (only when the code throws — prefer Result pattern)
Should.Throw<ArgumentException>(() => new SomeValueObject(null));

## NEVER do these
- Do NOT use Assert.IsFalse / Assert.AreEqual — use Shouldly
- Do NOT use Assert.ThrowsException — use Should.Throw<T>() from Shouldly
- Do NOT use #region — use nested [TestClass] instead
- Do NOT import EF Core, MediatR, or any DI container
- Do NOT test private methods
- Do NOT invent error codes — only use codes found in Step 1
- Do NOT add XML doc comments
- Do NOT create helper classes beyond the single CreateValid factory
- Do NOT generate multiple files

## After writing
Run: dotnet test src/backend/<BC>/ECommerce.<BC>.Tests/ECommerce.<BC>.Tests.csproj
All tests must PASS.

---

## STEP 1 output (fill this before generating)

Public methods found: [LIST]
Result.Fail() calls found: [LIST with exact error code strings]
Domain events raised: [LIST]

---

## Code to test

[PASTE THE AGGREGATE OR VALUE OBJECT CLASS HERE]

[PASTE the error codes file if separate, e.g. CatalogErrors.cs — exact strings are required]
```
