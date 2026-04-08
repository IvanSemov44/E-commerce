# Prompt: Frontend Component / Hook Test

Use this prompt when adding or changing a React component or custom hook.

---

```
You are writing Vitest tests for a React component or custom hook in this storefront application.

## Stack
- Vitest 3 + jsdom
- @testing-library/react 16 — always import from the custom test-utils, not from the library directly
- Import path: import { renderWithProviders, renderHookWithProviders, screen, waitFor } from '@/shared/lib/test/test-utils';
- userEvent: import userEvent from '@testing-library/user-event';
- MSW v2 for HTTP interception: import { server } from '@/shared/lib/test/msw-server'; import { http, HttpResponse } from 'msw';

## API mocking — use MSW, not vi.mock

DO NOT vi.mock RTK Query hooks. That replaces the hook entirely and tests nothing real.
USE MSW to intercept HTTP at the network level. The real RTK Query hook fires.

// WRONG
vi.mock('@/features/cart/api/cartApi', () => ({
    useGetCartQuery: () => ({ data: mockCart, isLoading: false }),
}));

// RIGHT
server.use(
    http.get('/api/cart', () => HttpResponse.json({ success: true, data: mockCart }))
);

The server resets after each test (afterEach(() => server.resetHandlers()) in setup.ts).
Per-test overrides use server.use() inside the test body.

## renderWithProviders signature
renderWithProviders(ui, {
    preloadedState?: { auth?, cart?, toast?, [baseApi.reducerPath]? },
    withRouter?: boolean,    // default true (uses MemoryRouter)
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

## Test structure

describe('<ComponentName>', () => {
    describe('rendering', () => {
        it('renders_WithDefaultProps_<WhatIsVisible>', () => {
            // Arrange
            renderWithProviders(<Component prop="value" />);
            // Act — (nothing; render is the act when testing initial render)
            // Assert
            expect(screen.getByText('Expected text')).toBeInTheDocument();
        });

        it('renders_WhenLoading_ShowsSkeleton', async () => {
            // Arrange — MSW delays response so loading state is visible
            server.use(
                http.get('/api/products', async () => {
                    await delay(100);
                    return HttpResponse.json({ success: true, data: [] });
                })
            );
            renderWithProviders(<Component />);
            // Assert — skeleton visible before data arrives
            expect(screen.getByTestId('skeleton')).toBeInTheDocument();
        });
    });

    describe('<action>', () => {
        it('click_<Action>_<ExpectedEffect>', async () => {
            // Arrange
            server.use(
                http.post('/api/<endpoint>', () =>
                    HttpResponse.json({ success: true, data: { id: '1' } })
                )
            );
            renderWithProviders(<Component productId="1" />);

            // Act
            await userEvent.click(screen.getByRole('button', { name: /add to cart/i }));

            // Assert — verify what the USER sees, not internal calls
            await waitFor(() =>
                expect(screen.getByText('Added to cart')).toBeInTheDocument()
            );
        });
    });
});

## Required test scenarios (generate all that apply)

For COMPONENTS:
  1. Default render — shows expected UI elements
  2. Loading state — shows skeleton/spinner while MSW response is delayed
  3. Empty state — shows empty message when API returns empty array
  4. Error state — shows error UI when MSW returns 4xx/5xx
  5. User interaction — each interactive element produces the right visible outcome
  6. Authenticated vs unauthenticated — if component behaves differently (use preloadedState.auth)

For HOOKS:
  1. Initial state — returns correct default values
  2. State after action — calling the hook's returned functions updates state correctly
  3. Error path — hook handles API failure gracefully (MSW returns error)

## Selector priority (use in this order)
  1. getByRole — getByRole('button', { name: /text/i })
  2. getByLabelText — for form inputs
  3. getByText — for visible text
  4. getByTestId — only when no semantic query works

## Rules
- Never vi.mock RTK Query hooks — use MSW
- Never use document.querySelector — always use Testing Library queries
- userEvent over fireEvent for user interactions
- waitFor only for async state updates after user actions
- Inject Redux slice state via preloadedState, not by dispatching actions

## After writing
Run: npm run test:run -- <ComponentName>.test.tsx
All tests must PASS.

---

## STEP 1 — Extract before generating (mandatory)

Before writing any test, read the pasted component/hook and list:
- Every prop (name, type, required/optional)
- Every conditional render path (loading, empty, error, authenticated)
- Every user interaction (button click, form submit, input change)
- Every API call made: HTTP verb + exact URL path (needed for MSW handlers)

If you cannot find the API base URL or endpoint path in the pasted code, write "MISSING: API path"
and ask the user to paste the baseApi configuration or endpoint definition.

## NEVER do these
- Do NOT vi.mock RTK Query hooks — use MSW server.use() instead
- Do NOT use document.querySelector — use Testing Library queries
- Do NOT use fireEvent — use userEvent
- Do NOT assert on internal function calls — assert on visible UI state
- Do NOT add XML doc comments or helper classes not in the template
- Do NOT invent prop names or state shape — use only what is in the pasted code

## Component / hook to test

[PASTE THE COMPONENT OR HOOK SOURCE HERE]

[PASTE RELEVANT TYPE DEFINITIONS AND SLICE STATE SHAPE HERE]

[PASTE THE RTK QUERY ENDPOINT DEFINITION — so MSW handlers use the correct URL and response shape]
```
