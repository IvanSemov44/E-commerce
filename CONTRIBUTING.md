# Contributing to the E-Commerce Platform

First off, thank you for considering contributing! This project thrives on community involvement, and every contribution is appreciated.

This document provides guidelines for contributing to the project. Please read it carefully to ensure a smooth and effective collaboration process.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
  - [Reporting Bugs](#reporting-bugs)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Submitting Pull Requests](#submitting-pull-requests)
- [Development Workflow](#development-workflow)
  - [Prerequisites](#prerequisites)
  - [Branching](#branching)
  - [Coding Conventions](#coding-conventions)
- [Pull Request Process](#pull-request-process)

## Code of Conduct

This project and everyone participating in it is governed by a [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior. (Note: `CODE_OF_CONDUCT.md` to be created).

## How Can I Contribute?

### Reporting Bugs

If you find a bug, please ensure the bug was not already reported by searching on GitHub under [Issues](https://github.com/your-repo/issues). If you're unable to find an open issue addressing the problem, open a new one. Be sure to include a **title and clear description**, as much relevant information as possible, and a **code sample or an executable test case** demonstrating the expected behavior that is not occurring.

### Suggesting Enhancements

If you have an idea for an enhancement, please open an issue to discuss it. This allows us to coordinate efforts and ensure the proposal aligns with the project's goals.

### Submitting Pull Requests

We love pull requests! Please see the sections below on the development workflow and pull request process before you start.

## Development Workflow

### Prerequisites

-   Ensure you have followed the setup guide in [docs/onboarding.md](docs/onboarding.md) to get your local environment running.
-   Read [CLAUDE.md](CLAUDE.md) — the architectural rules that must not be violated.
-   Read [.ai/reference/common-mistakes.md](.ai/reference/common-mistakes.md) before writing any code.

### Branching

All development happens in feature branches off `main`.

1.  **Create a new branch** from the `main` branch:
    ```sh
    git checkout -b feat/stripe-integration
    git checkout -b fix/cart-concurrency
    git checkout -b refactor/checkout-hooks
    git checkout -b docs/adr-rtk-query
    git checkout -b chore/upgrade-dotnet
    ```

Branch prefixes: `feat/` · `fix/` · `refactor/` · `docs/` · `chore/` · `perf/` · `test/`

### Commit messages

Use conventional commits format:

```
<type>(<scope>): <short summary under 72 chars>

feat(checkout): add Stripe payment intent creation
fix(cart): resolve race condition on concurrent add-to-cart
refactor(products): split god hook into focused hooks
docs(adr): add ADR for RTK Query decision
chore(deps): upgrade to .NET 10.0.3
```

### Coding Conventions

-   **TypeScript/Frontend:** We use ESLint to enforce code style. Run `npm run lint` in the `admin` and `storefront` directories to check your code.
-   **C#/Backend:** Please follow the standard .NET/C# coding conventions. Most rules are enforced by modern IDEs like Visual Studio and JetBrains Rider.
-   **Commit Messages:** Write clear and concise commit messages. The first line should be a short summary (50 characters or less), followed by a blank line and a more detailed explanation if necessary.

#### Frontend Icon Components

All SVG icons must be imported from the centralized icons library (`src/shared/components/icons/`) rather than embedded as inline SVG elements.

**Creating a new icon:**

1. Create a new component file in `src/shared/components/icons/` (e.g., `MyIcon.tsx`):
   ```tsx
   import type { SVGProps } from 'react';

   export default function MyIcon(props: SVGProps<SVGSVGElement>) {
     return (
       <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" {...props}>
         <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M..." />
       </svg>
     );
   }
   ```

2. Export the icon in `src/shared/components/icons/index.ts`:
   ```ts
   export { default as MyIcon } from './MyIcon';
   ```

3. Use it anywhere in the app:
   ```tsx
   import { MyIcon } from '@/shared/components/icons';

   export function MyComponent() {
     return <MyIcon width={20} height={20} className="my-icon" />;
   }
   ```

**Available icons:** HeartIcon, ShoppingCartIcon, UserIcon, SearchIcon, PackageIcon, CheckIcon, DocumentIcon, ErrorIcon, CloseIcon, MenuIcon, ChevronDownIcon

**Never:** Create inline `<svg>` elements in components. Always use the icons library.

#### Frontend Import Path Conventions

Use the `@` alias for imports in the frontend instead of relative paths (`../../../`). This improves readability and makes refactoring easier.

**Alias mappings:**
- `@/features/*` → `src/features/*` (feature modules)
- `@/shared/*` → `src/shared/*` (shared components, hooks, utils)
- `@/` → `src/` (root level)

**Examples:**

```tsx
// ✅ GOOD - Use @ alias
import Button from '@/shared/components/ui/Button';
import { useGetOrdersQuery } from '@/features/orders/api/ordersApi';
import { useForm } from '@/shared/hooks/useForm';

// ❌ AVOID - Relative paths
import Button from '../../../../shared/components/ui/Button';
import { useGetOrdersQuery } from '../../api/ordersApi';
```

The `@` alias is configured in `tsconfig.json` and works in all `.ts` and `.tsx` files. Always prefer it over relative imports.

#### Frontend Component Colocation Architecture

Components follow a **colocation pattern** where a component and its related files are organized together in a dedicated folder structure. This improves code organization, reusability, and maintainability.

**Basic colocation structure (single hook/utility):**
```
ComponentName/
├── ComponentName.tsx         # Main component
├── ComponentName.types.ts    # TypeScript interfaces/types
├── ComponentName.module.css  # Scoped styles
├── ComponentName.hooks.ts    # Custom hooks (if ≤2 hooks)
├── ComponentName.utils.ts    # Utility functions
├── ComponentName.test.tsx    # Component tests
└── index.ts                  # Barrel export
```

**Advanced colocation structure (multiple hooks):**
```
ComponentName/
├── ComponentName.tsx         # Main component
├── ComponentName.types.ts    # TypeScript interfaces/types
├── ComponentName.module.css  # Scoped styles
├── ComponentName.test.tsx    # Component tests
├── hooks/                    # Separate folder for 3+ hooks
│   ├── useFirstHook.ts      # Individual hook file
│   ├── useSecondHook.ts     # Individual hook file
│   ├── useThirdHook.ts      # Individual hook file
│   └── index.ts             # Barrel export
├── utils/                    # Optional: separate utils folder if 5+ functions
│   ├── helper1.utils.ts
│   ├── helper2.utils.ts
│   └── index.ts
└── index.ts                  # Main barrel export
```

**When to use each pattern:**

1. **Single `.hooks.ts` file** (1-2 tightly-coupled hooks):
   ```tsx
   // ProductCard.hooks.ts
   export function useProductCardHandlers(...) { ... }
   export function useProductValidation(...) { ... }
   ```

2. **`hooks/` folder with separate files** (3+ hooks OR hooks used elsewhere):
   ```tsx
   // hooks/usePriceFilters.ts
   export function usePriceFilters(...) { ... }

   // hooks/useRatingFilter.ts
   export function useRatingFilter(...) { ... }

   // hooks/useFeaturedFilter.ts
   export function useFeaturedFilter(...) { ... }

   // hooks/index.ts
   export { usePriceFilters } from './usePriceFilters';
   export { useRatingFilter } from './useRatingFilter';
   export { useFeaturedFilter } from './useFeaturedFilter';
   ```

**Main component barrel export (`index.ts`):**
```typescript
export { default } from './ComponentName';
export type { ComponentNameProps, RelatedTypes } from './ComponentName.types';
export { useCustomHook, anotherHook } from './hooks'; // or './ComponentName.hooks'
export { utilFunction1, utilFunction2 } from './utils'; // or './ComponentName.utils'
```

**Benefits:**
- ✅ Encapsulation: All component-related code in one folder
- ✅ Scalability: Easy to add tests, hooks, utils without cluttering files
- ✅ Reusability: Clear barrel exports make it easy to import what you need
- ✅ Maintainability: Each file has a single responsibility
- ✅ Type safety: Centralized types in `.types.ts` files

## Pull Request Process

1.  Ensure your code adheres to the **Coding Conventions**.
2.  Make sure all existing **tests pass**. Add new tests for your feature or bug fix. Run tests with `dotnet test` in the `src/backend` directory.
3.  Update the [README.md](./README.md) or other relevant documentation if your changes impact the project's setup, environment variables, architecture, or established coding patterns.
4.  If your PR changes an established implementation pattern, update the related `.ai/` documentation in the same PR.
5.  If code and docs conflict, code is source of truth and docs must be corrected before merge.
4.  Push your feature branch to your fork on GitHub.
5.  Open a pull request from your feature branch to the `main` branch of the original repository.
6.  In the pull request description, clearly explain the **purpose** and **scope** of your changes. Link to any relevant issues.
7.  The pull request will be reviewed by maintainers. Address any feedback or requested changes.

Once your PR is approved and merges, your contribution will be part of the project. Thank you for your hard work!

## Documentation Maintenance Contract

To prevent documentation drift:

1. Pattern changes require documentation updates in the same commit/PR.
2. Do not leave "update docs later" for pattern-level changes.
3. If you add a new recurring pattern, document it in `.ai/` as part of implementation.
4. If you discover a repeated anti-pattern, add it to `.ai/reference/common-mistakes.md`.

### PR Checklist Additions

- [ ] Pattern changed? Related `.ai/` docs updated in this PR.
- [ ] New anti-pattern discovered? Added to `.ai/reference/common-mistakes.md`.
