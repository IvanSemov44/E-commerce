# Pattern: Frontend Unit Tests (Components, Hooks, Slices)

Layers F1, F2, F3. Vitest + jsdom + @testing-library/react. Co-located with source files.

---

## Stack

| Tool | Purpose |
|---|---|
| Vitest 3 | Test runner, assertions |
| jsdom | DOM environment |
| @testing-library/react 16 | `render`, `screen`, `userEvent`, `waitFor` |
| `renderWithProviders` | Custom render with Redux + MemoryRouter wrapper |
| `renderHookWithProviders` | Custom renderHook with Redux wrapper |
| **MSW v2** | Network-level HTTP interception — replaces `vi.mock` for API layer |

Helpers live in `src/shared/lib/test/`. Always import from there.

```tsx
import { renderWithProviders, screen, waitFor } from '@/shared/lib/test/test-utils';
import userEvent from '@testing-library/user-event';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';
```

---

## Why MSW instead of vi.mock

`vi.mock` replaces the RTK Query hook entirely. The test asserts against a fake with no relation to the real hook — different caching, different loading states, different error shapes. The component can be completely broken and the test passes.

MSW intercepts at the network level. The real RTK Query hook fires, the real cache updates, the real loading state transitions. Your test exercises actual component behaviour.

```ts
// WRONG — mocking the hook mocks away everything interesting
vi.mock('@/features/cart/api/cartApi', () => ({
    useRemoveFromCartMutation: () => [vi.fn(), { isLoading: false }],
}));

// RIGHT — MSW: real hook, real cache, real loading state
server.use(
    http.delete('/api/cart/:itemId', () => HttpResponse.json({ success: true }))
);
```

---

## MSW server setup

The server is created once in `src/shared/lib/test/msw-server.ts` and lifecycle hooks live in `setup.ts`.

```ts
// src/shared/lib/test/msw-server.ts
import { setupServer } from 'msw/node';
export const server = setupServer();
```

```ts
// src/shared/lib/test/setup.ts (add these lines)
import { server } from './msw-server';

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

`onUnhandledRequest: 'error'` fails the test if your component makes a request you forgot to handle — catches forgotten mocks immediately.

---

## Component test template

```tsx
// src/features/cart/components/CartItem/CartItem.test.tsx

import { renderWithProviders, screen, waitFor } from '@/shared/lib/test/test-utils';
import userEvent from '@testing-library/user-event';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';
import { CartItem } from './CartItem';

const defaultProps = {
    id: '1',
    productName: 'Widget Pro',
    quantity: 2,
    unitPrice: 29.99,
};

describe('CartItem', () => {
    describe('rendering', () => {
        it('renders_WithDefaultProps_ShowsProductNameAndTotal', () => {
            // Arrange
            renderWithProviders(<CartItem {...defaultProps} />);

            // Act — (nothing; render is the act)

            // Assert
            expect(screen.getByText('Widget Pro')).toBeInTheDocument();
            expect(screen.getByText('$59.98')).toBeInTheDocument(); // 2 × 29.99
        });

        it('renders_WhenQuantityIsOne_DecreaseButtonIsDisabled', () => {
            // Arrange
            renderWithProviders(<CartItem {...defaultProps} quantity={1} />);

            // Assert
            expect(screen.getByRole('button', { name: /decrease/i })).toBeDisabled();
        });
    });

    describe('remove', () => {
        it('click_Remove_RemovesItemFromCart', async () => {
            // Arrange
            server.use(
                http.delete('/api/cart/1', () => HttpResponse.json({ success: true }))
            );
            renderWithProviders(<CartItem {...defaultProps} />);

            // Act
            await userEvent.click(screen.getByRole('button', { name: /remove/i }));

            // Assert — item disappears from the cart
            await waitFor(() =>
                expect(screen.queryByText('Widget Pro')).not.toBeInTheDocument()
            );
        });

        it('click_Remove_WhenApiErrors_ShowsErrorMessage', async () => {
            // Arrange
            server.use(
                http.delete('/api/cart/1', () =>
                    HttpResponse.json({ success: false }, { status: 500 })
                )
            );
            renderWithProviders(<CartItem {...defaultProps} />);

            // Act
            await userEvent.click(screen.getByRole('button', { name: /remove/i }));

            // Assert
            await waitFor(() =>
                expect(screen.getByRole('alert')).toBeInTheDocument()
            );
        });
    });
});
```

---

## Hook test template

For hooks that use RTK Query, inject data via MSW. For hooks that use only Redux slice state, inject via `preloadedState`.

```tsx
// src/features/cart/hooks/useCartSummary.test.ts

import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { useCartSummary } from './useCartSummary';

describe('useCartSummary', () => {
    it('returnsZeroTotals_WhenCartIsEmpty', () => {
        // Arrange + Act
        const { result } = renderHookWithProviders(() => useCartSummary(), {
            preloadedState: {
                cart: { items: [], couponCode: null },
            },
        });

        // Assert
        expect(result.current.totalItems).toBe(0);
        expect(result.current.totalPrice).toBe(0);
    });

    it('calculatesCorrectTotal_WithMultipleItems', () => {
        // Arrange
        const { result } = renderHookWithProviders(() => useCartSummary(), {
            preloadedState: {
                cart: {
                    items: [
                        { id: '1', productId: 'p1', quantity: 2, unitPrice: 10 },
                        { id: '2', productId: 'p2', quantity: 1, unitPrice: 25 },
                    ],
                },
            },
        });

        // Assert
        expect(result.current.totalItems).toBe(3);
        expect(result.current.totalPrice).toBe(45);
    });
});
```

---

## Slice / selector test template

Slice tests have no DOM. Pure input/output. No MSW needed — no HTTP involved.

```ts
// src/features/cart/slices/cartSlice.test.ts

import { cartReducer, addItem, removeItem, clearCart } from './cartSlice';
import type { CartState } from './cartSlice';

const initialState: CartState = { items: [], couponCode: null };

describe('cartSlice', () => {
    describe('addItem', () => {
        it('addsNewItem_WhenProductNotInCart', () => {
            // Arrange
            const action = addItem({ productId: 'p1', quantity: 1, unitPrice: 10 });

            // Act
            const state = cartReducer(initialState, action);

            // Assert
            expect(state.items).toHaveLength(1);
            expect(state.items[0].productId).toBe('p1');
        });

        it('incrementsQuantity_WhenProductAlreadyInCart', () => {
            // Arrange
            const existing: CartState = {
                items: [{ id: '1', productId: 'p1', quantity: 2, unitPrice: 10 }],
                couponCode: null,
            };
            const action = addItem({ productId: 'p1', quantity: 1, unitPrice: 10 });

            // Act
            const state = cartReducer(existing, action);

            // Assert
            expect(state.items).toHaveLength(1);
            expect(state.items[0].quantity).toBe(3);
        });
    });

    describe('clearCart', () => {
        it('emptiesAllItems', () => {
            // Arrange
            const existing: CartState = {
                items: [{ id: '1', productId: 'p1', quantity: 1, unitPrice: 10 }],
                couponCode: 'SAVE10',
            };

            // Act
            const state = cartReducer(existing, clearCart());

            // Assert
            expect(state.items).toHaveLength(0);
            expect(state.couponCode).toBeNull();
        });
    });
});
```

---

## Rules

1. **Import from `test-utils.tsx`**, not from `@testing-library/react` directly.

2. **Use MSW for any component/hook that fetches data.** Do not `vi.mock` RTK Query hooks. Exception: slice tests and pure computation hooks that never touch the network.

3. **`onUnhandledRequest: 'error'` in MSW setup** — any unhandled request fails the test. This prevents silent pass-throughs.

4. **Inject slice state via `preloadedState`**, not by dispatching actions in setup.

5. **Use `MemoryRouter` in test-utils**, not `BrowserRouter`. `BrowserRouter` interacts with the real browser history API and causes subtle leaks between tests in jsdom. The `renderWithProviders` wrapper should use `MemoryRouter` from `react-router`.

6. **Use `data-testid` sparingly.** Prefer semantic queries:
   ```tsx
   // GOOD — accessible roles
   screen.getByRole('button', { name: /add to cart/i })
   screen.getByLabelText('Email address')
   screen.getByText('Widget Pro')
   // OK — when no semantic query works
   screen.getByTestId('cart-count')
   // BAD
   document.querySelector('.cart-btn')
   ```

7. **`userEvent` over `fireEvent`** — `userEvent` simulates real browser behaviour (focus, keyboard, pointer events).

8. **`waitFor` only for async state updates** — after user actions that trigger network calls. Do not use it to patch timing issues.
