# Technologies Reference

Updated: 2026-03-08
Owner: @ivans

## Backend
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- FluentValidation
- AutoMapper

## Frontend
- React 19
- TypeScript
- Redux Toolkit + RTK Query
- React Router v7 (Library Mode now; Framework Mode migration planned — see `.ai/workflows/rr7-framework-migration.md`)
- Vite 7
- CSS Modules

## Tooling and Ops
- Docker / docker-compose
- PowerShell scripts for local workflows
- Test stack: Vitest, Testing Library, Playwright

## Where to verify exact versions
- Root dependencies: `package.json`
- Backend project files: `src/backend/**/*.csproj`
