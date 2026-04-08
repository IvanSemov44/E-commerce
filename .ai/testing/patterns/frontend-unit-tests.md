# Pattern: Frontend Unit Tests (Components, Hooks, Slices)

Layers F1, F2, F3. Vitest + jsdom + @testing-library/react. Co-located with source files.

---

## Stack

| Tool | Purpose |
|---|---|
| Vitest | Test runner, assertions |
| jsdom | DOM environment |
| @testing-library/react | `render`, `screen`, `userEvent`, `waitFor` |
| `renderWithProviders` | Custom render with Redux + Router wrapper |
| `renderHookWithProviders` | Custom renderHook with Redux + Router wrapper |
| `vi.mock` | Mock modules (RTK Query endpoints, utilities) |

Helpers are in `src/shared/lib/test/test-utils.tsx`. Always import from there, not from `@testing-library/react` directly.

```tsx
import { renderWithProviders, screen, userEvent } from '@/shared/lib/test/test-utils';
```

---

## Component test template

```tsx
// src/features/cart/components/CartItem/CartItem.test.tsx

import { renderWithProviders, screen } from '@/shared/lib/test/test-utils';
import userEvent from '@testing-library/user-event';
import { CartItem } from './CartItem';

// Mock RTK Query hooks — never make real HTTP calls
vi.mock('@/features/cart/api/cartApi', () => ({
    useRemoveFromCartMutation: () => [vi.fn(), { isLoading: false }],
    useUpdateCartItemMutation: () => [vi.fn(), { isLoading: false }],
}));

describe('CartItem', () => {
    const defaultProps = {
        id: '1',
        productName: 'Widget Pro',
        quantity: 2,
        unitPrice: 29.99,
    };

    it('renders_WithDefaultProps_ShowsProductNameAndTotal', () => {
        // Arrange
        renderWithProviders(<CartItem {...defaultProps} />);

        // Act — (nothing; render is the act)

        // Assert
        expect(screen.getByText('Widget Pro')).toBeInTheDocument();
        expect(screen.getByText('$59.98')).toBeInTheDocument(); // 2 × 29.99
    });

    it('renders_WhenQuantityIsOne_DoesNotShowDecrement', () => {
        // Arrange
        renderWithProviders(<CartItem {...defaultProps} quantity={1} />);

        // Assert
        expect(screen.getByRole('button', { name: /decrease/i })).toBeDisabled();
    });

    it('click_Remove_CallsRemoveMutation', async () => {
        // Arrange
        const removeFn = vi.fn();
        vi.mocked(useRemoveFromCartMutation).mockReturnValue([removeFn, { isLoading: false }]);
        renderWithProviders(<CartItem {...defaultProps} />);

        // Act
        await userEvent.click(screen.getByRole('button', { name: /remove/i }));

        // Assert
        expect(removeFn).toHaveBeenCalledWith({ itemId: '1' });
    });

    it('renders_WhenLoading_DisablesButtons', () => {
        // Arrange
        vi.mocked(useRemoveFromCartMutation).mockReturnValue([vi.fn(), { isLoading: true }]);
        renderWithProviders(<CartItem {...defaultProps} />);

        // Assert
        expect(screen.getByRole('button', { name: /remove/i })).toBeDisabled();
    });
});
```

---

## Hook test template

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

1. **Import from `test-utils.tsx`**, not from `@testing-library/react` directly:
   ```tsx
   // GOOD
   import { renderWithProviders, screen } from '@/shared/lib/test/test-utils';
   // BAD
   import { render, screen } from '@testing-library/react';
   ```

2. **Never make real HTTP calls.** Mock every RTK Query hook with `vi.mock`. Do not test that the API returns data — test that the component renders correctly given a specific state.

3. **Inject state via `preloadedState`**, not by dispatching actions in the test setup. Tests should declare their starting state, not construct it through side effects.

4. **Use `data-testid` sparingly.** Prefer semantic queries:
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

5. **`userEvent` over `fireEvent`** for interactions — `userEvent` simulates real browser behaviour (focus, keyboard events, pointer events).

6. **`waitFor` only when needed** — for async side effects (API calls that update state). Do not use it to avoid fixing timing issues.
