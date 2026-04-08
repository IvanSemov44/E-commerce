# Prompt: Frontend Hook Test

Use this prompt when adding or changing a custom hook in `features/*/hooks/` or `shared/hooks/`.

---

```
You are writing Vitest tests for a custom React hook in this storefront application.

## Stack
- Vitest 3 + jsdom
- @testing-library/react 16 — always use the custom renderHookWithProviders, not bare renderHook
- Import path: import { renderHookWithProviders, waitFor } from '@/shared/lib/test/test-utils';
- MSW v2 for HTTP interception: import { server } from '@/shared/lib/test/msw-server'; import { http, HttpResponse } from 'msw';
- userEvent is NOT used in hook tests (no DOM to interact with)

## API mocking — use MSW, not vi.mock

DO NOT vi.mock RTK Query hooks or the baseApi. Use MSW to intercept HTTP at the network level.
The real RTK Query hook fires — you test real caching, real loading state transitions.

// WRONG
vi.mock('@/features/cart/api/cartApi', () => ({
    useGetCartQuery: () => ({ data: mockCart, isLoading: false }),
}));

// RIGHT
server.use(
    http.get('/api/cart', () => HttpResponse.json({ success: true, data: mockCart }))
);

The server resets after each test (afterEach(() => server.resetHandlers()) in setup.ts).

## renderHookWithProviders signature
renderHookWithProviders(hook, {
    preloadedState?: { auth?, cart?, toast?, [baseApi.reducerPath]? },
    withRouter?: boolean,    // default false — set true only if hook uses useNavigate/useParams
})
Returns: { store, result, rerender, unmount, ... }

## File location
Hook: src/features/<feature>/hooks/<hookName>.ts  OR  src/shared/hooks/<hookName>.ts
Test: same directory, <hookName>.test.ts

## Test structure

describe('<hookName>', () => {
    describe('initialState', () => {
        it('returnsDefaultValues_WhenNothingLoaded', () => {
            // Arrange — no MSW handler needed for pure-logic hooks
            const { result } = renderHookWithProviders(() => useMyHook());
            // Assert
            expect(result.current.items).toEqual([]);
            expect(result.current.isLoading).toBe(false);
        });
    });

    describe('fetchesData', () => {
        it('populatesState_WhenApiSucceeds', async () => {
            // Arrange
            server.use(
                http.get('/api/items', () =>
                    HttpResponse.json({ success: true, data: [{ id: '1', name: 'Item' }] })
                )
            );
            const { result } = renderHookWithProviders(() => useMyHook());
            // Assert — wait for async state update
            await waitFor(() => expect(result.current.items).toHaveLength(1));
        });

        it('setsError_WhenApiFails', async () => {
            // Arrange
            server.use(
                http.get('/api/items', () => HttpResponse.json({ success: false }, { status: 500 }))
            );
            const { result } = renderHookWithProviders(() => useMyHook());
            // Assert
            await waitFor(() => expect(result.current.error).toBeTruthy());
        });
    });

    describe('withPreloadedState', () => {
        it('readsSliceState_WhenPreloaded', () => {
            // Arrange — inject Redux state directly without dispatching actions
            const { result } = renderHookWithProviders(() => useMyHook(), {
                preloadedState: { auth: { user: { id: '1', email: 'a@b.com' }, isAuthenticated: true, loading: false } },
            });
            // Assert
            expect(result.current.isAuthenticated).toBe(true);
        });
    });
});

## Required test scenarios (generate all that apply)

1. Initial state — what does the hook return before any async work completes?
2. Successful data fetch — MSW returns 200, assert hook state reflects the data
3. Error path — MSW returns 4xx/5xx, assert hook exposes an error value
4. Loading state — assert isLoading is true while MSW response is pending (use delay())
5. State after action — calling a function returned by the hook updates state correctly
6. Slice-dependent state — if hook reads Redux slice, inject via preloadedState

For pure logic hooks (no API calls, no Redux):
- Test each branch of the logic directly via result.current
- No MSW needed; no preloadedState needed

## Rules
- Never vi.mock RTK Query hooks — use MSW
- Use renderHookWithProviders, not bare renderHook (needs Redux store for RTK Query)
- Use waitFor for any assertion that depends on async state (RTK Query responses)
- Inject Redux state via preloadedState, not by dispatching actions in the test
- withRouter: true only when the hook explicitly calls useNavigate, useParams, or useLocation

## Naming convention
MethodOrScenario_ExpectedOutcome
Examples:
  fetchesOnMount_PopulatesItems
  apiFailure_ExposesErrorState
  addItem_IncrementsCount
  withAuthUser_ReturnsUserId

## After writing
Run: npm run test:run -- <hookName>.test.ts
All tests must PASS.

---

## STEP 1 — Extract before generating (mandatory)

Before writing any test, read the pasted hook and list:
- Every value returned (name, type, initial value)
- Every function returned (name, what it does, what it calls)
- Every API call made: HTTP verb + exact URL path (needed for MSW handlers)
- Every Redux slice the hook reads from (needed for preloadedState shape)
- Whether the hook uses useNavigate / useParams / useLocation (determines withRouter)

If you cannot find the API base URL or endpoint path, write "MISSING: API path"
and ask the user to paste the baseApi or endpoint definition.

## NEVER do these
- Do NOT vi.mock RTK Query hooks or baseApi — use MSW server.use() instead
- Do NOT use bare renderHook — always use renderHookWithProviders
- Do NOT dispatch Redux actions to set up state — use preloadedState
- Do NOT assert on internal function calls (vi.fn()) — assert on returned state values
- Do NOT add XML doc comments or helper classes not in the template
- Do NOT invent return values or state shape — use only what is in the pasted hook

## Hook to test

[PASTE THE HOOK SOURCE HERE]

[PASTE RELEVANT TYPE DEFINITIONS AND SLICE STATE SHAPE HERE]

[PASTE THE RTK QUERY ENDPOINT DEFINITION — so MSW handlers use the correct URL and response shape]
```
