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

### Export pattern — pick based on whether a named import is needed

**Named export only** (default when the component is consumed by `{ Name }` imports):
```tsx
// MyComponent.tsx
export function MyComponent(...) { ... }

// index.ts
export { MyComponent } from './MyComponent';
```

**Default export only** (for simple components not needing named access):
```tsx
// MyComponent.tsx
export default function MyComponent(...) { ... }

// index.ts
export { default } from './MyComponent';
```

**Both** (only when the component is imported both ways — check actual imports first):
```tsx
// MyComponent.tsx
export function MyComponent(...) { ... }
export default MyComponent;

// index.ts
export { MyComponent } from './MyComponent';
export { default } from './MyComponent';
```

> Before adding a default export, grep for `import MyComponent from` to confirm it is actually used as a default. If nothing imports it as default, use named only.

### Props type — always a named interface in the same file or a `.types.ts` file
```tsx
interface MyComponentProps {
  title: string;
  onClose?: () => void;
}

export function MyComponent({ title, onClose }: MyComponentProps) { ... }
```

### Real examples in this project
- Named only: `src/frontend/storefront/src/app/SearchBar/SearchBar.tsx`
- Default only: `src/frontend/storefront/src/app/AnnouncementBar/AnnouncementBar.tsx`
- Both (named + default): `src/frontend/storefront/src/app/LanguageSwitcher/LanguageSwitcher.tsx`

## Core Rules
1. Components should be presentational by default.
2. Keep API/data-fetch logic in feature API files and hooks.
3. Move complex state transitions to hooks or slices.
4. Keep props explicit and strongly typed.
5. Prefer composition over large monolithic components.

## Real Code References
- Shared UI components: `src/frontend/storefront/src/shared/components/`
- Feature UI components: `src/frontend/storefront/src/features/`
- App entry composition: `src/frontend/storefront/src/App.tsx`

## Practical Guidance
- Co-locate feature-specific components under their feature folder.
- Keep shared components domain-agnostic in `shared/components`.
- Avoid passing raw API envelopes into UI components.

## Common Mistakes
- Embedding endpoint calls directly in component bodies.
- One component handling layout, data loading, mutation, and error parsing all at once.
- Reusing a component across features with incompatible prop contracts.

## Checklist
- [ ] Component API is typed and minimal.
- [ ] Data fetching delegated to RTK Query hooks or feature hooks.
- [ ] Shared component is domain-agnostic.
- [ ] Rendering logic stays clear and testable.
