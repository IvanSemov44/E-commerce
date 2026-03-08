# E2E Tests with Playwright

This directory contains end-to-end tests for the storefront application using Playwright.

## Test Structure

- **`product-browsing.spec.ts`** - Tests for product listing, detail pages, search, and filtering
- **`cart.spec.ts`** - Tests for shopping cart operations (add, remove, update quantity)
- **`checkout-guest.spec.ts`** - Tests for guest checkout flow
- **`auth.spec.ts`** - Tests for login, registration, and authentication

## Running Tests

### Prerequisites

1. Install dependencies:

   ```bash
   npm install
   ```

2. Install Playwright browsers (if not already done):
   ```bash
   npx playwright install
   ```

### Commands

```bash
# Run all E2E tests
npm run test:e2e

# Run tests in UI mode (interactive)
npm run test:e2e:ui

# Run tests in debug mode
npm run test:e2e:debug

# View test report after run
npm run test:e2e:report

# Run specific test file
npx playwright test e2e/product-browsing.spec.ts

# Run tests in specific browser
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit
```

## Test Configuration

Configuration is in `playwright.config.ts`:

- Tests run against `http://localhost:5173` (dev server)
- Dev server starts automatically before tests
- Screenshots on failure
- Video recording on failure
- HTML report generated after tests

## Writing Tests

Tests use Playwright's flexible selectors that work with various implementations:

```typescript
// Multiple selector strategies for resilience
const button = page.locator(
  'button:has-text("Add to Cart"), ' + // Text-based
    '[data-testid="add-to-cart"], ' + // Test ID (preferred)
    'button:has-text("Add to Bag")' // Alternative text
);
```

This approach ensures tests work even if:

- Component names change
- CSS classes change
- Exact text changes slightly

## Best Practices

1. **Use test IDs when possible** (`data-testid` attributes)
2. **Have fallback selectors** (text, class, etc.)
3. **Wait for elements** before interacting
4. **Handle timeouts gracefully** with conditional logic
5. **Test critical paths** rather than every edge case

## CI/CD Integration

These tests can run in CI pipelines:

```yaml
# Example GitHub Actions
- name: Install Playwright
  run: npx playwright install --with-deps chromium

- name: Run E2E tests
  run: npm run test:e2e
  env:
    CI: true
```

## Troubleshooting

### Tests failing locally

1. Ensure dev server is running or use `npm run test:e2e` (auto-starts server)
2. Check API backend is accessible
3. Clear browser data if needed

### Flaky tests

1. Increase timeouts for slow operations
2. Add explicit waits: `await page.waitForSelector(...)`
3. Use `test.retry()` for network-dependent tests

### Screenshots and videos

Find test artifacts in:

- `test-results/` - Screenshots and videos
- `playwright-report/` - HTML report
