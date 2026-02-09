# Implementation Plan - E-Commerce Platform Remediation

**Created:** February 6, 2026
**Based On:** CODE_REVIEW.md (Comprehensive Code Review Report)
**Purpose:** Step-by-step implementation guide for fixing all identified issues
**Audience:** Developers and AI coding assistants implementing fixes

---

## How To Use This Document

This plan is organized into **6 Phases**, executed sequentially. Each phase contains **Tasks** broken into atomic, implementable steps. Every task includes:

- **What to do** - Clear description
- **Where to do it** - Exact file paths and line numbers
- **How to do it** - Code examples and implementation details
- **How to verify** - Testing/validation steps
- **Dependencies** - What must be done first

> **Important for AI models:** Each task is self-contained. Read the full task description before starting. Do NOT skip verification steps. If a task references another task, check its completion status first.

---

## Phase Overview

| Phase | Focus | Priority | Tasks | Status | Estimated Effort |
|-------|-------|----------|-------|--------|-----------------|
| **Phase 1** | Critical Security Fixes | P0 | 8 tasks | ⏳ 50% (4/8) | 1-2 days |
| **Phase 2** | Authentication & Access Control | P0-P1 | 6 tasks | ✅ **Complete** | 2-3 days |
| **Phase 3** | Backend Code Quality | P1-P2 | 8 tasks | ✅ **Complete** | 3-4 days |
| **Phase 4** | Frontend Code Quality | P1-P2 | 10 tasks | ✅ **Complete** | 3-4 days |
| **Phase 5** | Infrastructure & DevOps | P2 | 5 tasks | ✅ **Complete** | 2-3 days |
| **Phase 6** | Testing & Documentation | P2-P3 | 5 tasks | ✅ 80% (4/5) | 2-3 days |

---

## Phase 1: Critical Security Fixes (P0) ⚠️ 50% Complete

> **Goal:** Eliminate all critical security vulnerabilities that make the application unsafe for any deployment.
> **Must complete before:** Any other phase.
> **Status:** 4 of 8 tasks complete. Credentials still exposed, need rotation and git cleanup.

---

### Task 1.1: Revoke and Rotate Exposed Credentials

**Priority:** P0 - IMMEDIATE
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 2, Issue #1

#### What to do
Revoke all exposed credentials found in `appsettings.Development.json`.

#### Steps

1. **Revoke SendGrid API Key:**
   - Log into SendGrid dashboard (https://app.sendgrid.com)
   - Navigate to Settings > API Keys
   - Find and revoke key starting with `SG.CDEhOOFCSkWJ5eA9n-lkdQ`
   - Generate a new API key
   - Store new key securely (NOT in source code)

2. **Change Gmail App Password:**
   - Log into Google Account > Security > App Passwords
   - Revoke the app password `qurd bqaj inyl ctfo`
   - Generate a new app password
   - Store new password securely (NOT in source code)

3. **Change the JWT Secret Key:**
   - Generate a new 256-bit key: use a cryptographic random generator
   - Store securely via environment variables or User Secrets

#### Verification
- Old SendGrid key returns 401 when used
- Old Gmail password fails authentication
- Application still works with new credentials

---

### Task 1.2: Remove Credentials from Git History

**Priority:** P0 - IMMEDIATE
**Depends on:** Task 1.1 (credentials already revoked)
**Reference:** CODE_REVIEW.md Section 2, Issue #1

#### What to do
Purge all sensitive data from the entire git history.

#### Steps

1. **Install BFG Repo-Cleaner** (faster than git filter-branch):
   ```bash
   # Download BFG (requires Java)
   # Or use git filter-repo (Python-based alternative)
   pip install git-filter-repo
   ```

2. **Create a backup first:**
   ```bash
   git clone --mirror <repo-url> backup-repo.git
   ```

3. **Remove the sensitive file from history:**
   ```bash
   # Option A: Using git filter-repo
   git filter-repo --path src/backend/ECommerce.API/appsettings.Development.json --invert-paths

   # Option B: Using BFG
   bfg --delete-files appsettings.Development.json
   git reflog expire --expire=now --all
   git gc --prune=now --aggressive
   ```

4. **Force push the cleaned history:**
   ```bash
   git push origin --force --all
   git push origin --force --tags
   ```

#### Verification
- Run: `git log --all --full-history -- src/backend/ECommerce.API/appsettings.Development.json`
- Should return empty (no history for that file)

---

### Task 1.3: Implement Secure Secrets Management

**Priority:** P0
**Depends on:** Task 1.1
**Reference:** CODE_REVIEW.md Section 2, Issue #1; Section 15.2, A02

#### What to do
Replace hardcoded secrets with environment variables and .NET User Secrets for development.

#### Steps

1. **Set up .NET User Secrets for development:**
   ```bash
   cd src/backend/ECommerce.API
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:SecretKey" "<your-new-256bit-key>"
   dotnet user-secrets set "SendGrid:ApiKey" "<your-new-sendgrid-key>"
   dotnet user-secrets set "EmailSettings:SmtpPassword" "<your-new-password>"
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=ECommerceDb;Username=ecommerce;Password=<your-password>"
   ```

2. **Update `appsettings.json`** to remove real secrets:
   - **File:** `src/backend/ECommerce.API/appsettings.json`
   - Replace all secret values with placeholder text:
   ```json
   {
     "Jwt": {
       "SecretKey": "REPLACE_WITH_ENV_VAR_OR_USER_SECRET",
       "Issuer": "ecommerce-api",
       "Audience": "ecommerce-client",
       "ExpireMinutes": 60
     },
     "ConnectionStrings": {
       "DefaultConnection": "REPLACE_WITH_ENV_VAR_OR_USER_SECRET"
     },
     "SendGrid": {
       "ApiKey": "REPLACE_WITH_ENV_VAR_OR_USER_SECRET"
     },
     "EmailSettings": {
       "SmtpPassword": "REPLACE_WITH_ENV_VAR_OR_USER_SECRET"
     }
   }
   ```

3. **Create `appsettings.Development.json`** (safe version - secrets come from User Secrets):
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AppUrl": "http://localhost:5173"
   }
   ```

4. **Update `.gitignore`** to prevent future secret leaks:
   - **File:** `.gitignore` (root of repository)
   - Add these lines if not present:
   ```
   # Secrets
   appsettings.Development.json
   appsettings.*.local.json
   *.pfx
   *.p12

   # Environment files
   .env
   .env.*
   !.env.example
   ```

5. **Update `docker-compose.yml`** to pass secrets via environment:
   - **File:** `docker-compose.yml`
   - For the API service, use environment variables:
   ```yaml
   api:
     environment:
       - ConnectionStrings__DefaultConnection=Host=postgres;Database=ECommerceDb;Username=ecommerce;Password=${DB_PASSWORD}
       - Jwt__SecretKey=${JWT_SECRET}
       - SendGrid__ApiKey=${SENDGRID_API_KEY}
   ```

6. **Create `.env.example`** at repository root:
   ```
   # Database
   DB_PASSWORD=your-db-password

   # JWT
   JWT_SECRET=your-jwt-secret-min-32-chars

   # SendGrid
   SENDGRID_API_KEY=your-sendgrid-api-key

   # SMTP
   SMTP_PASSWORD=your-smtp-password
   ```

#### Verification
- Application starts successfully using User Secrets in development
- No secrets visible in any committed file
- `git grep -i "password\|secret\|apikey"` returns no real credentials
- Docker compose works with `.env` file

---

### Task 1.4: Fix .env File Tracking in Frontend

**Priority:** P0
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 2, Issue #2

#### What to do
Remove `.env` from git tracking and create a template.

#### Steps

1. **Remove `.env` from git tracking** (keeps the local file):
   ```bash
   git rm --cached src/frontend/storefront/.env
   ```

2. **Create `.env.example`:**
   - **File:** `src/frontend/storefront/.env.example`
   ```
   # API Configuration
   VITE_API_URL=http://localhost:5000/api

   # Application
   VITE_APP_NAME=E-Commerce Store
   ```

3. **Update `.gitignore`** (should already be done in Task 1.3 step 4)

4. **Check admin app** for same issue:
   ```bash
   # If src/frontend/admin/.env exists and is tracked:
   git rm --cached src/frontend/admin/.env
   ```
   Create corresponding `.env.example` for admin app.

#### Verification
- `git status` shows `.env` as deleted (from tracking, not from disk)
- `.env.example` exists and is committed
- `.env` is in `.gitignore`

---

### Task 1.5: Add Security Headers Middleware

**Priority:** P0
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 15.2, A05 - Security Misconfiguration

#### What to do
Create middleware that adds security headers to all HTTP responses.

#### Steps

1. **Create SecurityHeadersMiddleware:**
   - **File:** `src/backend/ECommerce.API/Middleware/SecurityHeadersMiddleware.cs` (NEW FILE)
   ```csharp
   namespace ECommerce.API.Middleware;

   public class SecurityHeadersMiddleware
   {
       private readonly RequestDelegate _next;

       public SecurityHeadersMiddleware(RequestDelegate next)
       {
           _next = next;
       }

       public async Task InvokeAsync(HttpContext context)
       {
           // Prevent clickjacking
           context.Response.Headers.Append("X-Frame-Options", "DENY");

           // Prevent MIME-type sniffing
           context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

           // XSS protection
           context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

           // Referrer policy
           context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

           // Permissions policy
           context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

           // Content Security Policy (adjust based on frontend needs)
           context.Response.Headers.Append("Content-Security-Policy",
               "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' https:;");

           // HSTS (only in production, after HTTPS is confirmed working)
           if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
           {
               context.Response.Headers.Append("Strict-Transport-Security",
                   "max-age=31536000; includeSubDomains");
           }

           await _next(context);
       }
   }
   ```

2. **Register the middleware in Program.cs:**
   - **File:** `src/backend/ECommerce.API/Program.cs`
   - Add **before** `app.UseRouting()` or `app.UseCors()`:
   ```csharp
   app.UseMiddleware<SecurityHeadersMiddleware>();
   ```
   - The middleware pipeline order should be:
     1. SecurityHeadersMiddleware
     2. GlobalExceptionMiddleware
     3. CORS
     4. Authentication
     5. Authorization
     6. Controllers

#### Verification
- Start the API and make a request: `curl -I http://localhost:5000/api/products`
- Response headers should include all security headers
- Run a security scanner (e.g., https://securityheaders.com) against the API

---

### Task 1.6: Implement Rate Limiting

**Priority:** P0
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 15.2, A04; Section 15.3

#### What to do
Add rate limiting to protect against brute force and API abuse.

#### Steps

1. **Install the NuGet package:**
   ```bash
   cd src/backend/ECommerce.API
   dotnet add package System.Threading.RateLimiting
   ```
   Note: ASP.NET Core 10 has built-in rate limiting. No additional package needed if using .NET 8+.

2. **Configure rate limiting in Program.cs:**
   - **File:** `src/backend/ECommerce.API/Program.cs`
   - Add to service registration section (before `builder.Build()`):
   ```csharp
   using System.Threading.RateLimiting;

   builder.Services.AddRateLimiter(options =>
   {
       // Global rate limit: 100 requests per minute per IP
       options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
           RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
               factory: _ => new FixedWindowRateLimiterOptions
               {
                   PermitLimit = 100,
                   Window = TimeSpan.FromMinutes(1),
                   QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                   QueueLimit = 0
               }));

       // Strict limiter for auth endpoints
       options.AddPolicy("AuthLimit", context =>
           RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
               factory: _ => new FixedWindowRateLimiterOptions
               {
                   PermitLimit = 5,
                   Window = TimeSpan.FromMinutes(1),
                   QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                   QueueLimit = 0
               }));

       // Strict limiter for password reset
       options.AddPolicy("PasswordResetLimit", context =>
           RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
               factory: _ => new FixedWindowRateLimiterOptions
               {
                   PermitLimit = 3,
                   Window = TimeSpan.FromMinutes(15),
                   QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                   QueueLimit = 0
               }));

       options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
       options.OnRejected = async (context, cancellationToken) =>
       {
           context.HttpContext.Response.ContentType = "application/json";
           var response = new { message = "Too many requests. Please try again later." };
           await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
       };
   });
   ```

3. **Add middleware to pipeline in Program.cs:**
   - Add **after** `app.UseRouting()` and **before** `app.UseAuthentication()`:
   ```csharp
   app.UseRateLimiter();
   ```

4. **Apply rate limiting policies to controllers:**
   - **File:** `src/backend/ECommerce.API/Controllers/AuthController.cs`
   - Add attribute to login endpoint:
   ```csharp
   [HttpPost("login")]
   [EnableRateLimiting("AuthLimit")]
   public async Task<IActionResult> Login(...)
   ```
   - Add attribute to register endpoint:
   ```csharp
   [HttpPost("register")]
   [EnableRateLimiting("AuthLimit")]
   public async Task<IActionResult> Register(...)
   ```
   - Add attribute to forgot-password endpoint:
   ```csharp
   [HttpPost("forgot-password")]
   [EnableRateLimiting("PasswordResetLimit")]
   public async Task<IActionResult> ForgotPassword(...)
   ```

5. **Add the required using statement** to controllers:
   ```csharp
   using Microsoft.AspNetCore.RateLimiting;
   ```

#### Verification
- Start the API
- Send 6 login requests rapidly within 1 minute to `/api/auth/login`
- The 6th request should return HTTP 429 Too Many Requests
- Normal API requests (under 100/min) should work fine

---

### Task 1.7: Implement Webhook Signature Verification

**Priority:** P0
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 15.2, A08 (CVSS 9.1)

#### What to do
Add HMAC-SHA256 signature verification to the payment webhook endpoint.

#### Steps

1. **Add webhook secret to configuration:**
   - **File:** `src/backend/ECommerce.API/appsettings.json`
   ```json
   {
     "PaymentWebhook": {
       "Secret": "REPLACE_WITH_ENV_VAR_OR_USER_SECRET"
     }
   }
   ```
   - Store actual secret via User Secrets or environment variable

2. **Create a webhook verification service:**
   - **File:** `src/backend/ECommerce.Application/Interfaces/IWebhookVerificationService.cs` (NEW FILE)
   ```csharp
   namespace ECommerce.Application.Interfaces;

   public interface IWebhookVerificationService
   {
       bool VerifySignature(string payload, string signature);
   }
   ```

   - **File:** `src/backend/ECommerce.Application/Services/WebhookVerificationService.cs` (NEW FILE)
   ```csharp
   using System.Security.Cryptography;
   using System.Text;
   using ECommerce.Application.Interfaces;
   using Microsoft.Extensions.Configuration;

   namespace ECommerce.Application.Services;

   public class WebhookVerificationService : IWebhookVerificationService
   {
       private readonly string _secret;

       public WebhookVerificationService(IConfiguration configuration)
       {
           _secret = configuration["PaymentWebhook:Secret"]
               ?? throw new InvalidOperationException("Webhook secret not configured");
       }

       public bool VerifySignature(string payload, string signature)
       {
           if (string.IsNullOrEmpty(signature))
               return false;

           using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
           var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
           var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

           return CryptographicOperations.FixedTimeEquals(
               Encoding.UTF8.GetBytes(computedSignature),
               Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
       }
   }
   ```

3. **Register the service in DI:**
   - **File:** `src/backend/ECommerce.API/Program.cs` (or appropriate extension method file)
   ```csharp
   builder.Services.AddScoped<IWebhookVerificationService, WebhookVerificationService>();
   ```

4. **Update PaymentsController webhook endpoint:**
   - **File:** `src/backend/ECommerce.API/Controllers/PaymentsController.cs`
   - Modify the ProcessPaymentWebhook method (around line 225):
   ```csharp
   [HttpPost("webhook")]
   [AllowAnonymous]
   public async Task<IActionResult> ProcessPaymentWebhook(CancellationToken cancellationToken)
   {
       // Read raw body for signature verification
       using var reader = new StreamReader(Request.Body);
       var rawBody = await reader.ReadToEndAsync(cancellationToken);

       // Verify signature
       var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
       if (!_webhookVerificationService.VerifySignature(rawBody, signature ?? string.Empty))
       {
           _logger.LogWarning("Webhook signature verification failed from IP {IP}",
               HttpContext.Connection.RemoteIpAddress);
           return Unauthorized(ApiResponse<object>.Error("Invalid webhook signature"));
       }

       // Deserialize after verification
       var webhookPayload = JsonSerializer.Deserialize<PaymentWebhookDto>(rawBody);
       if (webhookPayload == null)
           return BadRequest(ApiResponse<object>.Error("Invalid webhook payload"));

       // Process the verified webhook...
       var result = await _paymentService.ProcessWebhookAsync(webhookPayload, cancellationToken);
       return Ok(ApiResponse<object>.Ok(result, "Webhook processed successfully"));
   }
   ```

5. **Inject the service into PaymentsController constructor:**
   ```csharp
   private readonly IWebhookVerificationService _webhookVerificationService;

   public PaymentsController(
       IPaymentService paymentService,
       IWebhookVerificationService webhookVerificationService,
       ILogger<PaymentsController> logger)
   {
       _paymentService = paymentService;
       _webhookVerificationService = webhookVerificationService;
       _logger = logger;
   }
   ```

#### Verification
- Send a webhook request without signature header -> should get 401
- Send a webhook request with wrong signature -> should get 401
- Send a webhook request with valid HMAC-SHA256 signature -> should get 200
- Check logs for warning messages on failed verifications

---

### Task 1.8: Fix Information Disclosure in Error Responses

**Priority:** P0
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 13.3, Issue #1; Section 15.2, A05

#### What to do
Stop leaking exception messages to API clients. Log details internally, return generic messages externally.

#### Steps

1. **Update GlobalExceptionMiddleware:**
   - **File:** `src/backend/ECommerce.API/Middleware/GlobalExceptionMiddleware.cs`
   - Find the catch-all case (around line 85-89) that returns `exception.Message`
   - Replace the generic exception handler:

   **Current code (approximately):**
   ```csharp
   _ => (StatusCodes.Status500InternalServerError,
       ApiResponse<object>.Error(
           "An internal server error occurred. Please try again later.",
           new List<string> { exception.Message }))
   ```

   **Replace with:**
   ```csharp
   _ => (StatusCodes.Status500InternalServerError,
       ApiResponse<object>.Error(
           "An internal server error occurred. Please try again later.",
           new List<string>()))
   ```

2. **Add additional exception type mappings** in the same middleware:
   ```csharp
   ArgumentNullException _ => (StatusCodes.Status400BadRequest,
       ApiResponse<object>.Error("A required parameter was missing.")),

   ArgumentException argEx => (StatusCodes.Status400BadRequest,
       ApiResponse<object>.Error(argEx.Message)),

   InvalidOperationException _ => (StatusCodes.Status409Conflict,
       ApiResponse<object>.Error("The requested operation could not be completed due to a conflict.")),
   ```

3. **Ensure the exception is still logged internally** (should already exist):
   ```csharp
   _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
   ```

#### Verification
- Trigger a 500 error (e.g., by temporarily breaking a database query)
- API response should NOT contain exception details
- Log files should still contain the full exception stack trace

---

## Phase 2: Authentication & Access Control (P0-P1) ✅ COMPLETE

> **Goal:** Fix all authentication, authorization, and access control vulnerabilities.
> **Depends on:** Phase 1 completed.
> **Status:** All 6 tasks completed. Token validation, refresh tokens, IDOR protection, CORS, and security logging all in place.

---

### Task 2.1: Fix Token Validation (Enable Issuer/Audience Checks)

**Priority:** P0
**Depends on:** Task 1.3 (secrets management)
**Reference:** CODE_REVIEW.md Section 15.1, Vulnerability #1 (CVSS 8.1)

#### What to do
Enable JWT issuer and audience validation.

#### Steps

1. **Update AuthService.cs ValidateTokenAsync:**
   - **File:** `src/backend/ECommerce.Application/Services/AuthService.cs`
   - Find ValidateTokenAsync method (around line 119-141)
   - Change `ValidateIssuer` and `ValidateAudience` to `true`:
   ```csharp
   var tokenValidationParameters = new TokenValidationParameters
   {
       ValidateIssuerSigningKey = true,
       IssuerSigningKey = key,
       ValidateIssuer = true,                          // Changed from false
       ValidIssuer = _configuration["Jwt:Issuer"],     // Add this
       ValidateAudience = true,                         // Changed from false
       ValidAudience = _configuration["Jwt:Audience"],  // Add this
       ClockSkew = TimeSpan.Zero
   };
   ```

2. **Verify that Program.cs JWT configuration** also validates issuer/audience:
   - **File:** `src/backend/ECommerce.API/Program.cs`
   - In the `AddAuthentication` / `AddJwtBearer` configuration, ensure:
   ```csharp
   options.TokenValidationParameters = new TokenValidationParameters
   {
       ValidateIssuerSigningKey = true,
       IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
       ValidateIssuer = true,
       ValidIssuer = configuration["Jwt:Issuer"],
       ValidateAudience = true,
       ValidAudience = configuration["Jwt:Audience"],
       ValidateLifetime = true,
       ClockSkew = TimeSpan.Zero
   };
   ```

#### Verification
- Login and get a token -> should work normally
- Manually modify the token's issuer claim -> should fail validation
- Tokens from before the fix will be invalidated (expected - users need to re-login)

---

### Task 2.2: Implement Proper Token Refresh

**Priority:** P1
**Depends on:** Task 2.1
**Reference:** CODE_REVIEW.md Section 13.2, Issue #7; Section 15.1, Vulnerability #2

#### What to do
Implement actual token refresh that issues a new token instead of returning the same one.

#### Steps

1. **Add RefreshToken entity to Core layer:**
   - **File:** `src/backend/ECommerce.Core/Entities/RefreshToken.cs` (NEW FILE)
   ```csharp
   namespace ECommerce.Core.Entities;

   public class RefreshToken
   {
       public Guid Id { get; set; }
       public Guid UserId { get; set; }
       public string Token { get; set; } = null!;
       public DateTime ExpiresAt { get; set; }
       public DateTime CreatedAt { get; set; }
       public bool IsRevoked { get; set; }
       public User User { get; set; } = null!;
   }
   ```

2. **Add DbSet to AppDbContext:**
   - **File:** `src/backend/ECommerce.Infrastructure/Data/AppDbContext.cs`
   ```csharp
   public DbSet<RefreshToken> RefreshTokens { get; set; }
   ```

3. **Update AuthService.RefreshTokenAsync:**
   - **File:** `src/backend/ECommerce.Application/Services/AuthService.cs`
   - Replace the RefreshTokenAsync method (around line 103-117):
   ```csharp
   public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
   {
       var storedToken = await _unitOfWork.RefreshTokens
           .FindByCondition(rt => rt.Token == refreshToken && !rt.IsRevoked)
           .FirstOrDefaultAsync(cancellationToken);

       if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
           throw new InvalidTokenException("Invalid or expired refresh token");

       // Revoke old refresh token (rotation)
       storedToken.IsRevoked = true;

       // Get user and generate new tokens
       var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId, cancellationToken)
           ?? throw new NotFoundException("User not found");

       var newAccessToken = GenerateJwtToken(user);
       var newRefreshToken = GenerateRefreshToken();

       // Save new refresh token
       await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
       {
           UserId = user.Id,
           Token = newRefreshToken,
           ExpiresAt = DateTime.UtcNow.AddDays(7),
           CreatedAt = DateTime.UtcNow
       }, cancellationToken);

       await _unitOfWork.SaveChangesAsync(cancellationToken);

       return new AuthResponseDto
       {
           Success = true,
           Message = "Token refreshed",
           Token = newAccessToken,
           RefreshToken = newRefreshToken
       };
   }

   private static string GenerateRefreshToken()
   {
       var randomBytes = new byte[64];
       using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
       rng.GetBytes(randomBytes);
       return Convert.ToBase64String(randomBytes);
   }
   ```

4. **Update AuthResponseDto** to include refresh token:
   - **File:** `src/backend/ECommerce.Application/DTOs/Auth/AuthDtos.cs`
   ```csharp
   public class AuthResponseDto
   {
       public bool Success { get; set; }
       public string Message { get; set; } = null!;
       public string? Token { get; set; }
       public string? RefreshToken { get; set; }  // Add this
   }
   ```

5. **Update Login to also return refresh token:**
   - In AuthService.LoginAsync, after generating the JWT token, also generate and save a refresh token

6. **Create a new EF migration:**
   ```bash
   cd src/backend/ECommerce.Infrastructure
   dotnet ef migrations add AddRefreshTokens --startup-project ../ECommerce.API
   ```

#### Verification
- Login returns both access token and refresh token
- Calling refresh endpoint with valid refresh token returns NEW tokens
- Old refresh token is revoked (can't be reused)
- Expired refresh tokens are rejected

---

### Task 2.3: Fix IDOR in Orders Endpoint

**Priority:** P0
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 15.1, Vulnerability #3 (CVSS 7.5)

#### What to do
Add ownership validation so users can only access their own orders.

#### Steps

1. **Update OrdersController.GetOrderById:**
   - **File:** `src/backend/ECommerce.API/Controllers/OrdersController.cs`
   - Find GetOrderById method (around line 69-78)
   - Add ownership check after retrieving the order:
   ```csharp
   [HttpGet("{id:guid}")]
   [Authorize]
   public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
   {
       var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);

       if (order == null)
           return NotFound(ApiResponse<OrderDetailDto>.Error("Order not found"));

       // Ownership check: only order owner or admin can view
       var currentUserId = _currentUser.UserIdOrNull;
       var isAdmin = _currentUser.Role == "Admin" || _currentUser.Role == "SuperAdmin";

       if (!isAdmin && order.UserId != currentUserId)
           return Forbid();

       return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order retrieved successfully"));
   }
   ```

2. **Ensure OrderDetailDto has UserId:**
   - Verify that `OrderDetailDto` includes a `UserId` property (or the user's identifier)
   - If not, add it to the DTO and the AutoMapper mapping

3. **Apply same pattern to GetUserOrders:**
   - Ensure the endpoint that lists orders already filters by current user
   - Verify at the service layer that the userId parameter matches the authenticated user

#### Verification
- Login as User A, create an order, note the order ID
- Login as User B, try to GET `/api/orders/{userA-order-id}` -> should get 403 Forbidden
- Login as Admin, try the same -> should get 200 OK
- Login as User A, try the same -> should get 200 OK

---

### Task 2.4: Fix IDOR in Reviews Endpoint

**Priority:** P0
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 15.1, Vulnerability #4

#### What to do
Add ownership validation for review modification operations.

#### Steps

1. **Update ReviewsController** for edit/delete operations:
   - **File:** `src/backend/ECommerce.API/Controllers/ReviewsController.cs`
   - For update and delete endpoints, add ownership check:
   ```csharp
   // Before allowing update/delete of a review:
   var review = await _reviewService.GetReviewByIdAsync(id, cancellationToken);
   if (review == null)
       return NotFound();

   var currentUserId = _currentUser.UserIdOrNull;
   var isAdmin = _currentUser.Role == "Admin" || _currentUser.Role == "SuperAdmin";

   if (!isAdmin && review.UserId != currentUserId)
       return Forbid();
   ```

2. **Review read access:**
   - GET operations for reviews are typically public (reading product reviews)
   - This is acceptable - focus ownership checks on mutation operations (PUT, DELETE)

#### Verification
- Create a review as User A
- Try to update/delete it as User B -> should get 403
- Try to update/delete it as User A -> should work
- Admin should be able to delete any review

---

### Task 2.5: Fix CORS Configuration for Production

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 15.2, A05

#### What to do
Restrict CORS methods and headers in production configuration.

#### Steps

1. **Update Program.cs CORS configuration:**
   - **File:** `src/backend/ECommerce.API/Program.cs`
   - Find the CORS policy configuration (around lines 79-103)
   - Update production CORS policy:
   ```csharp
   if (isDevelopment)
   {
       options.AddPolicy("AllowAll", policy =>
       {
           policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
       });
   }
   else
   {
       var allowedOrigins = configuration.GetSection("Cors:Origins").Get<string[]>()
           ?? Array.Empty<string>();

       if (allowedOrigins.Length == 0)
           throw new InvalidOperationException("CORS origins must be configured for production");

       options.AddPolicy("AllowAll", policy =>
       {
           policy.WithOrigins(allowedOrigins)
               .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
               .WithHeaders("Content-Type", "Authorization", "Accept")
               .AllowCredentials();
       });
   }
   ```

2. **Add production CORS origins to appsettings.json:**
   ```json
   {
     "Cors": {
       "Origins": ["https://yourdomain.com", "https://admin.yourdomain.com"]
     }
   }
   ```

#### Verification
- In development: all origins, methods, headers still allowed
- In production: only configured origins, specific methods and headers allowed
- Test with a request from an unauthorized origin -> should be blocked

---

### Task 2.6: Add Security Event Logging

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 15.2, A09

#### What to do
Add structured security logging for authentication events and sensitive operations.

#### Steps

1. **Add security logging to AuthService:**
   - **File:** `src/backend/ECommerce.Application/Services/AuthService.cs`
   - After successful login:
   ```csharp
   _logger.LogInformation("Successful login for user {Email} (ID: {UserId})", user.Email, user.Id);
   ```
   - After failed login:
   ```csharp
   _logger.LogWarning("Failed login attempt for {Email}: Invalid credentials", dto.Email);
   ```
   - After registration:
   ```csharp
   _logger.LogInformation("New user registered: {Email} (ID: {UserId})", user.Email, user.Id);
   ```
   - After password reset request:
   ```csharp
   _logger.LogInformation("Password reset requested for {Email}", email);
   ```

2. **Add security logging to controllers:**
   - Log authorization failures (403 responses)
   - Log admin actions (user management, product changes)

3. **Update Serilog configuration** for security log enrichment:
   - **File:** `src/backend/ECommerce.API/Program.cs`
   ```csharp
   Log.Logger = new LoggerConfiguration()
       .MinimumLevel.Information()
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .WriteTo.Console()
       .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
       .WriteTo.File("logs/security-.txt",
           rollingInterval: RollingInterval.Day,
           restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
       .CreateLogger();
   ```

#### Verification
- Login successfully -> check logs for success message
- Login with wrong password -> check logs for warning
- Access unauthorized resource -> check security log file
- All security events should include timestamp, user identifier, and IP where available

---

## Phase 3: Backend Code Quality (P1-P2) ✅ COMPLETE

> **Goal:** Fix race conditions, performance issues, and code quality problems in the backend.
> **Depends on:** Phase 1 and Phase 2 critical tasks completed.
> **Status:** All 8 tasks completed

---

### Task 3.1: Fix Race Condition in Order Creation

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 13.2, Issue #1

#### What to do
Wrap order creation in a database transaction with pessimistic locking on inventory.

#### Steps

1. **Update OrderService.CreateOrderAsync:**
   - **File:** `src/backend/ECommerce.Application/Services/OrderService.cs`
   - Wrap the entire order creation flow (lines ~61-266) in a transaction:
   ```csharp
   public async Task<OrderDetailDto> CreateOrderAsync(CreateOrderDto dto, Guid? userId, CancellationToken cancellationToken)
   {
       await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
       try
       {
           // 1. Lock and check stock (within transaction)
           var stockCheckItems = dto.Items.Select(i => new StockCheckItem
           {
               ProductId = i.ProductId,
               RequestedQuantity = i.Quantity
           }).ToList();

           // Use FOR UPDATE to lock inventory rows
           var stockAvailable = await _inventoryService.CheckAndLockStockAsync(stockCheckItems, cancellationToken);
           if (!stockAvailable.AllAvailable)
               throw new InsufficientStockException(stockAvailable.UnavailableItems);

           // 2. Create the order
           var order = MapToOrder(dto, userId);
           await _unitOfWork.Orders.AddAsync(order, cancellationToken);

           // 3. Reduce stock (still within same transaction)
           await _inventoryService.ReduceStockAsync(stockCheckItems, cancellationToken);

           // 4. Save everything atomically
           await _unitOfWork.SaveChangesAsync(cancellationToken);
           await transaction.CommitAsync(cancellationToken);

           // 5. Send email AFTER transaction commits (non-critical)
           _ = Task.Run(() => SendOrderConfirmationEmailAsync(order, dto));

           return MapToOrderDetailDto(order);
       }
       catch
       {
           await transaction.RollbackAsync(cancellationToken);
           throw;
       }
   }
   ```

2. **Add CheckAndLockStockAsync to InventoryService:**
   - This method should use `SELECT ... FOR UPDATE` equivalent in EF Core
   - For PostgreSQL, use raw SQL with row locking:
   ```csharp
   public async Task<StockCheckResult> CheckAndLockStockAsync(
       List<StockCheckItem> items, CancellationToken cancellationToken)
   {
       // Lock the product rows to prevent concurrent modification
       var productIds = items.Select(i => i.ProductId).ToList();
       var products = await _dbContext.Products
           .FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Id\" = ANY({0}) FOR UPDATE", productIds)
           .ToListAsync(cancellationToken);

       // Check availability against locked rows
       // ...
   }
   ```

3. **Ensure UnitOfWork supports transactions:**
   - Verify that `_unitOfWork.BeginTransactionAsync()` exists
   - If not, add it to the IUnitOfWork interface and implementation

#### Verification
- Simulate concurrent orders for the same product (use a load testing tool)
- Stock should never go negative
- Only one of two concurrent orders for the last item should succeed

---

### Task 3.2: Fix N+1 Query Problems

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 13.2, Issue #4

#### What to do
Replace N+1 query patterns with batch/eager loading.

#### Steps

1. **Fix CartService.ValidateCartAsync:**
   - **File:** `src/backend/ECommerce.Application/Services/CartService.cs` (around line 198-209)
   - Replace the foreach loop with a batch query:
   ```csharp
   // Before (N+1):
   // foreach (var item in cart.Items)
   //     var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);

   // After (single query):
   var productIds = cart.Items.Select(i => i.ProductId).ToList();
   var products = await _unitOfWork.Products
       .FindByCondition(p => productIds.Contains(p.Id))
       .ToListAsync(cancellationToken);

   var productMap = products.ToDictionary(p => p.Id);

   foreach (var item in cart.Items)
   {
       if (productMap.TryGetValue(item.ProductId, out var product))
       {
           // Validate item against product
       }
   }
   ```

2. **Fix ProductService search:**
   - **File:** `src/backend/ECommerce.Application/Services/ProductService.cs` (around line 133-138)
   - Push filtering to database:
   ```csharp
   // Before: var allProducts = await _unitOfWork.Products.GetAllAsync();
   // After:
   var searchResults = await _unitOfWork.Products
       .FindByCondition(p => p.IsActive &&
           (p.Name.Contains(query) || p.Description.Contains(query)))
       .ToListAsync(cancellationToken);
   ```

3. **Fix InventoryService admin alert:**
   - **File:** `src/backend/ECommerce.Application/Services/InventoryService.cs` (around line 299-314)
   - Use targeted query instead of GetAllAsync:
   ```csharp
   // Before: var admins = (await _unitOfWork.Users.GetAllAsync()).Where(u => u.Role == ...)
   // After:
   var admins = await _unitOfWork.Users
       .FindByCondition(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
       .ToListAsync(cancellationToken);
   ```

#### Verification
- Enable EF Core query logging (already available via Serilog)
- Perform each operation and verify only 1-2 queries are executed instead of N+1
- Compare response times before and after

---

### Task 3.3: Remove Simulation Logic from Production

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 13.2, Issue #5

#### What to do
Remove the random 5% payment failure simulation.

#### Steps

1. **Update PaymentService:**
   - **File:** `src/backend/ECommerce.Application/Services/PaymentService.cs`
   - Find `ShouldSimulatePaymentFailure()` method (around line 240-243)
   - Either remove it entirely or gate it behind configuration:
   ```csharp
   private bool ShouldSimulatePaymentFailure()
   {
       // Only simulate failures in development/testing
       if (_environment.IsProduction())
           return false;

       var simulateFailures = _configuration.GetValue<bool>("Payment:SimulateFailures", false);
       if (!simulateFailures)
           return false;

       var random = new Random();
       return random.Next(0, 100) < 5;
   }
   ```

2. **Inject IWebHostEnvironment** into PaymentService constructor if not already present.

#### Verification
- In production configuration: payments should never randomly fail
- In development with `Payment:SimulateFailures=true`: 5% failure still works

---

### Task 3.4: Replace Static Payment Store

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 13.2, Issue #2

#### What to do
Replace the static `MockPaymentStore` with a scoped/proper service.

#### Steps

1. **Create an IPaymentStore interface:**
   - **File:** `src/backend/ECommerce.Application/Interfaces/IPaymentStore.cs` (NEW FILE)
   ```csharp
   namespace ECommerce.Application.Interfaces;

   public interface IPaymentStore
   {
       Task StorePaymentAsync(string paymentId, PaymentDetailsDto details);
       Task<PaymentDetailsDto?> GetPaymentAsync(string paymentId);
       Task RemovePaymentAsync(string paymentId);
   }
   ```

2. **Create InMemoryPaymentStore (for development):**
   - **File:** `src/backend/ECommerce.Infrastructure/Services/InMemoryPaymentStore.cs` (NEW FILE)
   - Register as **Singleton** but with proper thread safety:
   ```csharp
   using System.Collections.Concurrent;

   public class InMemoryPaymentStore : IPaymentStore
   {
       private readonly ConcurrentDictionary<string, PaymentDetailsDto> _store = new();

       public Task StorePaymentAsync(string paymentId, PaymentDetailsDto details)
       {
           _store[paymentId] = details;
           return Task.CompletedTask;
       }

       public Task<PaymentDetailsDto?> GetPaymentAsync(string paymentId)
       {
           _store.TryGetValue(paymentId, out var details);
           return Task.FromResult(details);
       }

       public Task RemovePaymentAsync(string paymentId)
       {
           _store.TryRemove(paymentId, out _);
           return Task.CompletedTask;
       }
   }
   ```

3. **Update PaymentService** to use the interface instead of static dictionary.

4. **Register in DI:**
   ```csharp
   builder.Services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
   ```

#### Verification
- Payment processing still works correctly
- No static dictionary in PaymentService
- For production: replace InMemoryPaymentStore with database-backed implementation

---

### Task 3.5: Fix Payment Failure HTTP Status Code

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 13.1, Issue #2

#### What to do
Return proper HTTP status codes for payment failures.

#### Steps

1. **Update PaymentsController:**
   - **File:** `src/backend/ECommerce.API/Controllers/PaymentsController.cs` (around line 53-64)
   - When payment fails, return 422 instead of 200:
   ```csharp
   if (!result.Success)
   {
       return UnprocessableEntity(ApiResponse<PaymentResultDto>.Error(
           result.Message ?? "Payment processing failed"));
   }

   return Ok(ApiResponse<PaymentResultDto>.Ok(result, "Payment processed successfully"));
   ```

#### Verification
- Trigger a payment failure -> should get 422, not 200
- Successful payment -> should still get 200

---

### Task 3.6: Extract Hardcoded Business Rules to Configuration

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 13.2, Issue #8

#### What to do
Move magic numbers to appsettings.json configuration.

#### Steps

1. **Create a configuration section:**
   - **File:** `src/backend/ECommerce.API/appsettings.json`
   ```json
   {
     "BusinessRules": {
       "FreeShippingThreshold": 100.00,
       "StandardShippingCost": 10.00,
       "TaxRate": 0.08
     }
   }
   ```

2. **Create a configuration class:**
   - **File:** `src/backend/ECommerce.Application/Configuration/BusinessRulesOptions.cs` (NEW FILE)
   ```csharp
   namespace ECommerce.Application.Configuration;

   public class BusinessRulesOptions
   {
       public const string SectionName = "BusinessRules";
       public decimal FreeShippingThreshold { get; set; } = 100.00m;
       public decimal StandardShippingCost { get; set; } = 10.00m;
       public decimal TaxRate { get; set; } = 0.08m;
   }
   ```

3. **Register in DI:**
   ```csharp
   builder.Services.Configure<BusinessRulesOptions>(
       builder.Configuration.GetSection(BusinessRulesOptions.SectionName));
   ```

4. **Update OrderService** to use IOptions<BusinessRulesOptions> instead of hardcoded values:
   - Inject `IOptions<BusinessRulesOptions>` in constructor
   - Replace hardcoded values (line 198-199) with configuration values

#### Verification
- Change values in appsettings.json
- Verify the new values are used in order calculations
- Default values still work if section is missing

---

### Task 3.7: Refactor OrderService.CreateOrderAsync

**Priority:** P2
**Depends on:** Task 3.1 (transaction management)
**Reference:** CODE_REVIEW.md Section 13.4

#### What to do
Break the 206-line method into smaller, focused methods.

#### Steps

1. **Extract helper methods** within OrderService:
   ```csharp
   // Extract these from CreateOrderAsync:
   private Order MapDtoToOrder(CreateOrderDto dto, Guid? userId) { ... }
   private ShippingAddress MapShippingAddress(AddressDto dto) { ... }
   private BillingAddress MapBillingAddress(AddressDto dto) { ... }
   private async Task<decimal> ApplyPromoCodeAsync(string promoCode, decimal subtotal, CancellationToken ct) { ... }
   private (decimal shipping, decimal tax) CalculateCharges(decimal subtotal) { ... }
   private async Task SendOrderConfirmationAsync(Order order, CreateOrderDto dto) { ... }
   ```

2. **The main method should read like a high-level recipe:**
   ```csharp
   public async Task<OrderDetailDto> CreateOrderAsync(CreateOrderDto dto, Guid? userId, CancellationToken ct)
   {
       await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
       try
       {
           var order = MapDtoToOrder(dto, userId);
           await ValidateAndLockStockAsync(order.Items, ct);
           await ApplyPromoCodeIfPresentAsync(order, dto.PromoCode, ct);
           CalculateOrderTotals(order);
           await PersistOrderAsync(order, ct);
           await ReduceStockAsync(order.Items, ct);
           await transaction.CommitAsync(ct);
           _ = SendOrderConfirmationAsync(order, dto);
           return _mapper.Map<OrderDetailDto>(order);
       }
       catch
       {
           await transaction.RollbackAsync(ct);
           throw;
       }
   }
   ```

#### Verification
- All existing order tests still pass
- No change in behavior
- Method is now under 30 lines

---

### Task 3.8: Add Missing Exception Types to Middleware

**Priority:** P2
**Depends on:** Task 1.8
**Reference:** CODE_REVIEW.md Section 13.3, Issue #2

#### What to do
Add handling for ArgumentNullException, ArgumentException, and InvalidOperationException.

#### Steps

1. **Update GlobalExceptionMiddleware exception mapping:**
   - **File:** `src/backend/ECommerce.API/Middleware/GlobalExceptionMiddleware.cs`
   - Add new cases to the exception handler switch:
   ```csharp
   var (statusCode, response) = exception switch
   {
       // Existing cases...
       NotFoundException ex => (StatusCodes.Status404NotFound,
           ApiResponse<object>.Error(ex.Message)),

       UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized,
           ApiResponse<object>.Error(ex.Message)),

       // NEW cases:
       ArgumentNullException ex => (StatusCodes.Status400BadRequest,
           ApiResponse<object>.Error($"Missing required parameter: {ex.ParamName}")),

       ArgumentException ex => (StatusCodes.Status400BadRequest,
           ApiResponse<object>.Error(ex.Message)),

       InvalidOperationException ex => (StatusCodes.Status409Conflict,
           ApiResponse<object>.Error(ex.Message)),

       // Generic fallback (no exception.Message!)
       _ => (StatusCodes.Status500InternalServerError,
           ApiResponse<object>.Error("An internal server error occurred. Please try again later."))
   };
   ```

#### Verification
- Throw ArgumentException from a service -> client gets 400
- Throw InvalidOperationException -> client gets 409
- Unknown exception -> client gets 500 with generic message

---

## Phase 4: Frontend Code Quality (P1-P2) ✅ COMPLETE

> **Goal:** Fix React anti-patterns, improve state management, and enhance error handling.
> **Depends on:** Can run in parallel with Phase 3.
> **Status:** All 10 tasks completed

---

### Task 4.1: Move LocalStorage Side Effects Out of Reducers

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.2, Issue #1

#### What to do
Replace localStorage calls in reducers with a Redux middleware.

#### Steps

1. **Create a cart persistence middleware:**
   - **File:** `src/frontend/storefront/src/store/middleware/cartPersistence.ts` (NEW FILE)
   ```typescript
   import { Middleware } from '@reduxjs/toolkit';
   import { saveCartToLocalStorage } from '../slices/cartSlice';

   const CART_ACTIONS = ['cart/addItem', 'cart/removeItem', 'cart/updateQuantity', 'cart/clearCart'];

   export const cartPersistenceMiddleware: Middleware = (store) => (next) => (action) => {
     const result = next(action);

     if (CART_ACTIONS.includes(action.type)) {
       const state = store.getState();
       saveCartToLocalStorage(state.cart.items);
     }

     return result;
   };
   ```

2. **Register middleware in store.ts:**
   - **File:** `src/frontend/storefront/src/store/store.ts`
   ```typescript
   import { cartPersistenceMiddleware } from './middleware/cartPersistence';

   export const store = configureStore({
     reducer: { ... },
     middleware: (getDefaultMiddleware) =>
       getDefaultMiddleware()
         .concat(/* existing API middlewares */)
         .concat(cartPersistenceMiddleware),
   });
   ```

3. **Remove localStorage calls from cartSlice reducers:**
   - **File:** `src/frontend/storefront/src/store/slices/cartSlice.ts`
   - Remove all `saveCartToLocalStorage(state.items)` calls from inside reducer functions (lines 70, 76, 94, 101)
   - Export `saveCartToLocalStorage` so the middleware can use it

#### Verification
- Add items to cart -> verify localStorage is updated
- Remove items -> verify localStorage is updated
- Cart state still persists across page reloads
- No `saveCartToLocalStorage` calls inside any reducer function

---

### Task 4.2: Create RTK Query Endpoints for Missing APIs

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.3, Issue #1

#### What to do
Replace raw fetch() calls in useCheckout with RTK Query mutations.

#### Steps

1. **Create promoCodeApi.ts:**
   - **File:** `src/frontend/storefront/src/store/api/promoCodeApi.ts` (NEW FILE)
   ```typescript
   import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
   import { API_URL } from '../../config';

   export const promoCodeApi = createApi({
     reducerPath: 'promoCodeApi',
     baseQuery: fetchBaseQuery({ baseUrl: API_URL }),
     endpoints: (builder) => ({
       validatePromoCode: builder.mutation<
         { isValid: boolean; discountAmount: number; message: string },
         { code: string; subtotal: number }
       >({
         query: (body) => ({
           url: '/promo-codes/validate',
           method: 'POST',
           body,
         }),
       }),
     }),
   });

   export const { useValidatePromoCodeMutation } = promoCodeApi;
   ```

2. **Create inventoryApi.ts** (if not exists):
   - **File:** `src/frontend/storefront/src/store/api/inventoryApi.ts` (NEW FILE)
   ```typescript
   import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
   import { API_URL } from '../../config';

   export const inventoryApi = createApi({
     reducerPath: 'inventoryApi',
     baseQuery: fetchBaseQuery({ baseUrl: API_URL }),
     endpoints: (builder) => ({
       checkAvailability: builder.mutation<
         { allAvailable: boolean; unavailableItems: string[] },
         { items: Array<{ productId: string; quantity: number }> }
       >({
         query: (body) => ({
           url: '/inventory/check-availability',
           method: 'POST',
           body,
         }),
       }),
     }),
   });

   export const { useCheckAvailabilityMutation } = inventoryApi;
   ```

3. **Register new APIs in store.ts:**
   - Add reducers and middlewares for the new API slices

4. **Update useCheckout.ts:**
   - **File:** `src/frontend/storefront/src/hooks/useCheckout.ts`
   - Replace raw fetch calls (lines 126-136 and 194-206) with RTK Query hooks:
   ```typescript
   const [validatePromoCode, { isLoading: validatingPromoCode }] = useValidatePromoCodeMutation();
   const [checkAvailability] = useCheckAvailabilityMutation();

   // Usage:
   const result = await validatePromoCode({ code: promoCode, subtotal }).unwrap();
   const stockResult = await checkAvailability({ items: cartItems }).unwrap();
   ```

#### Verification
- Promo code validation still works
- Stock checking still works
- Loading states are properly tracked
- Errors are properly handled
- No raw fetch() calls remain in useCheckout.ts

---

### Task 4.3: Replace Alert-Based Error Handling with Toast Notifications

**Priority:** P1
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.3, Issue #2

#### What to do
Install a toast notification library and replace all `alert()` calls.

#### Steps

1. **Install react-hot-toast** (lightweight, zero dependencies):
   ```bash
   cd src/frontend/storefront
   npm install react-hot-toast
   ```
   ```bash
   cd src/frontend/admin
   npm install react-hot-toast
   ```

2. **Add Toaster to App root:**
   - **File:** `src/frontend/storefront/src/App.tsx` (or main layout)
   ```typescript
   import { Toaster } from 'react-hot-toast';

   // Add inside the App component's return:
   <Toaster position="top-right" />
   ```
   - Do the same for admin app.

3. **Replace all alert() calls:**

   **Storefront - useCart.ts (line 115):**
   ```typescript
   // Before: alert('Failed to update item quantity');
   // After:
   import toast from 'react-hot-toast';
   toast.error('Failed to update item quantity');
   ```

   **Admin - Products.tsx (line 59):**
   ```typescript
   // Before: alert('Failed to delete product');
   toast.error('Failed to delete product');
   ```

   **Admin - Orders.tsx (line 41):**
   ```typescript
   // Before: alert('Failed to update order status');
   toast.error('Failed to update order status');
   ```

4. **Search for any remaining alert() calls:**
   ```bash
   grep -r "alert(" src/frontend/ --include="*.tsx" --include="*.ts"
   ```
   Replace all occurrences.

#### Verification
- Trigger an error -> toast notification appears in top-right corner
- Toast auto-dismisses after a few seconds
- No browser `alert()` dialogs appear anywhere
- Search confirms zero `alert(` calls in frontend code

---

### Task 4.4: Add Memoization to Cart Totals

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.1, Issue #3

#### What to do
Wrap cart total calculations in useMemo.

#### Steps

1. **Update useCart.ts:**
   - **File:** `src/frontend/storefront/src/hooks/useCart.ts` (around line 80-83)
   ```typescript
   // Before (recalculates every render):
   // const cartSubtotal = displayItems.reduce(...);

   // After:
   const { cartSubtotal, shipping, tax, total } = useMemo(() => {
     const subtotal = displayItems.reduce(
       (sum, item) => sum + item.price * item.quantity, 0
     );
     const shippingCost = subtotal > FREE_SHIPPING_THRESHOLD ? 0 : STANDARD_SHIPPING_COST;
     const taxAmount = subtotal * DEFAULT_TAX_RATE;
     return {
       cartSubtotal: subtotal,
       shipping: shippingCost,
       tax: taxAmount,
       total: subtotal + shippingCost + taxAmount,
     };
   }, [displayItems]);
   ```

2. **Add useMemo import** if not already imported:
   ```typescript
   import { useMemo } from 'react';
   ```

#### Verification
- Cart totals still calculate correctly
- Use React DevTools Profiler to confirm fewer recalculations

---

### Task 4.5: Memoize CartItem Component

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.1, Issue #4

#### What to do
Wrap CartItem in React.memo to prevent unnecessary re-renders.

#### Steps

1. **Wrap CartItem with React.memo:**
   - **File:** `src/frontend/storefront/src/components/CartItem.tsx`
   ```typescript
   // Change export from:
   // export default function CartItem({ ... }) { ... }

   // To:
   const CartItem = React.memo(function CartItem({ ... }: CartItemProps) {
     // ... existing component code
   });

   export default CartItem;
   ```

2. **Ensure parent wraps callbacks in useCallback:**
   - In the parent component that renders CartItem, wrap `onUpdateQuantity` and `onRemove` callbacks:
   ```typescript
   const handleUpdateQuantity = useCallback((itemId: string, quantity: number) => {
     dispatch(updateQuantity({ itemId, quantity }));
   }, [dispatch]);

   const handleRemove = useCallback((itemId: string) => {
     dispatch(removeItem(itemId));
   }, [dispatch]);
   ```

#### Verification
- Use React DevTools Profiler
- Adding item to cart should only re-render affected CartItem, not all CartItems

---

### Task 4.6: Fix TypeScript `any` Types

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.4

#### What to do
Replace all `any` types with proper TypeScript types.

#### Steps

1. **Fix each `any` type:**

   **Admin Orders.tsx:**
   - Define proper Order interface or import from types
   ```typescript
   // Before: useGetOrdersQuery<any[]>
   // After: useGetOrdersQuery<Order[]>
   ```

   **Admin Products.tsx:**
   ```typescript
   // Before: async (data: any) =>
   // After: async (data: CreateProductDto) =>
   ```

   **useProfileForm.ts:**
   ```typescript
   // Before: profile: any
   // After: profile: UserProfile | null
   ```

   **Admin ProductForm.tsx:**
   ```typescript
   // Before: onSubmit: (data: any) => Promise<void>
   // After: onSubmit: (data: ProductFormData) => Promise<void>
   ```

2. **Remove unsafe type casting:**
   ```typescript
   // Before: setEditingProduct(product as ProductDetail)
   // After: Use proper type or fetch the full product detail
   ```

#### Verification
- Run `npx tsc --noEmit` -> no type errors
- No `any` types remaining in codebase (search: `grep -r ": any" src/frontend/`)

---

### Task 4.7: Remove Unnecessary useCallback Wrappers

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.5, Issue #1

#### What to do
Remove useCallback from simple dispatch calls in useAuth.ts.

#### Steps

1. **Identify simple callbacks** that don't need memoization:
   - **File:** `src/frontend/storefront/src/hooks/useAuth.ts` (lines 33-113)
   - Simple dispatch wrappers don't benefit from useCallback
   - Only keep useCallback for callbacks passed as props to memoized children

2. **Replace with plain functions:**
   ```typescript
   // Before:
   const handleLoginSuccess = useCallback(
     (userData: AuthUser, authToken: string) => {
       dispatch(loginSuccess({ user: userData, token: authToken }));
       persistToken(authToken);
       clearError();
     },
     [dispatch, persistToken, clearError]
   );

   // After:
   const handleLoginSuccess = (userData: AuthUser, authToken: string) => {
     dispatch(loginSuccess({ user: userData, token: authToken }));
     localStorage.setItem('token', authToken);
   };
   ```

3. **Keep useCallback only where needed:**
   - Functions passed as props to React.memo components
   - Functions used as dependencies in useEffect

#### Verification
- All auth functionality still works
- Login/logout/register flows unchanged
- Reduced code complexity

---

### Task 4.8: Fix QueryRenderer Default Function

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.1, Issue #5

#### What to do
Extract the default isEmpty function outside the component.

#### Steps

1. **Update QueryRenderer.tsx:**
   - **File:** `src/frontend/storefront/src/components/QueryRenderer.tsx`
   ```typescript
   // Define outside component (module-level constant)
   const defaultIsEmpty = <T,>(data: T): boolean =>
     !data || (Array.isArray(data) && data.length === 0);

   // Inside component, use as default:
   function QueryRenderer<T>({
     isEmpty = defaultIsEmpty,
     // ... other props
   }: QueryRendererProps<T>) {
     // ...
   }
   ```

#### Verification
- QueryRenderer still works correctly with and without custom isEmpty
- No functional changes

---

### Task 4.9: Consolidate Form Handling

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 14.5, Issues #2 and #3

#### What to do
Use the existing useForm hook consistently across all forms.

#### Steps

1. **Audit all form implementations:**
   - `src/frontend/admin/src/components/ProductForm.tsx` - uses custom state
   - `src/frontend/storefront/src/hooks/useCheckout.ts` - uses custom state
   - `src/frontend/storefront/src/hooks/useProfileForm.ts` - uses custom state

2. **For each form, refactor to use the existing useForm hook:**
   - **File:** `src/frontend/storefront/src/hooks/useForm.ts` (already exists)
   - Update ProductForm.tsx to use useForm instead of manual useState
   - Ensure useForm supports validation rules

3. **Create shared validation utilities:**
   - **File:** `src/frontend/storefront/src/utils/validation.ts` (NEW or update existing)
   ```typescript
   export const validators = {
     required: (value: string) => value.trim() ? '' : 'This field is required',
     email: (value: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value) ? '' : 'Invalid email',
     minLength: (min: number) => (value: string) =>
       value.length >= min ? '' : `Must be at least ${min} characters`,
     phone: (value: string) => /^\+?[\d\s-]{10,}$/.test(value) ? '' : 'Invalid phone number',
   };
   ```

#### Verification
- All forms still work correctly
- Validation messages appear properly
- Consistent behavior across all forms

---

### Task 4.10: Fix Silent Error Catching

**Priority:** P2
**Depends on:** Task 4.3 (toast notifications installed)
**Reference:** CODE_REVIEW.md Section 14.3, Issue #3

#### What to do
Replace empty catch blocks with proper error handling.

#### Steps

1. **Fix ReviewForm.tsx:**
   - **File:** `src/frontend/storefront/src/components/ReviewForm.tsx` (line 41)
   ```typescript
   // Before:
   } catch {
     // Error handled by error state
   }

   // After:
   } catch (error) {
     console.error('Failed to submit review:', error);
     toast.error('Failed to submit review. Please try again.');
   }
   ```

2. **Search for all empty catch blocks:**
   ```bash
   grep -rn "catch\s*{" src/frontend/ --include="*.tsx" --include="*.ts"
   ```
   Fix each one with proper error handling.

#### Verification
- Trigger an error in review submission -> user sees toast notification
- Error is logged to console for debugging
- No empty catch blocks remain

---

## Phase 5: Infrastructure & DevOps (P2) ✅ **Complete**

> **Goal:** Add CI/CD, clean up repository, and standardize dependencies.
> **Depends on:** Phase 1 completed. Can run in parallel with Phases 3-4.
> **Status:** All 5 tasks complete. Repository cleaned, CI/CD operational, documentation organized.

---

### Task 5.1: Remove Large Files and Update .gitignore

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 3, Issues #5 and #6

#### Steps

1. **Remove PDF from repository:**
   ```bash
   git rm "Ultimate.ASP.NET.Core.Web.API*.pdf"
   ```

2. **Remove unused SQL Server dependency:**
   - **File:** `src/backend/ECommerce.API/ECommerce.API.csproj`
   - Remove the line: `<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" ... />`

3. **Clean up test results:**
   ```bash
   git rm -r src/backend/ECommerce.Tests/TestResults/
   ```

4. **Update .gitignore:**
   ```
   # Test results
   **/TestResults/

   # Large files
   *.pdf

   # IDE
   .vs/
   .vscode/
   *.user
   ```

---

### Task 5.2: Standardize React Router Versions

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 3, Issue #4

#### Steps

1. **Update admin app:**
   ```bash
   cd src/frontend/admin
   npm install react-router-dom@^7
   ```

2. **Update admin routing code** for React Router v7 API changes.

3. **Test all admin routes** after migration.

---

### Task 5.3: Set Up GitHub Actions CI/CD

**Priority:** P2
**Depends on:** Task 1.3 (secrets management)

#### Steps

1. **Create workflow file:**
   - **File:** `.github/workflows/ci.yml` (NEW FILE)
   ```yaml
   name: CI

   on:
     push:
       branches: [main]
     pull_request:
       branches: [main]

   jobs:
     backend-tests:
       runs-on: ubuntu-latest
       services:
         postgres:
           image: postgres:alpine
           env:
             POSTGRES_DB: ECommerceTestDb
             POSTGRES_USER: test
             POSTGRES_PASSWORD: test
           ports:
             - 5432:5432
           options: >-
             --health-cmd pg_isready
             --health-interval 10s
             --health-timeout 5s
             --health-retries 5

       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with:
             dotnet-version: '10.0.x'
         - run: dotnet restore src/backend/ECommerce.API/ECommerce.API.csproj
         - run: dotnet build src/backend/ECommerce.API/ECommerce.API.csproj --no-restore
         - run: dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --no-restore --verbosity normal

     frontend-build:
       runs-on: ubuntu-latest
       strategy:
         matrix:
           app: [storefront, admin]
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-node@v4
           with:
             node-version: '22'
             cache: 'npm'
             cache-dependency-path: src/frontend/${{ matrix.app }}/package-lock.json
         - run: npm ci
           working-directory: src/frontend/${{ matrix.app }}
         - run: npm run build
           working-directory: src/frontend/${{ matrix.app }}
   ```

2. **Add Dependabot:**
   - **File:** `.github/dependabot.yml` (NEW FILE)
   ```yaml
   version: 2
   updates:
     - package-ecosystem: "nuget"
       directory: "/src/backend"
       schedule:
         interval: "weekly"

     - package-ecosystem: "npm"
       directory: "/src/frontend/storefront"
       schedule:
         interval: "weekly"

     - package-ecosystem: "npm"
       directory: "/src/frontend/admin"
       schedule:
         interval: "weekly"
   ```

---

### Task 5.4: Clean Up Empty Shared Directories

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 3, Issue #3

#### Steps

1. **Decision point:** Either implement shared types or remove empty directories.

2. **Option A - Implement (Recommended):**
   - Use NSwag to auto-generate TypeScript types from the .NET API
   - **File:** `src/shared/api-contracts/generate.sh` (NEW FILE)
   ```bash
   # Generate TypeScript types from Swagger/OpenAPI spec
   npx @openapitools/openapi-generator-cli generate \
     -i http://localhost:5000/swagger/v1/swagger.json \
     -g typescript-fetch \
     -o src/shared/api-contracts/generated
   ```

3. **Option B - Remove:**
   ```bash
   rm -rf src/shared/api-contracts/
   rm -rf src/frontend/shared/api/
   ```

---

### Task 5.5: Consolidate Documentation

**Priority:** P3
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 10

#### Steps

1. **Move planning documents to /docs:**
   ```bash
   mv TESTING_PHASE_1_CHECKLIST.md docs/
   mv src/frontend/storefront/PHASE_2_IMPROVEMENTS_PLAN.md docs/
   ```

2. **Ensure docs/ has:**
   - `docs/ARCHITECTURE.md` - System architecture overview
   - `docs/DEVELOPMENT.md` - Local development setup guide
   - `docs/DEPLOYMENT.md` - Deployment instructions
   - `docs/API.md` - API documentation reference

---

## Phase 6: Testing & Quality Assurance (P2-P3)

> **Goal:** Add frontend testing framework, improve backend test coverage.
> **Depends on:** Phase 4 frontend changes completed.

---

### Task 6.1: Set Up Frontend Testing Framework

**Priority:** P2
**Depends on:** Nothing

#### Steps

1. **Install Vitest and React Testing Library:**
   ```bash
   cd src/frontend/storefront
   npm install -D vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event jsdom
   ```

2. **Configure Vitest:**
   - **File:** `src/frontend/storefront/vite.config.ts`
   ```typescript
   export default defineConfig({
     // ... existing config
     test: {
       globals: true,
       environment: 'jsdom',
       setupFiles: './src/test/setup.ts',
       css: true,
     },
   });
   ```

3. **Create test setup:**
   - **File:** `src/frontend/storefront/src/test/setup.ts` (NEW FILE)
   ```typescript
   import '@testing-library/jest-dom';
   ```

4. **Add test script to package.json:**
   ```json
   {
     "scripts": {
       "test": "vitest",
       "test:coverage": "vitest run --coverage"
     }
   }
   ```

5. **Write initial tests for critical paths:**
   - Cart operations (add, remove, update quantity)
   - Checkout form validation
   - Authentication flow

---

### Task 6.2: Write Critical Path Tests (Frontend)

**Priority:** P2
**Depends on:** Task 6.1

#### Steps

1. **Test cartSlice reducers:**
   - **File:** `src/frontend/storefront/src/store/slices/__tests__/cartSlice.test.ts` (NEW FILE)
   - Test: addItem, removeItem, updateQuantity, clearCart

2. **Test useCart hook:**
   - Test: total calculations, item management

3. **Test CheckoutForm component:**
   - Test: form validation, submission

4. **Test ErrorBoundary:**
   - Test: error catching and display

---

### Task 6.3: Add E2E Testing with Playwright

**Priority:** P3
**Depends on:** Task 6.1

#### Steps

1. **Install Playwright:**
   ```bash
   cd src/frontend/storefront
   npm install -D @playwright/test
   npx playwright install
   ```

2. **Create critical path E2E tests:**
   - Product browsing flow
   - Add to cart flow
   - Checkout flow (guest and authenticated)
   - Login/Register flow

---

### Task 6.4: Improve Backend Test Coverage

**Priority:** P2
**Depends on:** Phase 3 backend changes

#### Steps

1. **Add tests for new security features:**
   - Rate limiting behavior
   - Webhook signature verification
   - IDOR prevention (ownership checks)
   - Token refresh rotation

2. **Add tests for fixed race conditions:**
   - Concurrent order creation tests
   - Stock locking verification

3. **Measure coverage:**
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```
   Target: 80% coverage on critical paths (auth, orders, payments)

---

### Task 6.5: Add Database Seeding Environment Guards

**Priority:** P2
**Depends on:** Nothing
**Reference:** CODE_REVIEW.md Section 4

#### Steps

1. **Update DatabaseSeeder:**
   - **File:** `src/backend/ECommerce.Infrastructure/Data/DatabaseSeeder.cs`
   - Add environment check:
   ```csharp
   public async Task SeedAsync(IWebHostEnvironment environment)
   {
       if (environment.IsProduction())
       {
           _logger.LogInformation("Skipping database seeding in production");
           return;
       }

       // Existing seeding logic...
   }
   ```

---

## Implementation Checklist

Use this checklist to track progress across all phases:

### Phase 1: Critical Security (4/8 Complete) ⚠️
- [ ] Task 1.1: Revoke exposed credentials
- [ ] Task 1.2: Remove credentials from git history
- [ ] Task 1.3: Implement secure secrets management
- [ ] Task 1.4: Fix .env file tracking
- [x] Task 1.5: Add security headers middleware
- [x] Task 1.6: Implement rate limiting
- [x] Task 1.7: Implement webhook signature verification
- [x] Task 1.8: Fix information disclosure in errors

### Phase 2: Authentication & Access Control ✅
- [x] Task 2.1: Fix token validation (issuer/audience)
- [x] Task 2.2: Implement proper token refresh
- [x] Task 2.3: Fix IDOR in orders endpoint
- [x] Task 2.4: Fix IDOR in reviews endpoint
- [x] Task 2.5: Fix CORS configuration for production
- [x] Task 2.6: Add security event logging

### Phase 3: Backend Code Quality ✅
- [x] Task 3.1: Fix race condition in order creation
- [x] Task 3.2: Fix N+1 query problems
- [x] Task 3.3: Remove simulation logic from production
- [x] Task 3.4: Replace static payment store
- [x] Task 3.5: Fix payment failure HTTP status code
- [x] Task 3.6: Extract hardcoded business rules to config
- [x] Task 3.7: Refactor OrderService.CreateOrderAsync
- [x] Task 3.8: Add missing exception types to middleware

### Phase 4: Frontend Code Quality ✅
- [x] Task 4.1: Move localStorage side effects out of reducers
- [x] Task 4.2: Create RTK Query endpoints for missing APIs
- [x] Task 4.3: Replace alerts with toast notifications
- [x] Task 4.4: Add memoization to cart totals
- [x] Task 4.5: Memoize CartItem component
- [x] Task 4.6: Fix TypeScript `any` types
- [x] Task 4.7: Remove unnecessary useCallback wrappers
- [x] Task 4.8: Fix QueryRenderer default function
- [x] Task 4.9: Consolidate form handling
- [x] Task 4.10: Fix silent error catching

### Phase 5: Infrastructure & DevOps
- [x] Task 5.1: Remove large files and update .gitignore
- [x] Task 5.2: Standardize React Router versions
- [x] Task 5.3: Set up GitHub Actions CI/CD
- [x] Task 5.4: Clean up empty shared directories
- [x] Task 5.5: Consolidate documentation

### Phase 6: Testing & Quality Assurance
- [x] Task 6.1: Set up frontend testing framework
- [x] Task 6.2: Write critical path tests (frontend)
- [ ] Task 6.3: Add E2E testing with Playwright
- [x] Task 6.4: Improve backend test coverage
- [x] Task 6.5: Add database seeding environment guards

---

## Success Criteria

After completing all phases, the application should:

1. **Security:** Pass OWASP Top 10 compliance (target: 8/10 or better)
2. **Performance:** No N+1 queries, proper memoization in frontend
3. **Code Quality:** No `any` types, no side effects in reducers, no alert() calls
4. **Testing:** 80%+ backend coverage, frontend test framework operational
5. **DevOps:** CI/CD pipeline running, automated security scanning
6. **Data Integrity:** Race conditions eliminated, transactions properly managed

---

**Plan Created:** February 6, 2026  
**Last Updated:** February 6, 2026  
**Total Tasks:** 42  
**Completed:** 37 (88%)  
**Phases Complete:** 5 of 6 (Phase 2, 3, 4, 5 & 6)  
**Estimated Remaining Effort:** 2-4 days  
**Critical Path:** Phase 1 ⚠️ 50% -> Phase 2 ✅ -> Phases 3✅/4✅/5✅ (parallel) -> Phase 6 ✅ 80%
