# 100% Test Coverage Plan for E-Commerce Project

## Executive Summary

This plan outlines a comprehensive strategy to achieve 100% line coverage across all three parts of the E-commerce application:
- **Backend** (.NET 10 with C#)
- **Frontend Storefront** (React 19 + TypeScript + Vite)
- **Frontend Admin** (React 19 + TypeScript + Vite)

## Current State Analysis

### Backend Current Coverage
The backend has an established testing foundation with 36 test classes:

| Category | Test Files | Status |
|----------|-----------|--------|
| Services | 14 test files | Partial coverage |
| Controllers/Integration | 14 test files | Good coverage |
| Validators | 6 test files | Good coverage |
| Middleware | 1 test file | Partial |
| ActionFilters | 1 test file | Covered |
| Mappings | 1 test file | Covered |

**Missing Areas:**
- Infrastructure layer (Repositories, UnitOfWork, Extensions)
- Email services (SendGridEmailService, SmtpEmailService)
- Health checks (MemoryHealthCheck, HealthCheckResponseWriter)
- Middleware (CsrfMiddleware, SecurityHeadersMiddleware)
- Configuration classes and extensions

### Frontend Storefront Current Coverage
Very limited test coverage:

| Category | Files | Tests Present |
|----------|-------|---------------|
| Components | 25+ | 1 (ErrorBoundary) |
| Hooks | 13 | 1 (useCart) |
| Pages | 14 | 0 |
| Store/Slices | 4 | 1 (cartSlice) |
| Store/API | 10 | 0 |
| Utils | 1 | 0 |

### Frontend Admin Current Coverage
Basic test coverage present:

| Category | Files | Tests Present |
|----------|-------|---------------|
| Components/UI | 8 | 4 (Button, Input, Modal, ProtectedRoute) |
| Hooks | 2 | 1 (useForm) |
| Pages | 11 | 0 |
| Store/Slices | 2 | 2 (authSlice, toastSlice) |
| Store/API | 8 | 0 |
| Utils | 2 | 1 (validation) |

---

## Detailed Implementation Plan

### Phase 1: Backend Infrastructure Tests

#### 1.1 Repository Tests
Create `Unit/Repositories/` directory with tests for:

- [ ] **RepositoryTests.cs** - Generic repository CRUD operations
  - Test all methods: GetByIdAsync, GetAllAsync, FindAll, FindByCondition
  - Test Add, AddAsync, AddRange, AddRangeAsync
  - Test Update, UpdateRange
  - Test Delete, DeleteRange
  - Test tracking vs no-tracking queries

- [ ] **ProductRepositoryTests.cs** - Product-specific queries
  - Test GetProductsWithCategoryAsync
  - Test GetProductBySlugAsync
  - Test GetProductsByCategoryAsync
  - Test SearchProductsAsync

- [ ] **OrderRepositoryTests.cs** - Order-specific queries
  - Test GetOrdersWithItemsAsync
  - Test GetOrderByNumberAsync
  - Test GetOrdersByUserAsync

- [ ] **CartRepositoryTests.cs** - Cart operations
  - Test GetCartWithItemsAsync
  - Test GetCartByUserIdAsync

- [ ] **UserRepositoryTests.cs** - User operations
  - Test GetByEmailAsync
  - Test EmailExistsAsync

- [ ] **CategoryRepositoryTests.cs** - Category operations
  - Test GetCategoryBySlugAsync
  - Test GetCategoriesWithProductCountAsync

- [ ] **ReviewRepositoryTests.cs** - Review operations
  - Test GetReviewsByProductAsync
  - Test GetUserProductReviewAsync

- [ ] **WishlistRepositoryTests.cs** - Wishlist operations
  - Test GetWishlistWithItemsAsync
  - Test GetWishlistByUserIdAsync

#### 1.2 UnitOfWork Tests
- [ ] **UnitOfWorkTests.cs**
  - Test repository property access
  - Test SaveChangesAsync
  - Test transaction handling if applicable

#### 1.3 Extension Tests
- [ ] **QueryableExtensionsTests.cs**
  - Test Paginate extension method
  - Test all filtering/sorting extensions

### Phase 2: Backend Service Tests

#### 2.1 Email Service Tests
- [ ] **SendGridEmailServiceTests.cs**
  - Test SendPasswordResetEmailAsync
  - Test SendEmailVerificationAsync
  - Test SendLowStockAlertAsync
  - Test SendOrderConfirmationAsync
  - Test all error handling paths

- [ ] **SmtpEmailServiceTests.cs**
  - Test all email sending methods
  - Test SMTP connection handling
  - Test error scenarios

#### 2.2 Other Service Tests
- [ ] **CurrentUserServiceTests.cs**
  - Test GetCurrentUserId
  - Test IsAuthenticated property
  - Test GetUserRole

- [ ] **InMemoryPaymentStoreTests.cs**
  - Test StorePaymentAsync
  - Test GetPaymentAsync
  - Test UpdatePaymentStatusAsync

### Phase 3: Backend Middleware and Configuration Tests

#### 3.1 Middleware Tests
- [ ] **CsrfMiddlewareTests.cs**
  - Test token generation
  - Test token validation
  - Test exclusion paths

- [ ] **SecurityHeadersMiddlewareTests.cs**
  - Test all security headers are added
  - Test header values

#### 3.2 Health Check Tests
- [ ] **MemoryHealthCheckTests.cs**
  - Test healthy state
  - Test degraded state
  - Test unhealthy state

- [ ] **HealthCheckResponseWriterTests.cs**
  - Test JSON response generation
  - Test response format

#### 3.3 Configuration Tests
- [ ] **AppConfigurationTests.cs**
  - Test configuration binding

- [ ] **ConfigurationExtensionsTests.cs**
  - Test all extension methods

- [ ] **ServiceCollectionExtensionsTests.cs**
  - Test all service registrations

- [ ] **ApplicationBuilderExtensionsTests.cs**
  - Test middleware registration order

- [ ] **DatabaseSchemaValidatorTests.cs**
  - Test schema validation logic

- [ ] **LoggingExtensionsTests.cs**
  - Test logging configuration

### Phase 4: Backend Controller Integration Tests

#### 4.1 Missing Controller Tests
- [ ] **HealthCheckControllerTests.cs** or add health check endpoints to existing tests
  - Test /health endpoint
  - Test /health/ready endpoint

### Phase 5: Frontend Storefront Tests

#### 5.1 Component Tests
Create tests in `src/components/__tests__/`:

- [ ] **CartItem.test.tsx** - Cart item display and interactions
- [ ] **CategoryFilter.test.tsx** - Category filtering
- [ ] **EmptyState.test.tsx** - Empty state display
- [ ] **ErrorAlert.test.tsx** - Error display
- [ ] **Footer.test.tsx** - Footer rendering
- [ ] **Header.test.tsx** - Navigation, auth state, cart icon
- [ ] **LoadingSkeleton.test.tsx** - Loading states
- [ ] **OptimizedImage.test.tsx** - Image loading and optimization
- [ ] **PageHeader.test.tsx** - Page header display
- [ ] **PaginatedView.test.tsx** - Pagination controls
- [ ] **ProductCard.test.tsx** - Product card display and actions
- [ ] **ProtectedRoute.test.tsx** - Route protection logic
- [ ] **QueryRenderer.test.tsx** - Query state rendering
- [ ] **ReviewForm.test.tsx** - Review submission form
- [ ] **ReviewList.test.tsx** - Review list display
- [ ] **StarRating.test.tsx** - Star rating component
- [ ] **Toast.test.tsx** - Toast notifications
- [ ] **ToastContainer.test.tsx** - Toast container

UI Components:
- [ ] **Button.test.tsx** - Button variants and states
- [ ] **Card.test.tsx** - Card component
- [ ] **Input.test.tsx** - Input component

Skeleton Components:
- [ ] **CartSkeleton.test.tsx**
- [ ] **ProductSkeleton.test.tsx**
- [ ] **ProductsGridSkeleton.test.tsx**
- [ ] **ProfileSkeleton.test.tsx**
- [ ] **Skeleton.test.tsx**

Icon Components:
- [ ] **CheckIcon.test.tsx**
- [ ] **HeartIcon.test.tsx**
- [ ] **PackageIcon.test.tsx**
- [ ] **SearchIcon.test.tsx**
- [ ] **ShoppingCartIcon.test.tsx**
- [ ] **UserIcon.test.tsx**

Page Sub-components:
- [ ] **CartItemList.test.tsx**
- [ ] **CartSummary.test.tsx**
- [ ] **CheckoutForm.test.tsx**
- [ ] **OrderSuccess.test.tsx**
- [ ] **OrderSummary.test.tsx**
- [ ] **PromoCodeSection.test.tsx**
- [ ] **OrderHeader.test.tsx**
- [ ] **OrderItemsList.test.tsx**
- [ ] **OrderTotals.test.tsx**
- [ ] **ShippingAddress.test.tsx**
- [ ] **OrderCard.test.tsx**
- [ ] **ProductActions.test.tsx**
- [ ] **ProductImageGallery.test.tsx**
- [ ] **ProductInfo.test.tsx**
- [ ] **ActiveFilters.test.tsx**
- [ ] **ProductFilters.test.tsx**
- [ ] **ProductGrid.test.tsx**
- [ ] **ProductSearchBar.test.tsx**
- [ ] **AccountDetails.test.tsx**
- [ ] **ProfileForm.test.tsx**
- [ ] **ProfileHeader.test.tsx**
- [ ] **ProfileMessages.test.tsx**
- [ ] **WishlistCard.test.tsx**

#### 5.2 Hook Tests
Create tests in `src/hooks/__tests__/`:

- [ ] **useApiErrorHandler.test.ts**
- [ ] **useAuth.test.ts**
- [ ] **useCartSync.test.ts**
- [ ] **useCheckout.test.ts**
- [ ] **useErrorHandler.test.ts**
- [ ] **useForm.test.ts**
- [ ] **useLocalStorage.test.ts**
- [ ] **useOnlineStatus.test.ts**
- [ ] **usePerformanceMonitor.test.ts**
- [ ] **useProductDetails.test.ts**
- [ ] **useProductFilters.test.ts**
- [ ] **useProfileForm.test.ts**
- [ ] **useToast.test.ts**

#### 5.3 Page Tests
Create tests in `src/pages/__tests__/`:

- [ ] **Cart.test.tsx**
- [ ] **Checkout.test.tsx**
- [ ] **ErrorPage.test.tsx**
- [ ] **ForgotPassword.test.tsx**
- [ ] **Home.test.tsx**
- [ ] **Login.test.tsx**
- [ ] **OrderDetail.test.tsx**
- [ ] **OrderHistory.test.tsx**
- [ ] **ProductDetail.test.tsx**
- [ ] **Products.test.tsx**
- [ ] **Profile.test.tsx**
- [ ] **Register.test.tsx**
- [ ] **ResetPassword.test.tsx**
- [ ] **Wishlist.test.tsx**

#### 5.4 Store Tests
Create tests in `src/store/`:

Slices (`src/store/slices/__tests__/`):
- [ ] **authSlice.test.ts**
- [ ] **toastSlice.test.ts**

API (`src/store/api/__tests__/`):
- [ ] **authApi.test.ts** - Mock API calls
- [ ] **baseApi.test.ts** - Base API configuration
- [ ] **cartApi.test.ts**
- [ ] **categoriesApi.test.ts**
- [ ] **inventoryApi.test.ts**
- [ ] **ordersApi.test.ts**
- [ ] **productApi.test.ts**
- [ ] **profileApi.test.ts**
- [ ] **promoCodeApi.test.ts**
- [ ] **reviewsApi.test.ts**
- [ ] **wishlistApi.test.ts**

Middleware (`src/store/middleware/__tests__/`):
- [ ] **cartPersistence.test.ts**

#### 5.5 Utility Tests
- [ ] **constants.test.ts**

### Phase 6: Frontend Admin Tests

#### 6.1 Component Tests
Create tests in `src/components/__tests__/`:

- [ ] **AuthInitializer.test.tsx**
- [ ] **ErrorBoundary.test.tsx**
- [ ] **Header.test.tsx**
- [ ] **ProductForm.test.tsx**
- [ ] **PromoCodeForm.test.tsx**
- [ ] **QueryRenderer.test.tsx**
- [ ] **Sidebar.test.tsx**

Icon Components:
- [ ] **CustomersIcon.test.tsx**
- [ ] **DashboardIcon.test.tsx**
- [ ] **InventoryIcon.test.tsx**
- [ ] **OrdersIcon.test.tsx**
- [ ] **ProductsIcon.test.tsx**
- [ ] **ReviewsIcon.test.tsx**

Toast Components:
- [ ] **Toast.test.tsx**
- [ ] **ToastContainer.test.tsx**

UI Components:
- [ ] **Badge.test.tsx**
- [ ] **Card.test.tsx**
- [ ] **Pagination.test.tsx**
- [ ] **Table.test.tsx**

Layouts:
- [ ] **AdminLayout.test.tsx**

#### 6.2 Page Tests
Create tests in `src/pages/__tests__/`:

- [ ] **Customers.test.tsx**
- [ ] **Dashboard.test.tsx**
- [ ] **ErrorPage.test.tsx**
- [ ] **Inventory.test.tsx**
- [ ] **Login.test.tsx**
- [ ] **Orders.test.tsx**
- [ ] **Products.test.tsx**
- [ ] **PromoCodes.test.tsx**
- [ ] **Reviews.test.tsx**
- [ ] **Settings.test.tsx**

#### 6.3 Store Tests
API (`src/store/api/__tests__/`):
- [ ] **authApi.test.ts**
- [ ] **customersApi.test.ts**
- [ ] **dashboardApi.test.ts**
- [ ] **inventoryApi.test.ts**
- [ ] **ordersApi.test.ts**
- [ ] **productsApi.test.ts**
- [ ] **promoCodesApi.test.ts**
- [ ] **reviewsApi.test.ts**

#### 6.4 Hook Tests
- [ ] **useAppDispatch.test.ts**
- [ ] **useToast.test.ts**

---

## Testing Tools and Configuration

### Backend Tools
- **MSTest 4.0.1** - Test framework
- **Moq 4.20.72** - Mocking library
- **FluentAssertions 7.0.0** - Assertions
- **coverlet 6.0.2** - Code coverage
- **Microsoft.EntityFrameworkCore.InMemory 10.0.0** - In-memory database
- **Bogus 35.6.1** - Test data generation

### Frontend Tools
- **Vitest 4.0.18** - Test runner
- **@testing-library/react 16.3.2** - React testing utilities
- **@testing-library/jest-dom 6.9.1** - DOM matchers
- **@testing-library/user-event 14.6.1** - User interaction simulation
- **jsdom 28.0.0** - DOM environment
- **Playwright 1.58.2** - E2E testing (storefront only)

### Coverage Configuration

#### Backend - Update .runsettings
```xml
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura,json,html</Format>
          <Exclude>[ECommerce.Tests]*,[ECommerce.API]Program,[ECommerce.API]*.Migrations*</Exclude>
          <Include>[ECommerce.*]*</Include>
          <SingleHit>true</SingleHit>
          <UseSourceLink>false</UseSourceLink>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

#### Frontend - Update vite.config.ts
Add coverage thresholds:
```typescript
coverage: {
  provider: 'v8',
  reporter: ['text', 'json', 'html', 'lcov'],
  exclude: [
    'node_modules/',
    'src/test/',
    '**/*.d.ts',
    '**/*.config.*',
    '**/mockData/**',
    'dist/',
    'src/main.tsx',  // Entry point
    'src/vite-env.d.ts',
  ],
  thresholds: {
    lines: 100,
    functions: 100,
    branches: 100,
    statements: 100,
  },
  all: true,  // Include all files, even untested ones
}
```

---

## Test Patterns and Best Practices

### Backend Test Patterns

#### Service Test Pattern
```csharp
[TestClass]
public class XxxServiceTests
{
    // Mocks
    private Mock<IXxxRepository> _mockRepository = null!;
    private Mock<ILogger<XxxService>> _mockLogger = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    
    // System Under Test
    private XxxService _sut = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IXxxRepository>();
        _mockLogger = new Mock<ILogger<XxxService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _sut = new XxxService(_mockRepository.Object, _mockLogger.Object);
    }
    
    // Tests organized by method
    #region MethodXxx Tests
    
    [TestMethod]
    public async Task MethodXxx_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = TestDataFactory.CreateValidInput();
        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(TestDataFactory.CreateEntity());
        
        // Act
        var result = await _sut.MethodXxx(input);
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }
    
    [TestMethod]
    public async Task MethodXxx_InvalidInput_ThrowsException()
    {
        // Arrange
        var input = TestDataFactory.CreateInvalidInput();
        
        // Act
        Func<Task> act = () => _sut.MethodXxx(input);
        
        // Assert
        await act.Should().ThrowAsync<XxxException>();
    }
    
    #endregion
}
```

#### Repository Test Pattern
```csharp
[TestClass]
public class XxxRepositoryTests
{
    private AppDbContext _context = null!;
    private XxxRepository _sut = null!;
    
    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _sut = new XxxRepository(_context);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }
    
    [TestMethod]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        var entity = TestDataFactory.CreateEntity();
        _context.Set<Xxx>().Add(entity);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _sut.GetByIdAsync(entity.Id);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
    }
}
```

### Frontend Test Patterns

#### Component Test Pattern
```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { Provider } from 'react-redux'
import { BrowserRouter } from 'react-router-dom'
import { configureStore } from '@reduxjs/toolkit'
import { XxxComponent } from '../XxxComponent'

// Mock hooks if needed
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

describe('XxxComponent', () => {
  const renderComponent = (props = {}, initialState = {}) => {
    const store = configureStore({
      reducer: { /* reducers */ },
      preloadedState: initialState,
    })
    
    return render(
      <Provider store={store}>
        <BrowserRouter>
          <XxxComponent {...props} />
        </BrowserRouter>
      </Provider>
    )
  }
  
  beforeEach(() => {
    vi.clearAllMocks()
  })
  
  it('renders correctly', () => {
    renderComponent()
    expect(screen.getByText('Expected Text')).toBeInTheDocument()
  })
  
  it('handles user interaction', async () => {
    const user = userEvent.setup()
    renderComponent()
    
    await user.click(screen.getByRole('button'))
    
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/expected-path')
    })
  })
  
  it('displays error state', () => {
    renderComponent({}, { error: 'Test error' })
    expect(screen.getByText('Test error')).toBeInTheDocument()
  })
})
```

#### Hook Test Pattern
```typescript
import { renderHook, act } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useXxx } from '../useXxx'

describe('useXxx', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })
  
  it('returns initial state', () => {
    const { result } = renderHook(() => useXxx())
    
    expect(result.current.data).toBeNull()
    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBeNull()
  })
  
  it('updates state on action', async () => {
    const { result } = renderHook(() => useXxx())
    
    await act(async () => {
      await result.current.doSomething('input')
    })
    
    expect(result.current.data).toEqual({ expected: 'data' })
  })
})
```

#### Redux Slice Test Pattern
```typescript
import { describe, it, expect } from 'vitest'
import { xxxSlice, xxxAction, xxxReducer } from '../xxxSlice'
import type { XxxState } from '../xxxSlice'

describe('xxxSlice', () => {
  const initialState: XxxState = {
    data: null,
    loading: false,
    error: null,
  }
  
  it('returns initial state', () => {
    expect(xxxSlice.reducer(undefined, { type: 'unknown' })).toEqual(initialState)
  })
  
  it('handles xxxAction', () => {
    const actual = xxxSlice.reducer(initialState, xxxAction('payload'))
    expect(actual.data).toEqual('payload')
  })
})
```

#### API Mock Test Pattern
```typescript
import { setupServer } from 'msw/node'
import { http, HttpResponse } from 'msw'
import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest'

const server = setupServer(
  http.get('/api/xxx', () => {
    return HttpResponse.json({ data: 'mocked' })
  })
)

beforeAll(() => server.listen())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

describe('xxxApi', () => {
  it('fetches data successfully', async () => {
    // Test RTK Query or axios calls
  })
})
```

---

## CI/CD Integration

### GitHub Actions Workflow
```yaml
name: Test Coverage

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Run tests with coverage
        run: |
          cd src/backend
          dotnet test --collect:"XPlat Code Coverage" \
                      --results-directory:coverage \
                      --settings .runsettings
      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: src/backend/coverage/**/*.cobertura.xml
          flags: backend

  storefront-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: src/frontend/storefront/package-lock.json
      - name: Install dependencies
        run: cd src/frontend/storefront && npm ci
      - name: Run tests with coverage
        run: cd src/frontend/storefront && npm run test:coverage
      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: src/frontend/storefront/coverage/lcov.info
          flags: storefront

  admin-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: src/frontend/admin/package-lock.json
      - name: Install dependencies
        run: cd src/frontend/admin && npm ci
      - name: Run tests with coverage
        run: cd src/frontend/admin && npm run test:coverage
      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: src/frontend/admin/coverage/lcov.info
          flags: admin
```

---

## Coverage Reports

### Local Coverage Commands

**Backend:**
```bash
cd src/backend
dotnet test --collect:"XPlat Code Coverage" --settings .runsettings
# Report generated in: TestResults/{guid}/coverage.cobertura.xml
```

**Frontend:**
```bash
cd src/frontend/storefront  # or admin
npm run test:coverage
# Report generated in: coverage/index.html
```

### Coverage Badge
Add to README.md:
```markdown
[![Coverage](https://codecov.io/gh/owner/repo/branch/graph/badge.svg)](https://codecov.io/gh/owner/repo)
```

---

## Estimated Test Count

| Area | Estimated New Tests |
|------|---------------------|
| Backend Infrastructure | ~150 tests |
| Backend Services | ~100 tests |
| Backend Middleware/Config | ~80 tests |
| Storefront Components | ~200 tests |
| Storefront Hooks | ~100 tests |
| Storefront Pages | ~150 tests |
| Storefront Store | ~80 tests |
| Admin Components | ~100 tests |
| Admin Pages | ~100 tests |
| Admin Store | ~60 tests |
| **Total** | **~1,120 tests** |

---

## Success Criteria

1. **Line Coverage**: 100% for all production code
2. **Branch Coverage**: 100% for all conditional logic
3. **Function Coverage**: 100% for all functions/methods
4. **All tests pass** in CI/CD pipeline
5. **No flaky tests** - tests must be deterministic
6. **Coverage reports** uploaded to Codecov or similar

---

## Phase 7: E2E Tests (Playwright)

The storefront already has Playwright configured with 4 E2E test files. This phase expands E2E coverage.

### Current E2E Tests
- `auth.spec.ts` - Authentication flows
- `cart.spec.ts` - Cart operations
- `checkout-guest.spec.ts` - Guest checkout
- `product-browsing.spec.ts` - Product browsing

### Additional E2E Tests Needed

#### 7.1 User Account Flows
- [ ] **profile.spec.ts**
  - View profile
  - Update profile information
  - Change password
  - View order history
  - View wishlist

- [ ] **orders.spec.ts**
  - View order history
  - View order details
  - Track order status
  - Reorder from past order

- [ ] **reviews.spec.ts**
  - Submit product review
  - Edit existing review
  - View product reviews

#### 7.2 Product Discovery
- [ ] **search.spec.ts**
  - Search by product name
  - Search with filters
  - Search results pagination
  - No results state

- [ ] **categories.spec.ts**
  - Browse by category
  - Category filtering
  - Category navigation

- [ ] **wishlist.spec.ts**
  - Add to wishlist
  - Remove from wishlist
  - Move to cart from wishlist
  - Wishlist persistence

#### 7.3 Checkout Flows
- [ ] **checkout-auth.spec.ts**
  - Authenticated user checkout
  - Saved address selection
  - Payment method selection
  - Promo code application
  - Order confirmation

- [ ] **payment.spec.ts**
  - Payment method selection
  - Payment validation
  - Payment failure handling

#### 7.4 Admin E2E Tests (New)
Create `src/frontend/admin/e2e/` directory:

- [ ] **admin-auth.spec.ts**
  - Admin login
  - Admin logout
  - Session timeout handling

- [ ] **admin-products.spec.ts**
  - Product listing
  - Create product
  - Edit product
  - Delete product
  - Product search/filter

- [ ] **admin-orders.spec.ts**
  - Order listing
  - Order detail view
  - Order status update
  - Order filtering

- [ ] **admin-inventory.spec.ts**
  - Inventory overview
  - Stock adjustment
  - Low stock alerts

- [ ] **admin-promocodes.spec.ts**
  - Promo code listing
  - Create promo code
  - Edit promo code
  - Deactivate promo code

### E2E Test Configuration Updates

#### Update playwright.config.ts
```typescript
export default defineConfig({
  // ... existing config
  
  // Add visual regression testing
  expect: {
    toHaveScreenshot: {
      maxDiffPixels: 100,
      threshold: 0.2,
    },
  },
  
  // Add accessibility testing
  projects: [
    // ... existing projects
    {
      name: 'accessibility',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
```

---

## Phase 8: Additional Testing Best Practices

### 8.1 Visual Regression Testing

Install and configure:
```bash
npm install -D @playwright/test-axe
```

- [ ] **Visual Regression Tests**
  - Component screenshots for UI consistency
  - Page-level screenshots for critical flows
  - Cross-browser visual comparison

- [ ] **Implementation**
  ```typescript
  // Example visual test
  test('product card visual regression', async ({ page }) => {
    await page.goto('/products/1');
    await expect(page.locator('.product-card')).toHaveScreenshot('product-card.png');
  });
  ```

### 8.2 Accessibility Testing

- [ ] **Install axe-core**
  ```bash
  npm install -D axe-playwright
  ```

- [ ] **Accessibility Tests**
  ```typescript
  import { injectAxe, checkA11y } from 'axe-playwright';
  
  test('product page accessibility', async ({ page }) => {
    await page.goto('/products/1');
    await injectAxe(page);
    await checkA11y(page, null, {
      detailedReport: true,
    });
  });
  ```

- [ ] **Accessibility Test Cases**
  - All pages pass WCAG 2.1 AA
  - Keyboard navigation works
  - Screen reader compatibility
  - Color contrast validation
  - Focus management

### 8.3 Performance Testing

- [ ] **Lighthouse CI Integration**
  ```yaml
  # .github/workflows/lighthouse.yml
  name: Lighthouse CI
  on: [push]
  jobs:
    lighthouse:
      runs-on: ubuntu-latest
      steps:
        - uses: actions/checkout@v4
        - name: Run Lighthouse CI
          uses: treosh/lighthouse-ci-action@v10
          with:
            configPath: ./lighthouserc.json
  ```

- [ ] **Performance Test Cases**
  - Page load time < 3s
  - First Contentful Paint < 1.5s
  - Time to Interactive < 3.5s
  - Cumulative Layout Shift < 0.1
  - Largest Contentful Paint < 2.5s

- [ ] **API Performance Tests**
  - Response time < 200ms for GET requests
  - Response time < 500ms for POST requests
  - Concurrent user handling

### 8.4 Security Testing

- [ ] **OWASP ZAP Integration**
  - Automated security scans in CI
  - SQL injection tests
  - XSS vulnerability tests
  - CSRF protection validation

- [ ] **Security Test Cases**
  - Authentication bypass attempts
  - Authorization boundary testing
  - Input validation testing
  - Session management testing
  - API rate limiting

### 8.5 API Contract Testing

- [ ] **Install Pact for Contract Testing**
  ```bash
  # Backend
  dotnet add package PactNet
  
  # Frontend
  npm install -D @pact-foundation/pact
  ```

- [ ] **Contract Tests**
  - Define consumer contracts
  - Verify provider compliance
  - Breaking change detection

### 8.6 Mutation Testing

- [ ] **Backend Mutation Testing (Stryker)**
  ```bash
  dotnet tool install -g dotnet-stryker
  ```
  
  ```json
  // stryker-config.json
  {
    "stryker-config": {
      "solution": "ECommerce.sln",
      "project": "ECommerce.Application",
      "thresholds": {
        "high": 80,
        "low": 60,
        "break": 0
      }
    }
  }
  ```

- [ ] **Frontend Mutation Testing**
  ```bash
  npm install -D stryker-cli
  ```

### 8.7 Test Data Management

- [ ] **Backend Test Data Builders**
  - Use Bogus for realistic test data
  - Create domain-specific data builders
  - Seed database for integration tests

- [ ] **Frontend Test Data Mocks**
  - MSW (Mock Service Worker) for API mocking
  - Fixture files for consistent test data
  - Factory functions for component props

### 8.8 Error Boundary & Edge Case Testing

- [ ] **Error Scenarios**
  - Network failure handling
  - API timeout handling
  - Invalid data handling
  - Empty state handling
  - Loading state handling

- [ ] **Boundary Testing**
  - Maximum input length
  - Minimum input length
  - Special characters
  - Unicode characters
  - SQL injection attempts

---

## Phase 9: Test Infrastructure Improvements

### 9.1 Test Database Management

- [ ] **Docker Compose Test Environment**
  ```yaml
  # docker-compose.test.yml
  services:
    test-db:
      image: postgres:15
      environment:
        POSTGRES_DB: ecommerce_test
        POSTGRES_USER: test
        POSTGRES_PASSWORD: test
      ports:
        - "5433:5432"
  ```

- [ ] **Database Reset Scripts**
  - Clean database between tests
  - Seed test data
  - Migration verification

### 9.2 Mock Service Worker (MSW) Setup

- [ ] **Install MSW**
  ```bash
  npm install -D msw
  npx msw init public/ --save
  ```

- [ ] **Define Handlers**
  ```typescript
  // src/mocks/handlers.ts
  import { http, HttpResponse } from 'msw'
  
  export const handlers = [
    http.get('/api/products', () => {
      return HttpResponse.json({ products: [] })
    }),
    // ... more handlers
  ]
  ```

- [ ] **Setup for Tests**
  ```typescript
  // src/test/setup.ts
  import { server } from './mocks/server'
  
  beforeAll(() => server.listen())
  afterEach(() => server.resetHandlers())
  afterAll(() => server.close())
  ```

### 9.3 Test Reporting

- [ ] **Allure Reports (Backend)**
  ```bash
  dotnet add package Allure.MSTest
  ```

- [ ] **HTML Reports (Frontend)**
  - Vitest HTML reporter
  - Playwright HTML reporter
  - Coverage HTML reports

### 9.4 Flaky Test Detection

- [ ] **Configure Retry Logic**
  ```typescript
  // vitest.config.ts
  export default defineConfig({
    test: {
      retry: 2,
      bail: 5,
    },
  })
  ```

- [ ] **Flaky Test Tracking**
  - Log flaky test occurrences
  - Quarantine flaky tests
  - Automatic issue creation

---

## Testing Pyramid Summary

```
                    ┌─────────┐
                    │   E2E   │  ← Playwright (Critical User Flows)
                    │  Tests  │    ~50 tests
                   ╱└─────────┘╲
                  ╱             ╲
                 ╱               ╲
                ╱  Integration    ╲  ← API Integration Tests
               ╱     Tests        ╱    ~100 tests
              ╱                   ╲
             ╱─────────────────────╲
            ╱                       ╲
           ╱      Unit Tests         ╲  ← Component, Hook, Service Tests
          ╱                           ╲    ~1,000 tests
         ╱─────────────────────────────╲
```

---

## Updated Estimated Test Count

| Area | Estimated Tests |
|------|-----------------|
| Backend Unit Tests | ~330 tests |
| Backend Integration Tests | ~100 tests |
| Storefront Unit Tests | ~530 tests |
| Admin Unit Tests | ~260 tests |
| E2E Tests (Storefront) | ~50 tests |
| E2E Tests (Admin) | ~30 tests |
| Accessibility Tests | ~20 tests |
| Visual Regression Tests | ~30 tests |
| **Total** | **~1,350 tests** |

---

## Phase 10: Senior Developer Considerations

### 10.1 Test Strategy & Risk-Based Prioritization

A senior developer understands that 100% coverage doesn't equal 100% quality. Prioritize tests based on:

| Priority | Area | Rationale |
|----------|------|-----------|
| P0 (Critical) | Payment processing, Order flow, Authentication | Business-critical, financial impact |
| P1 (High) | Cart, Inventory, Promo codes | Core business functionality |
| P2 (Medium) | Product catalog, Categories, Reviews | User experience features |
| P3 (Low) | UI components, Static pages | Lower risk, easier to fix |

### 10.2 Test Architecture & Design Patterns

**Test Fixtures Pattern:**
```csharp
// Backend - Shared test fixtures
public class OrderTestFixture : IDisposable
{
    public AppDbContext DbContext { get; }
    public IUnitOfWork UnitOfWork { get; }
    
    public OrderTestFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")
            .Options;
        DbContext = new AppDbContext(options);
        UnitOfWork = new UnitOfWork(DbContext);
    }
    
    public void Dispose() => DbContext.Dispose();
}

[Collection("Order Tests")]
public class OrderServiceTests : IClassFixture<OrderTestFixture>
{
    // Tests use shared fixture
}
```

**Test Builder Pattern:**
```typescript
// Frontend - Test data builders
class ProductBuilder {
    private product: Product = {
        id: '1',
        name: 'Test Product',
        price: 100,
        // ... defaults
    };
    
    withName(name: string): this {
        this.product.name = name;
        return this;
    }
    
    withPrice(price: number): this {
        this.product.price = price;
        return this;
    }
    
    build(): Product {
        return { ...this.product };
    }
}

// Usage
const product = new ProductBuilder()
    .withName('Custom Name')
    .withPrice(200)
    .build();
```

### 10.3 Test Execution Strategy

**Parallel Execution Configuration:**
```typescript
// vitest.config.ts
export default defineConfig({
    test: {
        pool: 'threads',
        poolOptions: {
            threads: {
                singleThread: false,
                minThreads: 2,
                maxThreads: 4,
            },
        },
        // Shard tests for CI
        shard: process.env.CI ? { count: 4, index: Number(process.env.SHARD_INDEX) } : undefined,
    },
});
```

**Backend Parallel Execution:**
```xml
<!-- .runsettings -->
<RunSettings>
  <RunConfiguration>
    <MaxCpuCount>0</MaxCpuCount> <!-- 0 = auto -->
  </RunConfiguration>
  <MSTest>
    <Parallelize Workers="4" Scope="ClassLevel" />
  </MSTest>
</RunSettings>
```

### 10.4 Test Environment Management

**Docker Compose for Integration Tests:**
```yaml
# docker-compose.test.yml
version: '3.8'
services:
  test-db:
    image: postgres:15
    environment:
      POSTGRES_DB: ecommerce_test
      POSTGRES_USER: test
      POSTGRES_PASSWORD: test
    ports:
      - "5433:5432"
    tmpfs:
      - /var/lib/postgresql/data
      
  test-redis:
    image: redis:7-alpine
    ports:
      - "6380:6379"
      
  test-mailhog:
    image: mailhog/mailhog
    ports:
      - "1026:1025"  # SMTP
      - "8026:8025"  # Web UI
```

### 10.5 Test Data Management Strategy

**Database Reseed Strategy:**
```csharp
public class TestDatabase : IDisposable
{
    private readonly AppDbContext _context;
    
    public TestDatabase()
    {
        _context = new AppDbContext(TestDbContextOptions);
        _context.Database.EnsureCreated();
    }
    
    public async Task ResetAsync()
    {
        // Fast truncate for PostgreSQL
        await _context.Database.ExecuteSqlRawAsync(
            "TRUNCATE ALL TABLES RESTART IDENTITY CASCADE");
        await SeedBaselineDataAsync();
    }
    
    private async Task SeedBaselineDataAsync()
    {
        // Minimal baseline data for all tests
        _context.Categories.AddRange(GetBaselineCategories());
        await _context.SaveChangesAsync();
    }
}
```

### 10.6 Test Metrics & Monitoring

**Beyond Coverage - Quality Metrics:**
| Metric | Target | Tool |
|--------|--------|------|
| Code Coverage | 100% lines | Coverlet/Vitest |
| Mutation Score | >80% | Stryker |
| Test Duration | <5min total | Built-in |
| Flaky Test Rate | <1% | Custom tracking |
| Test Maintainability Index | >70 | Custom analysis |

**Flaky Test Detection:**
```yaml
# GitHub Actions - Flaky test detection
- name: Run tests 3 times
  run: |
    for i in 1 2 3; do
      dotnet test --no-build || echo "Run $i failed"
    done
```

### 10.7 Developer Experience (DX)

**Pre-commit Hooks:**
```yaml
# .pre-commit-config.yaml
repos:
  - repo: local
    hooks:
      - id: backend-tests
        name: Backend Unit Tests
        entry: dotnet test --filter "Category=Unit"
        language: system
        pass_filenames: false
        
      - id: frontend-tests
        name: Frontend Unit Tests
        entry: npm run test:run
        language: system
        pass_filenames: false
```

**VS Code Test Configuration:**
```json
// .vscode/settings.json
{
    "dotnet.testExplorer.enable": true,
    "vitest.enable": true,
    "vitest.debuggerPort": 9229,
    "testExplorer.onStart": "retag",
    "testExplorer.onDidChange": "retag"
}
```

### 10.8 Test Documentation

**Living Documentation:**
```csharp
/// <summary>
/// Tests for OrderService.
/// 
/// Business Rules Tested:
/// 1. Orders can only be placed for in-stock items
/// 2. Total must match sum of item prices
/// 3. Promo codes must be valid and not expired
/// 
/// Edge Cases:
/// - Empty cart throws EmptyCartException
/// - Out of stock throws InsufficientStockException
/// - Invalid promo code throws InvalidPromoCodeException
/// </summary>
[TestClass]
public class OrderServiceTests
```

### 10.9 Integration with Development Workflow

**PR Template with Test Checklist:**
```markdown
## Test Checklist
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] All tests pass locally
- [ ] Coverage maintained or improved
- [ ] Edge cases covered
- [ ] Error paths tested
- [ ] No flaky tests introduced
```

**Branch Protection Rules:**
- Require PR approval
- Require status checks: tests, coverage
- Require branches to be up to date
- Require linear history

### 10.10 Cost-Benefit Analysis

**Test ROI Calculation:**
```
Cost of Test = Development Time + Maintenance Time + Execution Time
Benefit = Bugs Caught × Cost Per Bug + Confidence Gained

Example:
- Test development: 2 hours
- Maintenance over year: 1 hour
- Execution: 0.1 seconds × 1000 runs = 100 seconds
- Total Cost: ~3 hours

Benefit:
- Catches 1 critical bug: Saves 8+ hours debugging
- Confidence for refactoring: Priceless
```

### 10.11 Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | Solution |
|--------------|--------------|----------|
| Testing implementation details | Brittle tests | Test behavior, not implementation |
| Over-mocking | Tests don't match reality | Use real dependencies when possible |
| Large test setup | Hard to understand | Use test fixtures and builders |
| Asserting on multiple things | Hard to debug failures | One assertion per test |
| Testing framework code | Wasted effort | Trust the framework |
| 100% coverage obsession | Diminishing returns | Focus on critical paths |

### 10.12 Continuous Improvement

**Test Retrospective Questions:**
1. Which tests caught the most bugs?
2. Which tests were most brittle?
3. What areas need more coverage?
4. Are tests fast enough?
5. Is test maintenance sustainable?

---

## Next Steps

1. Review and approve this plan
2. Switch to Code mode to begin implementation
3. Start with backend infrastructure tests (highest value)
4. Progress through phases systematically
5. Monitor coverage reports after each phase
6. Conduct test retrospectives monthly