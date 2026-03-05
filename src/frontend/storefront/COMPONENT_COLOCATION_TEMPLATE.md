# Component Colocation Template (Storefront)

Use this template when creating a new colocated component folder.

## Folder Shape

```text
src/features/<feature>/components/<ComponentName>/
├─ <ComponentName>.tsx
├─ <ComponentName>.module.css
├─ <ComponentName>.test.tsx
├─ index.ts
├─ <ComponentName>.types.ts        (optional)
├─ <ComponentName>.hooks.ts        (optional)
└─ <ComponentName>.utils.ts        (optional)
```

## Rules

- Keep only component-local code in this folder.
- Move shared code to feature-level or `src/shared`.
- With React Compiler enabled, prefer plain functions; add manual `memo`/`useCallback` only for measured wins.
- Export through `index.ts` so imports stay stable.

## Starter Files

### `<ComponentName>.tsx`

```tsx
import styles from './<ComponentName>.module.css';

interface <ComponentName>Props {
  title: string;
  onAction?: () => void;
}

export default function <ComponentName>({ title, onAction }: <ComponentName>Props) {
  return (
    <section className={styles.container}>
      <h3 className={styles.title}>{title}</h3>
      {onAction ? (
        <button type="button" className={styles.actionButton} onClick={onAction}>
          Action
        </button>
      ) : null}
    </section>
  );
}
```

### `<ComponentName>.module.css`

```css
.container {
  display: grid;
  gap: 0.5rem;
}

.title {
  margin: 0;
}

.actionButton {
  width: fit-content;
}
```

### `<ComponentName>.test.tsx`

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import <ComponentName> from './<ComponentName>';

describe('<ComponentName>', () => {
  it('renders title', () => {
    render(<<ComponentName> title="Example" />);

    expect(screen.getByRole('heading', { name: 'Example' })).toBeInTheDocument();
  });

  it('calls action handler', async () => {
    const user = userEvent.setup();
    const onAction = vi.fn();

    render(<<ComponentName> title="Example" onAction={onAction} />);

    await user.click(screen.getByRole('button', { name: 'Action' }));
    expect(onAction).toHaveBeenCalledTimes(1);
  });
});
```

### `index.ts`

```ts
export { default as <ComponentName> } from './<ComponentName>';
```

### `<ComponentName>.types.ts` (optional)

```ts
export interface <ComponentName>ViewModel {
  id: string;
  title: string;
}
```

### `<ComponentName>.hooks.ts` (optional)

```ts
export function use<ComponentName>State() {
  return { isReady: true };
}
```

### `<ComponentName>.utils.ts` (optional)

```ts
export function format<ComponentName>Label(value: string): string {
  return value.trim();
}
```

## Import Pattern

```ts
import { <ComponentName> } from '@/features/<feature>/components/<ComponentName>';
```
