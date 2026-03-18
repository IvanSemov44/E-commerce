# Developer Onboarding

Get the project running locally in under 10 minutes.

---

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10+ | https://dotnet.microsoft.com/download |
| Node.js | 20+ | https://nodejs.org |
| Docker Desktop | latest | https://www.docker.com/products/docker-desktop |
| Git | any | https://git-scm.com |

---

## 1. Clone & open

```bash
git clone <repo-url>
cd E-commerce
```

---

## 2. Start the database

```bash
docker compose up -d
```

This starts PostgreSQL on port `5432`. Verify with:

```bash
docker compose ps
```

---

## 3. Configure environment

Copy the example env file for the API:

```bash
cp src/backend/ECommerce.API/.env.example src/backend/ECommerce.API/.env
```

Key values to set in `.env` (or `appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ecommerce;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "your-dev-secret-key-min-32-chars",
    "Issuer": "ECommerce",
    "Audience": "ECommerce",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "EmailSettings": {
    "Provider": "Smtp",
    "SmtpHost": "localhost",
    "SmtpPort": 1025
  }
}
```

For the storefront frontend, create `src/frontend/storefront/.env.local`:

```env
VITE_API_BASE_URL=http://localhost:5000/api
```

---

## 4. Run database migrations + seed

```bash
cd src/backend/ECommerce.API
dotnet ef database update
```

The app auto-seeds on first run (categories, products, admin user). Or run manually:

```bash
dotnet run --seed
```

**Default admin credentials after seed:**
- Email: `admin@ecommerce.com`
- Password: `Admin123!`

---

## 5. Start the backend

```bash
cd src/backend/ECommerce.API
dotnet run
```

API available at: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

---

## 6. Start the storefront

```bash
cd src/frontend/storefront
npm install
npm run dev
```

Storefront available at: `http://localhost:5173`

---

## 7. Start the admin panel (optional)

```bash
cd src/frontend/admin
npm install
npm run dev
```

Admin panel available at: `http://localhost:5174`

---

## Run tests

**Backend:**
```bash
cd src/backend
dotnet test
```

**Frontend (unit):**
```bash
cd src/frontend/storefront
npm run test
```

**Frontend (E2E with Playwright):**
```bash
npm run test:e2e
```

---

## Project structure at a glance

```
src/
├── backend/
│   ├── ECommerce.Core/          Domain entities, interfaces, Result<T>
│   ├── ECommerce.Application/   Services, DTOs, validators
│   ├── ECommerce.Infrastructure/ Repositories, EF Core, migrations
│   ├── ECommerce.API/           Controllers, middleware, Program.cs
│   └── ECommerce.Tests/         Unit + integration tests
└── frontend/
    ├── storefront/              Customer-facing React app
    ├── admin/                   Admin dashboard React app
    └── shared/                  Shared components and utilities
```

Full architecture: [architecture.md](architecture.md)
Database schema: [database.md](database.md)

---

## Key concepts to understand first

| Concept | Where to read |
|---------|--------------|
| Clean Architecture layers | [architecture.md](architecture.md) |
| Why `Result<T>` instead of exceptions | [adr/002-result-pattern.md](adr/002-result-pattern.md) |
| How to add a new feature | `.ai/workflows/adding-feature.md` |
| Common mistakes to avoid | `.ai/reference/common-mistakes.md` |
| Error codes | `src/backend/ECommerce.Core/Constants/ErrorCodes.cs` |

---

## Common issues

**`dotnet ef` not found:**
```bash
dotnet tool install --global dotnet-ef
```

**Port 5432 already in use:**
```bash
docker compose down
# change port in docker-compose.yml if needed
```

**Frontend can't reach API (CORS error):**
Check `VITE_API_BASE_URL` in `.env.local` matches the running API port.

**Migrations out of date:**
```bash
dotnet ef database update --project src/backend/ECommerce.Infrastructure
```
