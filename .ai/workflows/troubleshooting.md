# Workflow: Troubleshooting

Updated: 2026-03-08
Owner: @ivans

## Purpose
Provide a fast triage path for the most common backend/frontend issues in this repository.

## Triage Order
1. Reproduce issue locally.
2. Check build/lint/test output.
3. Check environment/config.
4. Check logs and endpoint health.
5. Add regression test once fixed.

## Backend Issues

### Build/Test failures
```powershell
cd src/backend
dotnet build
dotnet test
```

### Migration issues
- Follow `.ai/workflows/database-migrations.md`.
- Ensure `-p ECommerce.Infrastructure -s ECommerce.API` are present.
- Do not edit old pushed migrations; add a new fix migration.

### Error-handling regressions
- Verify service returns `Result<T>` for business failures.
- Verify controller maps failure/success to `ApiResponse<T>`.
- Check: `.ai/backend/error-handling.md`

## Frontend Issues

### Storefront
```powershell
cd src/frontend/storefront
npm run build
npm run test:run
```

### Admin
```powershell
cd src/frontend/admin
npm run build
npm run test:run
```

### E2E flow failures
- Run storefront E2E:
```powershell
cd src/frontend/storefront
npm run test:e2e
```
- See guide: `src/frontend/storefront/e2e/README.md`

## Environment/Runtime Issues
- Validate required secrets/config values before startup.
- For containers:
```bash
docker-compose ps
docker-compose logs -f api
```
- Verify API health endpoint responds.

## Real Code References
- Global exception middleware: `src/backend/ECommerce.API/Middleware/GlobalExceptionMiddleware.cs`
- Validation filter: `src/backend/ECommerce.API/ActionFilters/ValidationFilterAttribute.cs`
- Backend error model: `src/backend/ECommerce.Core/Results/Result.cs`
- Storefront API base: `src/frontend/storefront/src/shared/lib/api/baseApi.ts`

## Common Failure Modes
- Fixing symptom without adding regression test.
- Mixing business failures with exceptions in service layer.
- Investigating UI bug without verifying API response shape.
- Ignoring env/config mismatch and debugging code first.
