# Environments & Configuration

## Overview

| Environment | Purpose | Backend URL | Frontend URL |
|-------------|---------|-------------|--------------|
| Development | Local dev | `http://localhost:5000` | `http://localhost:5173` |
| Staging | Pre-release testing | TBD | TBD |
| Production | Live | TBD | TBD |

---

## All environment variables

### Backend — `appsettings.json` / environment variables

#### Database
| Variable | Example | Required | Notes |
|----------|---------|----------|-------|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Port=5432;Database=ecommerce;Username=postgres;Password=postgres` | Yes | PostgreSQL connection string |

#### JWT
| Variable | Example | Required | Notes |
|----------|---------|----------|-------|
| `JwtSettings__SecretKey` | `your-secret-key-min-32-chars` | Yes | Min 32 chars. Use a cryptographically random key in prod |
| `JwtSettings__Issuer` | `ECommerce` | Yes | |
| `JwtSettings__Audience` | `ECommerce` | Yes | |
| `JwtSettings__AccessTokenExpiryMinutes` | `15` | No | Default: 15 |
| `JwtSettings__RefreshTokenExpiryDays` | `7` | No | Default: 7 |

#### Email
| Variable | Example | Required | Notes |
|----------|---------|----------|-------|
| `EmailSettings__Provider` | `Smtp` or `SendGrid` | Yes | Selects which implementation to use |
| `EmailSettings__SmtpHost` | `smtp.example.com` | If SMTP | |
| `EmailSettings__SmtpPort` | `587` | If SMTP | |
| `EmailSettings__SmtpUsername` | `user@example.com` | If SMTP | |
| `EmailSettings__SmtpPassword` | `password` | If SMTP | Store in secrets manager in prod |
| `EmailSettings__SendGridApiKey` | `SG.xxxxx` | If SendGrid | Store in secrets manager in prod |
| `EmailSettings__FromEmail` | `noreply@ecommerce.com` | Yes | |
| `EmailSettings__FromName` | `ECommerce` | No | |

#### Payments (Stripe)
| Variable | Example | Required | Notes |
|----------|---------|----------|-------|
| `StripeSettings__SecretKey` | `sk_test_xxxx` | Yes (prod) | Use `sk_test_` in dev/staging |
| `StripeSettings__PublishableKey` | `pk_test_xxxx` | Yes (prod) | Sent to frontend |
| `StripeSettings__WebhookSecret` | `whsec_xxxx` | Yes (prod) | From Stripe dashboard webhook config |

#### CORS
| Variable | Example | Required | Notes |
|----------|---------|----------|-------|
| `AllowedOrigins` | `http://localhost:5173,http://localhost:5174` | Yes | Comma-separated list |

#### Logging
| Variable | Example | Required | Notes |
|----------|---------|----------|-------|
| `Serilog__MinimumLevel__Default` | `Information` | No | `Debug` in dev, `Warning` in prod |

---

### Frontend — `.env.local` (storefront)

| Variable | Example | Required | Notes |
|----------|---------|----------|-------|
| `VITE_API_BASE_URL` | `http://localhost:5000/api` | Yes | No trailing slash |
| `VITE_STRIPE_PUBLISHABLE_KEY` | `pk_test_xxxx` | Yes (prod) | Use test key in dev |
| `VITE_APP_ENV` | `development` | No | `development` / `staging` / `production` |

### Frontend — `.env.local` (admin)

| Variable | Example | Required | Notes |
|----------|---------|----------|-------|
| `VITE_API_BASE_URL` | `http://localhost:5000/api` | Yes | Same API, different origin |
| `VITE_APP_ENV` | `development` | No | |

---

## Per-environment config matrix

| Setting | Development | Staging | Production |
|---------|-------------|---------|------------|
| JWT secret | Any 32+ char string | Secrets manager | Secrets manager |
| DB | `localhost:5432` | Managed PostgreSQL | Managed PostgreSQL |
| Email provider | SMTP (Mailhog/localhost) | SendGrid sandbox | SendGrid production |
| Stripe keys | `sk_test_` / `pk_test_` | `sk_test_` / `pk_test_` | `sk_live_` / `pk_live_` |
| CORS origins | `localhost:5173,5174` | Staging domain | Production domain |
| Log level | `Debug` | `Information` | `Warning` |
| HTTPS | Optional | Required | Required |
| Data seeding | Auto on startup | Manual | Never |

---

## Local development: secrets

Never commit secrets. For local dev, use one of these approaches:

**Option A — `appsettings.Development.json`** (gitignored):
```json
{
  "JwtSettings": {
    "SecretKey": "dev-only-secret-key-32chars-min!!"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;..."
  }
}
```

**Option B — .NET User Secrets** (recommended):
```bash
cd src/backend/ECommerce.API
dotnet user-secrets set "JwtSettings:SecretKey" "dev-only-secret-32chars!"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;..."
```

---

## Production secrets checklist

- [ ] `JwtSettings__SecretKey` — 64-char cryptographically random string from secrets manager
- [ ] `ConnectionStrings__DefaultConnection` — production DB credentials, SSL enabled
- [ ] `StripeSettings__SecretKey` — live Stripe key (`sk_live_`)
- [ ] `StripeSettings__WebhookSecret` — from Stripe dashboard
- [ ] `EmailSettings__SendGridApiKey` — production SendGrid key
- [ ] `AllowedOrigins` — production domain only, no localhost
- [ ] No `.env` files committed to git (check `.gitignore`)

---

## Docker Compose (local)

`docker-compose.yml` at the project root starts:

| Service | Port | Credentials |
|---------|------|-------------|
| PostgreSQL | `5432` | `postgres` / `postgres` |
| (optional) Mailhog | `1025` (SMTP), `8025` (UI) | none |

```bash
docker compose up -d          # start
docker compose down           # stop
docker compose down -v        # stop + delete volumes (wipes DB)
```
