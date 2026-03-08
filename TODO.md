# TODO

## Consolidate EmptyState components (storefront)

### Context
There are two different `EmptyState` implementations in the storefront codebase:

1. `src/frontend/storefront/src/shared/components/EmptyState/EmptyState.tsx`
   - Generic, slot-based component
   - Props: `icon?: ReactNode`, `title`, `description?`, `action?: ReactNode`
   - Wraps content in `Card`

2. `src/frontend/storefront/src/shared/components/ui/EmptyState/EmptyState.tsx`
   - Opinionated UI component + presets
   - Props: `icon: 'cart' | 'wishlist' | 'orders' | 'search' | 'error'`, `title`, `description`, optional action labels + handlers
   - Exports presets: `EmptyCart`, `EmptyWishlist`, `EmptyOrders`, `NoSearchResults`, `ErrorState`

This duplication increases confusion and maintenance cost.

### Suggested solution (low-risk, incremental)
1. Pick a single canonical component to keep (recommended: the `ui/EmptyState` one).
2. Add a compatibility shim at the old import path so nothing breaks immediately:
   - Update `src/frontend/storefront/src/shared/components/EmptyState/index.ts` to re-export from `shared/components/ui/EmptyState`.
   - Optionally keep `src/shared/components/EmptyState/EmptyState.tsx` temporarily but mark it `@deprecated`.
3. Migrate imports gradually:
   - Replace imports from `@/shared/components/EmptyState` with `@/shared/components/ui/EmptyState` (or the repo’s standard import style).
   - Adjust call sites because props differ (ReactNode slots vs `icon` union + button handlers).
4. Run `npm run typecheck` and `npm run lint` after each batch.
5. Once all references are migrated and checks pass, delete the deprecated implementation and folder.

### Notes
I (Copilot Chat) can help with the migration, but since I cannot run `tsc`/ESLint/Prettier here, prefer doing this in small steps with local checks or CI.