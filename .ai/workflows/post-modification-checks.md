# Workflow: Post-Modification Checks

Updated: 2026-03-15
Owner: @ivans

## Purpose
Run this after every code change — refactor, bug fix, feature, or cleanup — before committing. Catches type errors, lint violations, formatting drift, and broken tests early.

## Required Order

Run steps in this order. Stop and fix on first failure.

### 1. TypeScript — type check only (no emit)

**Frontend (storefront):**
```bash
cd src/frontend/storefront
npx tsc --noEmit
```

**Frontend (admin):**
```bash
cd src/frontend/admin
npx tsc --noEmit
```

**Backend:**
```bash
cd src/backend
dotnet build
```

> Use `tsc --noEmit` not `npm run build` — build includes Vite bundling and takes longer.

---

### 2. ESLint — lint the changed app

**Storefront:**
```bash
cd src/frontend/storefront
npm run lint
```

**Admin:**
```bash
cd src/frontend/admin
npm run lint
```

> To auto-fix lint violations: `npm run lint:fix`

---

### 3. Prettier — format check (or write)

**After writing code** — format the changed files:
```bash
npx prettier --write "src/app/YourFolder/**/*.{ts,tsx}"
```

**Before committing** — verify no files are unformatted:
```bash
cd src/frontend/storefront   # or admin
npm run format:check
```

---

### 4. Tests — run targeted tests first

**Storefront targeted (single file or folder):**
```bash
cd src/frontend/storefront
npx vitest run src/path/to/__tests__/YourComponent.test.tsx
```

**Full storefront suite:**
```bash
npm run test:run
```

**With coverage:**
```bash
npm run test:coverage
```

**Backend:**
```bash
cd src/backend
dotnet test
```

> Run targeted tests first for fast feedback. Run the full suite before committing.

---

### 5. E2E — only when user flows are affected

Run E2E if the change touches auth, cart, checkout, or navigation:
```bash
cd src/frontend/storefront
npm run test:e2e
```

> Skip E2E for isolated component/hook/utility changes.

---

## Quick Reference — Full Storefront Verification

```bash
cd src/frontend/storefront
npx tsc --noEmit && npm run lint && npm run format:check && npm run test:run
```

## By Change Type

| What changed | Run |
|---|---|
| Component / hook / util | tsc + lint + format:check + targeted tests |
| Slice / RTK Query endpoint | tsc + lint + format:check + slice tests |
| Routing / page layout | tsc + lint + format:check + affected tests + E2E |
| Backend service / repository | dotnet build + dotnet test |
| Backend controller / contract | dotnet build + dotnet test (integration) |
| Schema / migration | dotnet build + dotnet test + see `database-migrations.md` |
| Cross-cutting (auth/cart/orders) | all of the above + E2E |

## Common Failures

- **TS error on unused import after refactor** — remove the import.
- **ESLint `react-hooks/exhaustive-deps`** — add the missing dependency or wrap the value.
- **Prettier diff on untouched file** — run `npm run format` once to align the whole project.
- **Test fails after removing a hook return value** — update the test to assert observable behavior instead.
