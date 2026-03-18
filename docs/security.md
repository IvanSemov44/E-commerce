# Security Model

---

## Authentication — JWT + Refresh Token Rotation

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant DB

    Client->>API: POST /auth/login {email, password}
    API->>DB: Verify password hash (BCrypt)
    API->>DB: Store RefreshToken (hashed, 7-day expiry)
    API-->>Client: { accessToken (15 min), refreshToken (7 days) }

    Note over Client: accessToken expires

    Client->>API: POST /auth/refresh {refreshToken}
    API->>DB: Validate token — not expired, not revoked
    API->>DB: DELETE old RefreshToken (rotation)
    API->>DB: INSERT new RefreshToken
    API-->>Client: { new accessToken, new refreshToken }

    Note over Client: User logs out

    Client->>API: POST /auth/logout {refreshToken}
    API->>DB: Mark RefreshToken IsRevoked = true
    API-->>Client: 204 No Content
```

**Key properties:**
- Access tokens: short-lived (15 min), stateless JWT — no DB lookup on every request
- Refresh tokens: long-lived (7 days), stored hashed in `RefreshTokens` table
- **Token rotation** — every refresh issues a new refresh token and revokes the old one. A stolen token can only be used once before it's invalidated
- Logout revokes the refresh token server-side — the access token remains valid until its 15 min expiry (acceptable tradeoff; use token blacklist for stricter requirements)

---

## Authorization — Role-Based Access Control

Two roles defined in `UserRole` enum:

| Role | Value | Access |
|------|-------|--------|
| `Customer` | 0 | Public endpoints + own data only |
| `Admin` | 1 | All endpoints including admin-only |

Controllers use standard ASP.NET Core policy attributes:

```csharp
[Authorize]                          // any authenticated user
[Authorize(Roles = "Admin")]         // admin only
// no attribute = public
```

**Ownership enforcement** — services check `ICurrentUserService.UserId` before returning user-scoped data. A customer cannot read another customer's orders even with a valid JWT.

---

## Password Security

| Concern | Implementation |
|---------|---------------|
| Storage | BCrypt hash — never stored in plaintext |
| Reset flow | Cryptographic random token, stored hashed, 1-hour expiry |
| Reset token exposure | Token sent via email only, not returned in API response |
| Min strength | Enforced via FluentValidation on `RegisterDto` / `ChangePasswordDto` |

---

## Input Validation

All write endpoints (POST, PUT, PATCH) are decorated with `[ValidationFilter]`. This intercepts the request before it reaches the controller action and returns `422 Unprocessable Entity` with field-level errors if validation fails.

Validators live in `ECommerce.Application/Validators/`. Every DTO has a corresponding `AbstractValidator<T>`.

**Common validations enforced:**
- Email format + uniqueness check
- Price / quantity — must be positive
- String length limits matching DB column sizes
- Enum range checks
- Required field checks

---

## CSRF Protection

The API uses `CsrfMiddleware` which requires a CSRF token header on state-changing requests from browser clients. The token is issued as a cookie and must be echoed back as a request header — the double-submit cookie pattern.

This protects against cross-site request forgery on authenticated endpoints.

---

## Rate Limiting

Applied at the ASP.NET Core middleware level. Current limits:

| Endpoint group | Limit |
|----------------|-------|
| `/auth/login` | 10 requests / minute per IP |
| `/auth/register` | 5 requests / minute per IP |
| `/auth/forgot-password` | 3 requests / minute per IP |
| All other endpoints | 100 requests / minute per IP |

Exceeds limit → `429 Too Many Requests`.

---

## Security Headers

`SecurityHeadersMiddleware` adds the following headers to every response:

| Header | Value |
|--------|-------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `X-XSS-Protection` | `1; mode=block` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Content-Security-Policy` | restrictive policy (no inline scripts) |

---

## Stripe Webhook Verification

The `POST /payments/webhook` endpoint verifies the `Stripe-Signature` header using HMAC-SHA256 before processing any event. Requests without a valid signature are rejected with `401`.

This prevents attackers from spoofing payment events.

Implemented in `WebhookVerificationService.cs`.

---

## Sensitive Data Masking

`StringMaskingExtensions` in Core is used to mask sensitive fields (e.g. card numbers, tokens) before writing to logs. Serilog is configured to redact properties marked with `[SensitiveData]`.

**Never log:** passwords, raw tokens, card numbers, SSNs.

---

## CORS

CORS policy is configured in `Program.cs` via `CorsPolicyNames` constants. Only the known frontend origins (storefront and admin panel) are whitelisted.

In development: `http://localhost:5173`, `http://localhost:5174`
In production: configured via environment variable `AllowedOrigins`

---

## Threat Model Summary

```mermaid
graph TD
    A[Attacker] -->|Brute force login| B[Rate limiting + BCrypt cost]
    A -->|Stolen access token| C[15-min expiry limits damage window]
    A -->|Stolen refresh token| D[Token rotation — single use only]
    A -->|CSRF from malicious site| E[CSRF middleware blocks state changes]
    A -->|SQL injection| F[EF Core parameterised queries]
    A -->|XSS| G[Security headers + CSP]
    A -->|Fake Stripe webhook| H[HMAC-SHA256 signature verification]
    A -->|Access other users data| I[Ownership check in every service]
    A -->|Mass enumeration| J[Rate limiting per IP]
```

---

## Security Checklist (before going to production)

- [ ] Replace JWT `SecretKey` with a cryptographically random 64-char key from a secrets manager
- [ ] Set `RefreshTokenExpiryDays` to your policy (7 days is a reasonable default)
- [ ] Configure real Stripe webhook signing secret
- [ ] Restrict `AllowedOrigins` to your production domain only
- [ ] Enable HTTPS-only (`UseHttpsRedirection`, `HSTS`)
- [ ] Set `Secure` + `HttpOnly` + `SameSite=Strict` flags on auth cookies
- [ ] Rotate DB credentials from defaults (`postgres/postgres`)
- [ ] Enable PostgreSQL SSL connection (`sslmode=require` in connection string)
- [ ] Review and tighten CSP for production (remove any `unsafe-*` directives)
- [ ] Set up alerts for repeated 401/403 spikes (potential credential stuffing)
