# .NET Error Checking — Like ESLint for C#

**Date**: March 5, 2026  
**Equivalent**: React/TypeScript ESLint checking

---

## 1. **Build-Time Checking** (`dotnet build`)

Equivalent to: **TypeScript compiler + ESLint**

```bash
dotnet build ECommerce.sln
```

**Catches**:
- ✅ Compilation errors (type mismatches, missing methods)
- ✅ Missing imports, namespace issues
- ✅ Syntax errors

**Output**: Shows file, line, error code
```
error CS0572: 'Success': cannot reference a type through an expression
error CS1061: 'Result<AuthResponseDto>' does not contain a definition for 'User'
```

---

## 2. **In-IDE Diagnostics** (VS Code/Visual Studio)

**Real-time feedback** as you type (like ESLint in VS Code)

- **Visual Studio**: Red squiggly lines under errors
- **VS Code + C# extension**: Roslyn analyzer shows errors/warnings

No command needed — IDE shows errors immediately.

---

## 3. **Code Analyzers** (like ESLint rules)

### Built-in Roslyn Analyzers
```bash
dotnet build ECommerce.sln /p:TreatWarningsAsErrors=true
```

**Common Rules**:
- `CA1062`: Validate arguments
- `CA1304`: Specify CultureInfo
- `CA1819`: Array properties should return ReadOnlyCollections
- `IDE0005`: Remove unnecessary imports

### StyleCop Analyzers (Code Style)
```bash
# Install
cd src/backend
dotnet add package StyleCop.Analyzers
```

Enforces naming, spacing, documentation (like ESLint's `prettier` preset).

---

## 4. **Unit Tests** (Behavioral Verification)

Equivalent to: **Jest tests**

```bash
dotnet test ECommerce.Tests.csproj
```

**What we're seeing now**: Tests fail because `AuthService.RegisterAsync` now returns `Result<AuthResponseDto>` instead of `AuthResponseDto` directly.

Old test:
```csharp
var result = await _authService.RegisterAsync(dto);
result.Success.Should().BeTrue();  // ❌ Result<T> doesn't have .Success property
result.User.Should().NotBeNull();  // ❌ Result<T> doesn't expose .User directly
```

New test (after updating):
```csharp
var result = await _authService.RegisterAsync(dto);

result.Match(
    onSuccess: authResponse =>
    {
        authResponse.Success.Should().BeTrue();
        authResponse.User.Should().NotBeNull();
        return true;
    },
    onFailure: _ => false
).Should().BeTrue();
```

---

## 5. **Comparison: React/TS vs .NET**

| Task | React/TypeScript | .NET C# |
|------|---|---|
| **Type checking** | `tsc` (TypeScript compiler) | `dotnet build` |
| **Linting** | ESLint | Roslyn analyzers + StyleCop |
| **Real-time IDE** | Intellisense + ESLint ext | Roslyn diagnostics |
| **Unit tests** | Jest | MSTest / xUnit |
| **Formatting** | Prettier | dotnet format |
| **CI/CD check** | `npm run lint && npm run test` | `dotnet build && dotnet test` |

---

## 6. **Setting Up CI/CD Like TypeScript**

Create a build check similar to ESLint:

```bash
#!/bin/bash
# scripts/check-build.sh

echo "🔍 Building solution..."
dotnet build ECommerce.sln

if [ $? -ne 0 ]; then
    echo "❌ Build failed! Fix compilation errors."
    exit 1
fi

echo "✅ Build succeeded!"
echo ""
echo "🧪 Running tests..."
dotnet test ECommerce.Tests.csproj --no-build

if [ $? -ne 0 ]; then
    echo "❌ Tests failed!"
    exit 1
fi

echo "✅ All checks passed!"
```

Run before commit:
```bash
./scripts/check-build.sh
```

---

## 7. **What Happens When We Change AuthService Signatures**

**Current Issue**: AuthService now returns `Result<AuthResponseDto>` but tests still expect old signature.

**Steps**:

1. **`dotnet build` fails** — Type mismatch detected
   - Line 96, 197, etc. show errors
   - Tests can't access `.Success`, `.User` directly on `Result<T>`

2. **Fix 1**: Update service tests to handle `Result<T>`
   ```csharp
   var result = await _authService.RegisterAsync(dto);
   result.IsSuccess.Should().BeTrue();
   
   if (result is Result<AuthResponseDto>.Success success)
   {
       success.Data.User.Should().NotBeNull();
   }
   ```

3. **Fix 2**: Update controller tests for new endpoint signatures
   - Controllers now call `result.Match()` instead of catching exceptions

4. **Rebuild & verify**
   ```bash
   dotnet build && dotnet test
   ```

---

## 8. **Quick Reference: Common Error Codes**

| Code | Meaning | Example |
|------|---------|---------|
| CS0029 | Cannot implicitly convert | Return wrong type |
| CS0572 | Can't reference type through expression | `result.Success` instead of `Result<T>.Success` |
| CS1061 | Type doesn't have member | Missing method on interface |
| CS1513 | Missing `}` | Unclosed brace |
| CA1819 | Return ReadOnlyCollection | Return `IList<T>` instead of `T[]` |

---

## 9. **Next Steps: Fix Test Files**

The 40+ compilation errors are in:
- `AuthServiceTests.cs` — Tests for AuthService
- Potentially in integration tests calling the old controller signatures

**Priority**: Update test expectations to match `Result<T>` pattern used in AuthService.

---

**Summary**: .NET uses `dotnet build` (like TypeScript), Roslyn analyzers (like ESLint), and tests (like Jest) to catch errors. We're currently seeing test failures because the service signatures changed from throwing exceptions to returning `Result<T>`.

To fully compile, we need to update all tests that call the migrated services.
