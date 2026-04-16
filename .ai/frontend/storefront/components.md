# Frontend Components Standard

Updated: 2026-03-15
Owner: @ivans

## Purpose
Keep components focused, composable, and easy to test.

## Writing Components

### Function declaration — always use this for components and hooks
```tsx
// ✅ correct
export function MyComponent({ title }: MyComponentProps) {
  return <div>{title}</div>;
}

// ❌ avoid — const/arrow expression for top-level named components
export const MyComponent = ({ title }: MyComponentProps) => <div>{title}</div>;

// ❌ avoid — named function expression assigned to const (worst of both)
export const MyComponent = function MyComponent({ title }: MyComponentProps) { ... };
```

### Export pattern — named exports only

Always use named exports. Default exports are not used in this project.

```tsx
// MyComponent.tsx
export function MyComponent(...) { ... }

// index.ts
export { MyComponent } from './MyComponent';
```

> **When editing any file** that uses `export default`, convert it to a named export as part of that edit. Update all import sites in the same change.

> **Exception — React Router route modules:** Files inside `src/app/routes/` are consumed by `flatRoutes()` (React Router Framework Mode). The route component **must** use `export default` — this is a framework requirement. Do **not** convert route module default exports to named exports.

### Props type — always a named interface in the same file as the component

Types that are only used by the component should be defined directly in the component file. Only create a separate `.types.ts` file if the type needs to be reused externally.
```tsx
interface MyComponentProps {
  title: string;
  onClose?: () => void;
}

export function MyComponent({ title, onClose }: MyComponentProps) { ... }
```

### Real examples in this project
- `src/frontend/storefront/src/features/checkout/components/OrderSummary/OrderSummary.tsx` — named export

## Async State Pattern — guard clauses for loading / error / empty

When a component receives `isLoading`, `error`, and `data` (from RTK Query or a hook), use **three early-return guard clauses** in this order, then the happy-path return last.

```tsx
export function MyList({ items, isLoading, error }: MyListProps) {
  if (isLoading) return <MyListSkeleton />;
  if (error)     return <ErrorAlert message={t('ns.failedToLoad')} />;
  if (!items.length) return <EmptyState title={t('ns.noItems')} />;

  return (
    <ul>
      {items.map((item) => <li key={item.id}>{item.name}</li>)}
    </ul>
  );
}
```

**Why this order matters**
- `isLoading` first — RTK Query can have both `isLoading: true` and `error` simultaneously on retry; show the spinner.
- `error` second — only reached once loading is definitely done.
- empty-state third — only when data is confirmed present but has zero items.

**When to skip the empty-state guard**
Skip it when the component renders meaningful UI even with an empty collection (e.g. `CategoryFilter` always shows an "All Products" button, so an empty category list is still usable).

**Anti-pattern — do not mix**
```tsx
// ❌ avoid — inconsistent: error exits early but isLoading is a ternary inside return
if (error) return <ErrorAlert />;

return (
  <div>
    {isLoading ? <Spinner /> : <ul>{items.map(...)}</ul>}
  </div>
);
```

**Real examples**
- `src/frontend/storefront/src/features/products/components/ReviewList/ReviewList.tsx`
- `src/frontend/storefront/src/features/products/components/CategoryFilter/CategoryFilter.tsx`

## Core Rules
1. Components should be presentational by default.
2. Keep API/data-fetch logic in feature API files and hooks.
3. Move complex state transitions to hooks or slices.
4. Keep props explicit and strongly typed.
5. Prefer composition over large monolithic components.

## Real Code References
- Shared UI components: `src/frontend/storefront/src/shared/components/`
- Feature UI components: `src/frontend/storefront/src/features/`
- App root: `src/frontend/storefront/src/app/root.tsx`

## Practical Guidance
- Co-locate feature-specific components under their feature folder.
- Keep shared components domain-agnostic in `shared/components`.
- Avoid passing raw API envelopes into UI components.

## Common Mistakes
- Embedding endpoint calls directly in component bodies.
- One component handling layout, data loading, mutation, and error parsing all at once.
- Reusing a component across features with incompatible prop contracts.

## Checklist
- [ ] Component uses a named export (`export function`, not `export default`).
- [ ] `index.ts` uses `export { Name }` not `export { default }`.
- [ ] Styles use CSS Modules (`.module.css`) — no Tailwind utility classes. When editing a file that uses Tailwind, convert it to CSS Modules in the same change.
- [ ] Component API is typed and minimal.
- [ ] Data fetching delegated to RTK Query hooks or feature hooks.
- [ ] Shared component is domain-agnostic.
- [ ] Rendering logic stays clear and testable.
