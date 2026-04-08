# Prompt: Frontend Component / Hook Test

Use this prompt when adding or changing a React component or custom hook.

---

```
You are writing Vitest tests for a React component or custom hook in this storefront application.

## Stack
- Vitest + jsdom
- @testing-library/react — always import from the custom test-utils, not from the library directly
- Import path: import { renderWithProviders, renderHookWithProviders, screen, waitFor } from '@/shared/lib/test/test-utils';
- userEvent: import userEvent from '@testing-library/user-event';

## renderWithProviders signature
renderWithProviders(ui, {
    preloadedState?: { auth?, cart?, toast?, [baseApi.reducerPath]? },
    withRouter?: boolean,    // default true
    withRedux?: boolean,     // default true
})
Returns: { store, ...renderResult }

## renderHookWithProviders signature
renderHookWithProviders(hook, {
    preloadedState?,
    withRouter?: boolean,    // default false
})
Returns: { store, result, rerender, ... }

## File location (co-located with source)
Component: src/features/<feature>/components/<Name>/<Name>.tsx
Test file: src/features/<feature>/components/<Name>/<Name>.test.tsx

Hook: src/features/<feature>/hooks/<hookName>.ts
Test file: src/features/<feature>/hooks/<hookName>.test.ts

## API mocking (mandatory — never make real HTTP)
Use vi.mock to mock RTK Query hooks or the entire api module:

vi.mock('@/features/cart/api/cartApi', () => ({
    useGetCartQuery: () => ({ data: mockCart, isLoading: false, isError: false }),
    useAddToCartMutation: () => [vi.fn().mockResolvedValue({ data: {} }), { isLoading: false }],
}));

## Test structure

describe('<ComponentName>', () => {
    it('renders_WithDefaultProps_<WhatIsVisible>', () => {
        // Arrange
        renderWithProviders(<Component prop="value" />);
        // Act — (nothing; render is the act when testing initial render)
        // Assert
        expect(screen.getByText('Expected text')).toBeInTheDocument();
    });

    it('renders_WhenLoading_ShowsSkeleton', () => {
        // Arrange
        vi.mocked(useGetProductsQuery).mockReturnValue({ isLoading: true, data: undefined });
        renderWithProviders(<Component />);
        // Assert
        expect(screen.getByTestId('skeleton')).toBeInTheDocument();
    });

    it('click_<Action>_<ExpectedEffect>', async () => {
        // Arrange
        const mutationFn = vi.fn().mockResolvedValue({ data: {} });
        vi.mocked(useAddToCartMutation).mockReturnValue([mutationFn, { isLoading: false }]);
        renderWithProviders(<Component productId="1" />);

        // Act
        await userEvent.click(screen.getByRole('button', { name: /add to cart/i }));

        // Assert
        expect(mutationFn).toHaveBeenCalledWith({ productId: '1', quantity: 1 });
    });
});

## Required test scenarios (generate all that apply)

For COMPONENTS:
  1. Default render — shows expected UI elements
  2. Loading state — shows skeleton/spinner (if component has loading state)
  3. Empty state — shows empty message (if component can be empty)
  4. Error state — shows error UI (if component handles errors)
  5. User interaction — each interactive element dispatches the right action
  6. Authenticated vs unauthenticated — if component behaves differently

For HOOKS:
  1. Initial state — returns correct default values
  2. State after action — calling the hook's returned functions updates state correctly
  3. Error path — hook handles API failure gracefully

## Selector priority (use in this order)
  1. getByRole — getByRole('button', { name: /text/i })
  2. getByLabelText — for form inputs
  3. getByText — for visible text
  4. getByTestId — only when no semantic query works

## Rules
- Never use document.querySelector — always use Testing Library queries
- userEvent over fireEvent for user interactions
- waitFor only for async state updates after user actions
- Inject initial state via preloadedState, not by dispatching actions

## After writing
Run: npm run test:run -- <ComponentName>.test.tsx
All tests must PASS.

---

## Component / hook to test

[PASTE THE COMPONENT OR HOOK SOURCE HERE]

[PASTE RELEVANT TYPE DEFINITIONS AND SLICE STATE SHAPE HERE]
```
