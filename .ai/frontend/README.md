# Frontend Documentation Hub

Updated: 2026-04-09
Owner: @ivans

This repository has two separate frontend applications. Start here to find the right docs.

---

## Which app are you working on?

| App | Location | Tech | Docs |
|---|---|---|---|
| **Storefront** | `src/frontend/storefront/` | React 19, RR7 Framework Mode, RTK Query, CSS Modules | `.ai/frontend/storefront/` |
| **Admin panel** | `src/frontend/admin/` | React 18, RR6 Library Mode, RTK Query, CSS Modules | `.ai/frontend/admin/` |

The two apps are independent — separate `package.json`, separate Vite configs, separate test setups. They share no source code. They do share the same backend API.

---

## Storefront docs

Start with `overview.md`, then the doc for the area you are changing.

| Doc | When to read |
|---|---|
| [overview.md](storefront/overview.md) | First time, or architecture questions |
| [routing.md](storefront/routing.md) | Adding or changing routes |
| [components.md](storefront/components.md) | Writing or reviewing components |
| [redux.md](storefront/redux.md) | State shape, slice vs RTK Query |
| [api-integration.md](storefront/api-integration.md) | Adding API endpoints, auth refresh |
| [forms.md](storefront/forms.md) | Any form — choose Pattern A or B |
| [auth-forms.md](storefront/auth-forms.md) | Auth-specific form implementation |
| [hooks.md](storefront/hooks.md) | Writing custom hooks |
| [route-loaders.md](storefront/route-loaders.md) | Loader constraints (SPA mode) |
| [loading-skeletons.md](storefront/loading-skeletons.md) | Bootstrap phases, skeleton system |
| [i18n.md](storefront/i18n.md) | Translation keys, adding locales |
| [styling.md](storefront/styling.md) | CSS Modules, design tokens |
| [accessibility.md](storefront/accessibility.md) | ARIA, keyboard, semantic HTML |
| [type-safety.md](storefront/type-safety.md) | No `any`, typed hooks and API |
| [testing.md](storefront/testing.md) | Unit/component/E2E test approach |

---

## Admin docs

| Doc | When to read |
|---|---|
| [overview.md](admin/overview.md) | Architecture, how it differs from storefront |

---

## Key differences between the two apps

| Concern | Storefront | Admin |
|---|---|---|
| Router | RR7 Framework Mode (`flatRoutes()`) | RR6 Library Mode (`<Routes>` in `App.tsx`) |
| Route files | `src/app/routes/` (file-based) | Manual `<Route>` tree in `App.tsx` |
| Auth guard | `_protected.tsx` pathless layout | `<ProtectedRoute>` component |
| API organisation | Feature-injected via `baseApi.injectEndpoints` | Flat `store/api/*.ts` files |
| State | Feature slices + RTK Query per feature | Two slices only: `authSlice`, `toastSlice` |
| Build tooling | `@react-router/dev/vite` (`reactRouter()`) | Standard `@vitejs/plugin-react` |
| Test setup | Vitest + MSW + Playwright | Vitest + Playwright |
| i18n | Yes — en + bg via `react-i18next` | No |
| CSS | CSS Modules (`*.module.css`) | CSS Modules (`*.module.css`) |
