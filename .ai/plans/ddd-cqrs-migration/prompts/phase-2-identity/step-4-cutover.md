# Phase 2, Step 4: Identity Cutover

**Prerequisite**: Steps 1–3 are complete, `dotnet build` is clean, and all existing tests pass.

**This is the point of no return.** Do NOT start this step until the new handlers are wired up and all tests pass against them.

---

## Pre-Cutover Verification

Run ALL three test suites and confirm all pass before touching the controllers:

```bash
# 1. Integration tests (InMemory DB — fast)
cd src/backend
dotnet test  # Must be 1020/1020 (or current total) passing

# 2. Characterization tests subset (confirms baseline)
dotnet test ECommerce.Tests/ECommerce.Tests.csproj \
    --filter "FullyQualifiedName~AuthCharacterizationTests|FullyQualifiedName~ProfileCharacterizationTests"

# 3. E2E tests (real PostgreSQL — backend must be running)
cd src/frontend/storefront
npx playwright test api-auth.spec.ts --reporter=list
```

All three must be green. If any fail here, fix the tests (wrong baseline assumption) — do NOT proceed with migration until they pass.

---

## Task: Update Controllers and Delete Old Services

### 0. Request body shape — keep existing DTOs

The existing controllers accept `RegisterDto`, `LoginDto`, etc. **Keep these DTOs.** Map from DTO → command inside the controller action. Do not change the API surface (the frontend depends on it).

```csharp
// Existing: controller accepts RegisterDto
public async Task<IActionResult> Register([FromBody] RegisterDto dto, ...)
{
    // Map DTO → command, then dispatch
    var result = await _mediator.Send(
        new RegisterCommand(dto.FirstName, dto.LastName, dto.Email, dto.Password), ct);
    ...
}
```

> Phase 1 (Catalog) let the controller accept the command directly because we controlled both sides. For Identity the existing DTOs are already public API — keep them to avoid breaking the frontend.

### 0b. Error code → HTTP status mapping

Use this table for ALL Identity result → response mappings:

| Error code | HTTP status | Reason |
|---|---|---|
| `VALIDATION_FAILED` | 400 Bad Request | FluentValidation failed |
| `EMAIL_EMPTY` / `EMAIL_INVALID` / `PASSWORD_*` / `NAME_*` | 422 Unprocessable | Domain validation (aggregate invariant) |
| `INVALID_CREDENTIALS` | 401 Unauthorized | Login/password mismatch |
| `TOKEN_INVALID` / `TOKEN_REVOKED` | 401 Unauthorized | Refresh token bad |
| `EMAIL_TAKEN` | 409 Conflict | Duplicate registration |
| `EMAIL_ALREADY_VERIFIED` | 422 Unprocessable | Already done |
| `EMAIL_TOKEN_INVALID` | 422 Unprocessable | Wrong verification token |
| `USER_NOT_FOUND` | 404 Not Found | User looked up by ID doesn't exist |
| `ADDRESS_NOT_FOUND` | 404 Not Found | Address ID not found |
| `ADDRESS_LIMIT` | 422 Unprocessable | Business rule violation |

In code:
```csharp
private IActionResult MapIdentityResult(DomainError error) => error.Code switch
{
    "INVALID_CREDENTIALS" or "TOKEN_INVALID" or "TOKEN_REVOKED"
        => Unauthorized(ApiResponse.Fail(error.Message)),

    "EMAIL_TAKEN"
        => Conflict(ApiResponse.Fail(error.Message)),

    "USER_NOT_FOUND" or "ADDRESS_NOT_FOUND"
        => NotFound(ApiResponse.Fail(error.Message)),

    "VALIDATION_FAILED"
        => BadRequest(ApiResponse.Fail(error.Message)),

    _ => UnprocessableEntity(ApiResponse.Fail(error.Message))
};
```

### 1. Update AuthController

Replace `IAuthService` injection with `IMediator`. Wire each endpoint to its command/query.

Open `src/backend/ECommerce.API/Controllers/AuthController.cs`.

**Change the constructor injection:**
```csharp
// BEFORE:
private readonly IAuthService _authService;
public AuthController(IAuthService authService, ...) { _authService = authService; }

// AFTER:
private readonly IMediator _mediator;
public AuthController(IMediator mediator, ...) { _mediator = mediator; }
```

**Wire each endpoint** (map DTO → command, dispatch, then map result → response):

```csharp
// POST /api/auth/register
var result = await _mediator.Send(
    new RegisterCommand(dto.FirstName, dto.LastName, dto.Email, dto.Password), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
// Set HttpOnly cookie for refresh token (copy cookie-setting code from old AuthController verbatim)
SetRefreshTokenCookie(result.GetDataOrThrow().RefreshToken);
return Ok(ApiResponse.Success(MapToUserDto(result.GetDataOrThrow())));

// POST /api/auth/login
var result = await _mediator.Send(new LoginCommand(dto.Email, dto.Password), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
SetRefreshTokenCookie(result.GetDataOrThrow().RefreshToken);
return Ok(ApiResponse.Success(MapToUserDto(result.GetDataOrThrow())));

// POST /api/auth/logout  ← NEW
var userId = GetUserIdFromClaims();
var refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
var result = await _mediator.Send(new LogoutCommand(userId, refreshToken), ct);
Response.Cookies.Delete("refreshToken");
return Ok(ApiResponse.Success("Logged out"));

// POST /api/auth/refresh-token  — token from HttpOnly cookie
var tokenFromCookie = Request.Cookies["refreshToken"];
if (string.IsNullOrEmpty(tokenFromCookie)) return Unauthorized(ApiResponse.Fail("No refresh token"));
var result = await _mediator.Send(new RefreshTokenCommand(tokenFromCookie), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
SetRefreshTokenCookie(result.GetDataOrThrow().RefreshToken);
return Ok(ApiResponse.Success(MapToUserDto(result.GetDataOrThrow())));

// GET /api/auth/me
var userId = GetUserIdFromClaims();
var result = await _mediator.Send(new GetCurrentUserQuery(userId), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success(result.GetDataOrThrow()));

// POST /api/auth/verify-email
var result = await _mediator.Send(new VerifyEmailCommand(request.UserId, request.Token), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success("Email verified"));

// POST /api/auth/forgot-password
var result = await _mediator.Send(new ForgotPasswordCommand(request.Email), ct);
return Ok(ApiResponse.Success("If that email is registered, a reset link has been sent"));

// POST /api/auth/reset-password
var result = await _mediator.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success("Password reset successful"));
```

### 2. Update ProfileController

Open `src/backend/ECommerce.API/Controllers/ProfileController.cs`.

Replace `IUserService` injection with `IMediator`. Wire each endpoint:

```csharp
// GET /api/profile
var userId = GetUserIdFromClaims();
var result = await _mediator.Send(new GetCurrentUserQuery(userId), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success(result.GetDataOrThrow()));

// PUT /api/profile
var result = await _mediator.Send(
    new UpdateProfileCommand(userId, dto.FirstName, dto.LastName, dto.PhoneNumber), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success(result.GetDataOrThrow()));

// POST /api/profile/change-password
var result = await _mediator.Send(
    new ChangePasswordCommand(userId, dto.OldPassword, dto.NewPassword), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success("Password changed"));

// POST /api/profile/addresses
var result = await _mediator.Send(
    new AddAddressCommand(userId, dto.Street, dto.City, dto.Country, dto.PostalCode), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success(result.GetDataOrThrow()));

// PUT /api/profile/addresses/{id}/default
var result = await _mediator.Send(new SetDefaultAddressCommand(userId, addressId), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success(result.GetDataOrThrow()));

// DELETE /api/profile  (delete account)
var result = await _mediator.Send(new DeleteAccountCommand(userId), ct);
if (!result.IsSuccess) return MapIdentityResult(result.GetErrorOrThrow());
return Ok(ApiResponse.Success("Account deleted"));
```

**Preferences endpoints** (`GET /api/profile/preferences`, `PUT /api/profile/preferences`):
These are not part of the Identity domain model (no `UserPreferences` in the `User` aggregate). Keep these wired to `IUserService` for now — migrate them in a future pass when preferences get their own aggregate, or leave them as-is if they're stored in a separate table the domain doesn't own.

### 3. Remove old services

Delete these files:
```bash
rm src/backend/ECommerce.Application/Services/AuthService.cs
rm src/backend/ECommerce.Application/Interfaces/IAuthService.cs
rm src/backend/ECommerce.Application/Services/UserService.cs
rm src/backend/ECommerce.Application/Interfaces/IUserService.cs
```

### 4. Remove DI registrations

In `src/backend/ECommerce.API/Extensions/ServiceCollectionExtensions.cs`, remove:
```csharp
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IUserService, UserService>();
```

In `src/backend/ECommerce.API/Program.cs`, remove from the DI validation block:
```csharp
_ = scope.ServiceProvider.GetRequiredService<IAuthService>();
```

### 5. Build and test

```bash
cd src/backend
dotnet build   # Must be clean — no IAuthService/IUserService references remaining

# Confirm zero references to old services:
grep -r "IAuthService\|IUserService\|AuthService\|UserService" src/backend \
    --include="*.cs" | grep -v ".git"
# Expected: zero results (only the test files if they have hard-coded class names)

dotnet test    # All tests must pass

# Re-run e2e tests against real PostgreSQL (backend must be running)
cd src/frontend/storefront
npx playwright test api-auth.spec.ts --reporter=list
```

---

## Acceptance Criteria

- [ ] `AuthController` injects `IMediator` and dispatches all 6 auth commands
- [ ] `ProfileController` injects `IMediator` and dispatches all 4 profile commands
- [ ] `AuthService.cs` deleted
- [ ] `IAuthService.cs` deleted
- [ ] `UserService.cs` deleted
- [ ] `IUserService.cs` deleted
- [ ] No remaining references to old services in the codebase (`grep` confirms zero)
- [ ] `dotnet build` is clean
- [ ] All characterization tests pass (`AuthCharacterizationTests` + `ProfileCharacterizationTests`)
- [ ] All integration tests pass (`dotnet test`)
- [ ] All e2e tests pass (`api-auth.spec.ts`) — same results as the step-0b baseline
