# Prompt: Frontend E2E Test (Playwright)

Use this prompt when adding or changing a user-facing flow or API contract.

---

```
You are writing Playwright tests for a user journey or API contract in this storefront application.

## Two modes — specify which you need

MODE A: UI flow test (spec file: e2e/<feature>.spec.ts)
  - Drives a real browser through user interactions
  - Uses Page Object Model — NO inline selectors in spec files
  - Requires: running backend on :5000 + frontend dev server on :5173

MODE B: API contract test (spec file: e2e/api-<feature>.spec.ts)
  - Hits the backend REST API directly using Playwright's request API
  - No browser, no UI — just HTTP assertions
  - Faster, more stable than UI tests
  - Requires: running backend on :5000

## File locations

UI flows:   src/frontend/storefront/e2e/<feature>.spec.ts
API tests:  src/frontend/storefront/e2e/api-<feature>.spec.ts
Page objects: src/frontend/storefront/e2e/pages/<FeatureName>Page.ts

## Page Object template (create one if it does not exist)

// e2e/pages/<Feature>Page.ts
import type { Page, Locator } from '@playwright/test';
import { expect } from '@playwright/test';

export class <Feature>Page {
    private readonly page: Page;

    // Define all locators here — NO locators in spec files
    private readonly <element>: Locator;

    constructor(page: Page) {
        this.page = page;
        this.<element> = page.getByRole('<role>', { name: /<name>/i });
    }

    async goto() {
        await this.page.goto('/<route>');
        await this.page.waitForLoadState('networkidle');
    }

    async <action>() {
        await this.<element>.click();
        await this.page.waitForLoadState('networkidle');
    }

    async expect<State>() {
        await expect(this.<element>).toBeVisible();
    }
}

## UI flow spec template

// e2e/<feature>.spec.ts
import { test, expect } from '@playwright/test';
import { <Feature>Page } from './pages/<Feature>Page';

test.describe('<Feature> flow', () => {
    test('user_Can<Action>_And<Outcome>', async ({ page }) => {
        // Arrange
        const featurePage = new <Feature>Page(page);
        await featurePage.goto();

        // Act
        await featurePage.<action>();

        // Assert
        await featurePage.expect<Outcome>();
    });

    test('guest_Cannot<ProtectedAction>_WithoutLogin', async ({ page }) => {
        // Arrange
        const featurePage = new <Feature>Page(page);

        // Act
        await featurePage.goto();
        await featurePage.<protectedAction>();

        // Assert — redirect to login
        await expect(page).toHaveURL(/\/login/);
    });
});

## API contract spec template

// e2e/api-<feature>.spec.ts
import { test, expect } from '@playwright/test';

const API = 'http://localhost:5000/api';

test.describe('<Feature> API', () => {
    let token: string;

    test.beforeAll(async ({ request }) => {
        const res = await request.post(`${API}/auth/login`, {
            data: { email: 'integration@test.com', password: 'TestPassword123!' },
        });
        token = (await res.json()).data.token;
    });

    test('GET_/<endpoint>_Returns200_WhenAuthenticated', async ({ request }) => {
        // Act
        const res = await request.get(`${API}/<endpoint>`, {
            headers: { Authorization: `Bearer ${token}` },
        });

        // Assert
        expect(res.status()).toBe(200);
        const body = await res.json();
        expect(body.success).toBe(true);
        expect(body.data).toBeDefined();
    });

    test('GET_/<endpoint>_Returns401_WhenUnauthenticated', async ({ request }) => {
        const res = await request.get(`${API}/<endpoint>`);
        expect(res.status()).toBe(401);
    });

    test('POST_/<endpoint>_AddsResource_Returns201', async ({ request }) => {
        // Arrange
        const payload = { /* required fields */ };

        // Act
        const res = await request.post(`${API}/<endpoint>`, {
            data: payload,
            headers: { Authorization: `Bearer ${token}` },
        });

        // Assert
        expect(res.status()).toBe(201);
        const body = await res.json();
        expect(body.success).toBe(true);
        expect(body.data.id).toBeDefined();
    });
});

## STEP 1 — Extract before generating (mandatory)

Before writing any test, list:
- Every user action in the described flow (click, fill, navigate)
- Every expected visible outcome (text, URL change, element state)
- Every API endpoint called during the flow (verb + path) — needed for API contract tests
- Every role involved (guest, authenticated user, admin)

If you cannot determine an item from the pasted components or routes, write "MISSING: [item]".

## NEVER do these
- Do NOT write inline selectors in spec files — all selectors belong in Page Objects
- Do NOT reuse Page Object instances across tests (each test gets a fresh page)
- Do NOT hardcode UUIDs or IDs — use constants from e2e/data/test-data.ts
- Do NOT write E2E tests for edge cases — those belong in unit/integration tests
- Do NOT add XML doc comments

## Rules

1. ALL element selectors belong in Page Object classes. Spec files ONLY call Page Object methods.
2. waitForLoadState('networkidle') after navigation in UI tests.
3. Use request fixture for API tests — no browser needed.
4. Tests must be independent — use beforeAll for shared auth setup, not inter-test state.
5. No hard-coded GUIDs in spec files — use constants from e2e/data/test-data.ts.
6. Keep E2E tests to critical happy paths — edge cases belong in unit/integration tests.

## After writing
Run UI tests:  npm run test:e2e -- <feature>.spec.ts
Run API tests: npm run test:e2e -- api-<feature>.spec.ts
All tests must PASS.

---

## Flow to test

[DESCRIBE THE USER JOURNEY OR API ENDPOINTS TO COVER]

[PASTE RELEVANT PAGE COMPONENTS OR API ROUTE DEFINITIONS FOR CONTEXT]

[PASTE EXISTING PAGE OBJECTS IF THEY EXIST — so you extend rather than duplicate]
```
