# E-Commerce Application - Comprehensive Test Plan

## Table of Contents
1. [Overview](#overview)
2. [Test Strategy](#test-strategy)
3. [Test Environment Setup](#test-environment-setup)
4. [Unit Tests](#unit-tests)
5. [Integration Tests](#integration-tests)
6. [End-to-End Tests](#end-to-end-tests)
7. [Test Data Management](#test-data-management)
8. [Coverage Goals](#coverage-goals)
9. [Implementation Roadmap](#implementation-roadmap)

---

## Overview

This document outlines the comprehensive testing strategy for the E-Commerce application, covering all layers of the Clean Architecture implementation:

- **ECommerce.Core** - Domain entities, exceptions, and interfaces
- **ECommerce.Application** - Business logic and services
- **ECommerce.Infrastructure** - Data access and external services
- **ECommerce.API** - HTTP endpoints and middleware

### Application Components to Test

| Layer | Component | Count |
|-------|-----------|-------|
| Services | AuthService, ProductService, CategoryService, CartService, OrderService, ReviewService, WishlistService, PaymentService, PromoCodeService, InventoryService, UserService, DashboardService, EmailService | 13 |
| Controllers | Auth, Products, Categories, Cart, Orders, Reviews, Wishlist, Payments, PromoCodes, Inventory, Profile, Dashboard | 12 |
| Repositories | Generic Repository, Category, Cart, Review, Wishlist, Order, User, Product | 8 |
| Entities | User, Product, Category, Cart, CartItem, Order, OrderItem, PromoCode, Review, Wishlist, Address, InventoryLog | 12 |
| Exceptions | Base (4) + Domain Specific (33) | 37 |

---

## Test Strategy

### Testing Pyramid

```
          /\
         /  \        E2E Tests (10%)
        /----\       - Full user workflows
       /      \      - API integration
      /--------\     Integration Tests (30%)
     /          \    - Service + Repository
    /------------\   - Controller + Service
   /              \  Unit Tests (60%)
  /----------------\ - Services, Entities, DTOs
```

### Test Types

| Type | Purpose | Tools | Execution Time |
|------|---------|-------|----------------|
| Unit | Test individual components in isolation | MSTest, Moq | Fast (< 1 min) |
| Integration | Test component interactions | MSTest, TestContainers, EF InMemory | Medium (< 5 min) |
| E2E | Test complete user workflows | MSTest, WebApplicationFactory | Slow (< 15 min) |

---

## Test Environment Setup

### Required NuGet Packages

```xml
<!-- Add to ECommerce.Tests.csproj -->
<ItemGroup>
  <!-- Test Framework -->
  <PackageReference Include="MSTest" Version="4.0.1" />

  <!-- Mocking -->
  <PackageReference Include="Moq" Version="4.20.72" />

  <!-- Assertions -->
  <PackageReference Include="FluentAssertions" Version="7.0.0" />

  <!-- Integration Testing -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />

  <!-- Test Data Generation -->
  <PackageReference Include="Bogus" Version="35.6.1" />

  <!-- Code Coverage -->
  <PackageReference Include="coverlet.collector" Version="6.0.2" />
</ItemGroup>
```

### Project References

```xml
<ItemGroup>
  <ProjectReference Include="..\ECommerce.API\ECommerce.API.csproj" />
  <ProjectReference Include="..\ECommerce.Application\ECommerce.Application.csproj" />
  <ProjectReference Include="..\ECommerce.Core\ECommerce.Core.csproj" />
  <ProjectReference Include="..\ECommerce.Infrastructure\ECommerce.Infrastructure.csproj" />
</ItemGroup>
```

### Test Folder Structure

```
ECommerce.Tests/
├── Unit/
│   ├── Services/
│   │   ├── AuthServiceTests.cs
│   │   ├── ProductServiceTests.cs
│   │   ├── CategoryServiceTests.cs
│   │   ├── CartServiceTests.cs
│   │   ├── OrderServiceTests.cs
│   │   ├── ReviewServiceTests.cs
│   │   ├── WishlistServiceTests.cs
│   │   ├── PaymentServiceTests.cs
│   │   ├── PromoCodeServiceTests.cs
│   │   ├── InventoryServiceTests.cs
│   │   ├── UserServiceTests.cs
│   │   └── DashboardServiceTests.cs
│   ├── Entities/
│   │   ├── ProductTests.cs
│   │   ├── OrderTests.cs
│   │   └── PromoCodeTests.cs
│   └── Exceptions/
│       └── ExceptionTests.cs
├── Integration/
│   ├── Repositories/
│   │   ├── ProductRepositoryTests.cs
│   │   ├── CategoryRepositoryTests.cs
│   │   ├── OrderRepositoryTests.cs
│   │   └── CartRepositoryTests.cs
│   ├── Services/
│   │   ├── OrderServiceIntegrationTests.cs
│   │   └── CartServiceIntegrationTests.cs
│   └── Controllers/
│       ├── AuthControllerTests.cs
│       ├── ProductsControllerTests.cs
│       └── OrdersControllerTests.cs
├── EndToEnd/
│   ├── AuthenticationFlowTests.cs
│   ├── ShoppingFlowTests.cs
│   ├── OrderFlowTests.cs
│   └── AdminFlowTests.cs
├── Helpers/
│   ├── TestDataFactory.cs
│   ├── MockHelpers.cs
│   └── IntegrationTestBase.cs
└── Fixtures/
    ├── DatabaseFixture.cs
    └── WebApplicationFixture.cs
```

---

## Unit Tests

### 1. AuthService Tests

**File:** `Unit/Services/AuthServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `RegisterAsync_ValidData_ReturnsAuthResponse` | Register with valid email and password | Returns JWT token and user info |
| `RegisterAsync_DuplicateEmail_ThrowsDuplicateEmailException` | Register with existing email | Throws `DuplicateEmailException` |
| `RegisterAsync_InvalidEmail_ThrowsValidationException` | Register with malformed email | Throws validation exception |
| `LoginAsync_ValidCredentials_ReturnsAuthResponse` | Login with correct credentials | Returns JWT token |
| `LoginAsync_InvalidPassword_ThrowsInvalidCredentialsException` | Login with wrong password | Throws `InvalidCredentialsException` |
| `LoginAsync_NonExistentUser_ThrowsInvalidCredentialsException` | Login with non-existent email | Throws `InvalidCredentialsException` |
| `RefreshTokenAsync_ValidToken_ReturnsNewToken` | Refresh with valid token | Returns new JWT token |
| `RefreshTokenAsync_ExpiredToken_ThrowsInvalidTokenException` | Refresh with expired token | Throws `InvalidTokenException` |
| `VerifyEmailAsync_ValidToken_VerifiesUser` | Verify email with valid token | User email verified flag set to true |
| `VerifyEmailAsync_InvalidToken_ThrowsInvalidTokenException` | Verify with invalid token | Throws `InvalidTokenException` |
| `GeneratePasswordResetTokenAsync_ValidEmail_ReturnsToken` | Request password reset for valid email | Returns reset token |
| `GeneratePasswordResetTokenAsync_NonExistentEmail_ThrowsUserNotFoundException` | Request reset for non-existent email | Throws `UserNotFoundException` |
| `ResetPasswordAsync_ValidToken_ResetsPassword` | Reset password with valid token | Password updated successfully |
| `ChangePasswordAsync_ValidOldPassword_ChangesPassword` | Change password with correct old password | Password updated |
| `ChangePasswordAsync_InvalidOldPassword_ThrowsInvalidCredentialsException` | Change password with wrong old password | Throws `InvalidCredentialsException` |
| `HashPassword_ValidInput_ReturnsHash` | Hash a password | Returns BCrypt hash |
| `VerifyPassword_CorrectPassword_ReturnsTrue` | Verify correct password | Returns true |
| `VerifyPassword_IncorrectPassword_ReturnsFalse` | Verify incorrect password | Returns false |
| `GenerateJwtToken_ValidUser_ReturnsValidJwt` | Generate JWT for user | Returns valid JWT with claims |

---

### 2. ProductService Tests

**File:** `Unit/Services/ProductServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetProductsAsync_NoFilters_ReturnsAllProducts` | Get products without filters | Returns paginated products |
| `GetProductsAsync_WithCategoryFilter_ReturnsFilteredProducts` | Filter by category | Returns only category products |
| `GetProductsAsync_WithSearchQuery_ReturnsMatchingProducts` | Search products | Returns matching products |
| `GetProductsAsync_WithPriceRange_ReturnsFilteredProducts` | Filter by price range | Returns products in range |
| `GetProductsAsync_WithRatingFilter_ReturnsHighRatedProducts` | Filter by minimum rating | Returns products above rating |
| `GetProductsAsync_WithSorting_ReturnsSortedProducts` | Sort by price/name/date | Returns correctly sorted |
| `GetProductBySlugAsync_ExistingSlug_ReturnsProduct` | Get product by valid slug | Returns product detail |
| `GetProductBySlugAsync_NonExistentSlug_ThrowsProductNotFoundException` | Get product by invalid slug | Throws `ProductNotFoundException` |
| `GetProductByIdAsync_ExistingId_ReturnsProduct` | Get product by valid ID | Returns product detail |
| `GetProductByIdAsync_NonExistentId_ThrowsProductNotFoundException` | Get product by invalid ID | Throws `ProductNotFoundException` |
| `GetFeaturedProductsAsync_ReturnsRequestedCount` | Get featured products | Returns correct count |
| `CreateProductAsync_ValidData_ReturnsCreatedProduct` | Create product with valid data | Returns created product |
| `CreateProductAsync_DuplicateSlug_ThrowsDuplicateProductSlugException` | Create with existing slug | Throws `DuplicateProductSlugException` |
| `CreateProductAsync_InvalidCategoryId_ThrowsCategoryNotFoundException` | Create with invalid category | Throws `CategoryNotFoundException` |
| `UpdateProductAsync_ValidData_ReturnsUpdatedProduct` | Update existing product | Returns updated product |
| `UpdateProductAsync_NonExistentId_ThrowsProductNotFoundException` | Update non-existent product | Throws `ProductNotFoundException` |
| `UpdateProductAsync_DuplicateSlug_ThrowsDuplicateProductSlugException` | Update with existing slug | Throws `DuplicateProductSlugException` |
| `DeleteProductAsync_ExistingId_DeletesProduct` | Delete existing product | Product is deleted |
| `DeleteProductAsync_NonExistentId_ThrowsProductNotFoundException` | Delete non-existent product | Throws `ProductNotFoundException` |

---

### 3. CategoryService Tests

**File:** `Unit/Services/CategoryServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetAllCategoriesAsync_ReturnsAllCategories` | Get all categories | Returns category list |
| `GetCategoryByIdAsync_ExistingId_ReturnsCategory` | Get category by valid ID | Returns category |
| `GetCategoryByIdAsync_NonExistentId_ThrowsCategoryNotFoundException` | Get category by invalid ID | Throws `CategoryNotFoundException` |
| `GetCategoryBySlugAsync_ExistingSlug_ReturnsCategory` | Get category by valid slug | Returns category |
| `GetCategoryBySlugAsync_NonExistentSlug_ThrowsCategoryNotFoundException` | Get category by invalid slug | Throws `CategoryNotFoundException` |
| `CreateCategoryAsync_ValidData_ReturnsCreatedCategory` | Create category with valid data | Returns created category |
| `CreateCategoryAsync_DuplicateSlug_ThrowsDuplicateCategorySlugException` | Create with existing slug | Throws `DuplicateCategorySlugException` |
| `UpdateCategoryAsync_ValidData_ReturnsUpdatedCategory` | Update existing category | Returns updated category |
| `UpdateCategoryAsync_NonExistentId_ThrowsCategoryNotFoundException` | Update non-existent category | Throws `CategoryNotFoundException` |
| `DeleteCategoryAsync_ExistingId_DeletesCategory` | Delete existing category | Category is deleted |
| `DeleteCategoryAsync_NonExistentId_ThrowsCategoryNotFoundException` | Delete non-existent category | Throws `CategoryNotFoundException` |
| `DeleteCategoryAsync_CategoryWithProducts_ThrowsCategoryHasProductsException` | Delete category with products | Throws `CategoryHasProductsException` |

---

### 4. CartService Tests

**File:** `Unit/Services/CartServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetOrCreateCartAsync_NewUser_CreatesNewCart` | Get cart for new user | Creates and returns new cart |
| `GetOrCreateCartAsync_ExistingUser_ReturnsExistingCart` | Get cart for existing user | Returns existing cart |
| `GetOrCreateCartAsync_SessionId_CreatesGuestCart` | Get cart with session ID | Creates guest cart |
| `GetCartAsync_ExistingUser_ReturnsCart` | Get user's cart | Returns cart with items |
| `AddToCartAsync_ValidProduct_AddsToCart` | Add product to cart | Product added to cart |
| `AddToCartAsync_ExistingProduct_IncreasesQuantity` | Add existing product again | Quantity increased |
| `AddToCartAsync_InvalidProduct_ThrowsProductNotFoundException` | Add non-existent product | Throws `ProductNotFoundException` |
| `AddToCartAsync_InactiveProduct_ThrowsProductNotAvailableException` | Add inactive product | Throws `ProductNotAvailableException` |
| `AddToCartAsync_ExceedsStock_ThrowsInsufficientStockException` | Add more than available stock | Throws `InsufficientStockException` |
| `UpdateCartItemAsync_ValidQuantity_UpdatesItem` | Update cart item quantity | Quantity updated |
| `UpdateCartItemAsync_ZeroQuantity_RemovesItem` | Update to zero quantity | Item removed from cart |
| `UpdateCartItemAsync_InvalidItemId_ThrowsCartItemNotFoundException` | Update non-existent item | Throws `CartItemNotFoundException` |
| `UpdateCartItemAsync_ExceedsStock_ThrowsInsufficientStockException` | Update beyond stock | Throws `InsufficientStockException` |
| `RemoveFromCartAsync_ExistingItem_RemovesItem` | Remove item from cart | Item removed |
| `RemoveFromCartAsync_InvalidItemId_ThrowsCartItemNotFoundException` | Remove non-existent item | Throws `CartItemNotFoundException` |
| `ClearCartAsync_ExistingCart_ClearsAllItems` | Clear cart | All items removed |
| `ValidateCartAsync_ValidCart_NoExceptions` | Validate cart with available products | No exceptions thrown |
| `ValidateCartAsync_InsufficientStock_ThrowsInsufficientStockException` | Validate with low stock items | Throws `InsufficientStockException` |

---

### 5. OrderService Tests

**File:** `Unit/Services/OrderServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `CreateOrderAsync_ValidData_CreatesOrder` | Create order from cart | Returns created order |
| `CreateOrderAsync_EmptyCart_ThrowsEmptyCartException` | Create order with empty cart | Throws `EmptyCartException` |
| `CreateOrderAsync_InsufficientStock_ThrowsInsufficientStockException` | Create order with low stock | Throws `InsufficientStockException` |
| `CreateOrderAsync_WithPromoCode_AppliesDiscount` | Create order with promo code | Discount applied correctly |
| `CreateOrderAsync_InvalidPromoCode_ThrowsInvalidPromoCodeException` | Create with invalid promo | Throws `InvalidPromoCodeException` |
| `GetOrderByIdAsync_ExistingId_ReturnsOrder` | Get order by valid ID | Returns order details |
| `GetOrderByIdAsync_NonExistentId_ReturnsNull` | Get non-existent order | Returns null |
| `GetOrderByNumberAsync_ExistingNumber_ReturnsOrder` | Get order by order number | Returns order details |
| `GetUserOrdersAsync_ValidUser_ReturnsUserOrders` | Get user's orders | Returns paginated orders |
| `UpdateOrderStatusAsync_ValidStatus_UpdatesOrder` | Update order status | Status updated |
| `UpdateOrderStatusAsync_InvalidStatus_ThrowsInvalidOrderStatusException` | Update with invalid status | Throws `InvalidOrderStatusException` |
| `UpdateOrderStatusAsync_NonExistentOrder_ThrowsOrderNotFoundException` | Update non-existent order | Throws `OrderNotFoundException` |
| `CancelOrderAsync_PendingOrder_CancelsOrder` | Cancel pending order | Order cancelled |
| `CancelOrderAsync_ShippedOrder_ReturnsFalse` | Cancel shipped order | Returns false (cannot cancel) |
| `CancelOrderAsync_NonExistentOrder_ThrowsOrderNotFoundException` | Cancel non-existent order | Throws `OrderNotFoundException` |
| `GetAllOrdersAsync_AdminRequest_ReturnsAllOrders` | Get all orders (admin) | Returns paginated orders |

---

### 6. ReviewService Tests

**File:** `Unit/Services/ReviewServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetProductReviewsAsync_ExistingProduct_ReturnsReviews` | Get reviews for product | Returns paginated reviews |
| `GetProductReviewsAsync_NonExistentProduct_ThrowsProductNotFoundException` | Get reviews for invalid product | Throws `ProductNotFoundException` |
| `GetReviewByIdAsync_ExistingId_ReturnsReview` | Get review by ID | Returns review |
| `GetReviewByIdAsync_NonExistentId_ThrowsReviewNotFoundException` | Get non-existent review | Throws `ReviewNotFoundException` |
| `CreateReviewAsync_ValidData_CreatesReview` | Create valid review | Returns created review |
| `CreateReviewAsync_DuplicateReview_ThrowsDuplicateReviewException` | Create duplicate review | Throws `DuplicateReviewException` |
| `CreateReviewAsync_InvalidRating_ThrowsInvalidRatingException` | Create with invalid rating | Throws `InvalidRatingException` |
| `CreateReviewAsync_EmptyComment_ThrowsEmptyReviewCommentException` | Create with empty comment | Throws `EmptyReviewCommentException` |
| `UpdateReviewAsync_ValidData_UpdatesReview` | Update existing review | Returns updated review |
| `UpdateReviewAsync_NotOwner_ThrowsUnauthorizedException` | Update other's review | Throws `UnauthorizedException` |
| `UpdateReviewAsync_ExpiredTime_ThrowsReviewUpdateTimeExpiredException` | Update old review | Throws `ReviewUpdateTimeExpiredException` |
| `DeleteReviewAsync_ExistingReview_DeletesReview` | Delete existing review | Review deleted |
| `DeleteReviewAsync_NotOwner_ThrowsUnauthorizedException` | Delete other's review | Throws `UnauthorizedException` |

---

### 7. WishlistService Tests

**File:** `Unit/Services/WishlistServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetWishlistAsync_ExistingUser_ReturnsWishlist` | Get user's wishlist | Returns wishlist items |
| `AddToWishlistAsync_ValidProduct_AddsItem` | Add product to wishlist | Product added |
| `AddToWishlistAsync_DuplicateItem_ThrowsDuplicateWishlistItemException` | Add existing item | Throws `DuplicateWishlistItemException` |
| `AddToWishlistAsync_InvalidProduct_ThrowsProductNotFoundException` | Add non-existent product | Throws `ProductNotFoundException` |
| `RemoveFromWishlistAsync_ExistingItem_RemovesItem` | Remove item from wishlist | Item removed |
| `RemoveFromWishlistAsync_InvalidItem_ThrowsWishlistItemNotFoundException` | Remove non-existent item | Throws `WishlistItemNotFoundException` |
| `ClearWishlistAsync_ExistingWishlist_ClearsAll` | Clear wishlist | All items removed |
| `IsInWishlistAsync_ExistingItem_ReturnsTrue` | Check existing item | Returns true |
| `IsInWishlistAsync_NonExistingItem_ReturnsFalse` | Check non-existing item | Returns false |

---

### 8. PaymentService Tests

**File:** `Unit/Services/PaymentServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetPaymentMethodsAsync_ReturnsAvailableMethods` | Get payment methods | Returns available methods |
| `ProcessPaymentAsync_ValidPayment_ReturnsSuccess` | Process valid payment | Returns success response |
| `ProcessPaymentAsync_UnsupportedMethod_ThrowsUnsupportedPaymentMethodException` | Process unsupported method | Throws `UnsupportedPaymentMethodException` |
| `ProcessPaymentAsync_AmountMismatch_ThrowsPaymentAmountMismatchException` | Process with wrong amount | Throws `PaymentAmountMismatchException` |
| `ProcessPaymentAsync_InvalidOrderId_ThrowsOrderNotFoundException` | Process for invalid order | Throws `OrderNotFoundException` |
| `GetPaymentStatusAsync_ExistingPayment_ReturnsStatus` | Get payment status | Returns payment status |
| `GetPaymentStatusAsync_NonExistentPayment_ThrowsNoPaymentFoundException` | Get non-existent payment | Throws `NoPaymentFoundException` |
| `ProcessRefundAsync_PaidOrder_ProcessesRefund` | Refund paid order | Refund processed |
| `ProcessRefundAsync_NotPaidOrder_ThrowsInvalidRefundException` | Refund unpaid order | Throws `InvalidRefundException` |
| `ProcessRefundAsync_NonExistentOrder_ThrowsOrderNotFoundException` | Refund invalid order | Throws `OrderNotFoundException` |

---

### 9. PromoCodeService Tests

**File:** `Unit/Services/PromoCodeServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetAllAsync_ReturnsAllPromoCodes` | Get all promo codes | Returns paginated codes |
| `GetAllAsync_WithSearchFilter_ReturnsMatching` | Search promo codes | Returns matching codes |
| `GetAllAsync_WithActiveFilter_ReturnsActiveOnly` | Filter active codes | Returns only active |
| `GetByIdAsync_ExistingId_ReturnsPromoCode` | Get promo code by ID | Returns promo code |
| `GetByIdAsync_NonExistentId_ReturnsNull` | Get non-existent code | Returns null |
| `GetByCodeAsync_ExistingCode_ReturnsPromoCode` | Get by code string | Returns promo code |
| `CreateAsync_ValidData_CreatesPromoCode` | Create valid promo code | Returns created code |
| `CreateAsync_DuplicateCode_ThrowsPromoCodeAlreadyExistsException` | Create duplicate code | Throws `PromoCodeAlreadyExistsException` |
| `CreateAsync_InvalidPercentage_ThrowsInvalidPromoCodeConfigurationException` | Create with percentage > 100 | Throws `InvalidPromoCodeConfigurationException` |
| `CreateAsync_NegativeValue_ThrowsInvalidPromoCodeConfigurationException` | Create with negative value | Throws `InvalidPromoCodeConfigurationException` |
| `CreateAsync_InvalidDateRange_ThrowsInvalidPromoCodeConfigurationException` | Create with start > end | Throws `InvalidPromoCodeConfigurationException` |
| `UpdateAsync_ValidData_UpdatesPromoCode` | Update existing code | Returns updated code |
| `UpdateAsync_NonExistentId_ThrowsPromoCodeNotFoundException` | Update non-existent code | Throws `PromoCodeNotFoundException` |
| `UpdateAsync_DuplicateCode_ThrowsPromoCodeAlreadyExistsException` | Update to existing code | Throws `PromoCodeAlreadyExistsException` |
| `DeactivateAsync_ExistingCode_DeactivatesCode` | Deactivate promo code | Code deactivated |
| `DeactivateAsync_NonExistentId_ThrowsPromoCodeNotFoundException` | Deactivate non-existent | Throws `PromoCodeNotFoundException` |
| `ValidatePromoCodeAsync_ValidCode_ReturnsSuccess` | Validate valid code | Returns valid with discount |
| `ValidatePromoCodeAsync_NonExistentCode_ReturnsInvalid` | Validate non-existent code | Returns invalid response |
| `ValidatePromoCodeAsync_InactiveCode_ReturnsInvalid` | Validate inactive code | Returns invalid response |
| `ValidatePromoCodeAsync_ExpiredCode_ReturnsInvalid` | Validate expired code | Returns invalid response |
| `ValidatePromoCodeAsync_NotYetActive_ReturnsInvalid` | Validate future code | Returns invalid response |
| `ValidatePromoCodeAsync_MaxUsesReached_ReturnsInvalid` | Validate exhausted code | Returns invalid response |
| `ValidatePromoCodeAsync_BelowMinAmount_ReturnsInvalid` | Validate below minimum | Returns invalid response |
| `ValidatePromoCodeAsync_PercentageDiscount_CalculatesCorrectly` | Validate percentage discount | Correct discount amount |
| `ValidatePromoCodeAsync_FixedDiscount_CalculatesCorrectly` | Validate fixed discount | Correct discount amount |
| `ValidatePromoCodeAsync_MaxDiscountCap_AppliesCap` | Validate with max cap | Cap applied correctly |
| `IncrementUsedCountAsync_ValidCode_IncrementsCount` | Increment usage count | Count increased by 1 |
| `IncrementUsedCountAsync_AtMaxUses_ThrowsPromoCodeUsageLimitReachedException` | Increment at limit | Throws `PromoCodeUsageLimitReachedException` |

---

### 10. InventoryService Tests

**File:** `Unit/Services/InventoryServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetProductInventoryAsync_ExistingProduct_ReturnsInventory` | Get product inventory | Returns inventory info |
| `GetProductInventoryAsync_NonExistentProduct_ThrowsProductNotFoundException` | Get non-existent product | Throws `ProductNotFoundException` |
| `CheckAvailabilityAsync_AvailableStock_ReturnsTrue` | Check available product | Returns available = true |
| `CheckAvailabilityAsync_InsufficientStock_ReturnsFalse` | Check low stock product | Returns available = false |
| `AdjustInventoryAsync_ValidAdjustment_UpdatesStock` | Adjust inventory | Stock updated |
| `AdjustInventoryAsync_NegativeResult_ThrowsInsufficientStockException` | Adjust below zero | Throws `InsufficientStockException` |
| `GetInventoryLogsAsync_ExistingProduct_ReturnsLogs` | Get inventory logs | Returns log history |
| `SetLowStockThresholdAsync_ValidThreshold_SetsThreshold` | Set low stock threshold | Threshold updated |
| `GetLowStockProductsAsync_ReturnsLowStockItems` | Get low stock products | Returns products below threshold |

---

### 11. UserService Tests

**File:** `Unit/Services/UserServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetUserProfileAsync_ExistingUser_ReturnsProfile` | Get user profile | Returns profile |
| `GetUserProfileAsync_NonExistentUser_ThrowsUserNotFoundException` | Get non-existent user | Throws `UserNotFoundException` |
| `UpdateUserProfileAsync_ValidData_UpdatesProfile` | Update profile | Returns updated profile |
| `GetUserAddressesAsync_ReturnsUserAddresses` | Get user addresses | Returns address list |
| `AddAddressAsync_ValidData_AddsAddress` | Add new address | Returns created address |
| `UpdateAddressAsync_ValidData_UpdatesAddress` | Update address | Returns updated address |
| `DeleteAddressAsync_ExistingAddress_DeletesAddress` | Delete address | Address deleted |
| `GetAllUsersAsync_AdminRequest_ReturnsAllUsers` | Get all users (admin) | Returns paginated users |
| `UpdateUserRoleAsync_ValidRole_UpdatesRole` | Update user role | Role updated |

---

### 12. DashboardService Tests

**File:** `Unit/Services/DashboardServiceTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `GetDashboardStatsAsync_ReturnsAggregatedStats` | Get dashboard stats | Returns all statistics |
| `GetRevenueChartDataAsync_ReturnsRevenueData` | Get revenue chart | Returns chart data |
| `GetOrderStatusDistributionAsync_ReturnsDistribution` | Get order status dist | Returns distribution |
| `GetTopSellingProductsAsync_ReturnsTopProducts` | Get top products | Returns top sellers |
| `GetRecentOrdersAsync_ReturnsRecentOrders` | Get recent orders | Returns recent orders |
| `GetNewCustomersCountAsync_ReturnsCount` | Get new customers | Returns count |

---

### 13. Exception Tests

**File:** `Unit/Exceptions/ExceptionTests.cs`

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `NotFoundException_SetsCorrectMessage` | Create NotFoundException | Correct message set |
| `BadRequestException_SetsCorrectMessage` | Create BadRequestException | Correct message set |
| `ConflictException_SetsCorrectMessage` | Create ConflictException | Correct message set |
| `UnauthorizedException_SetsCorrectMessage` | Create UnauthorizedException | Correct message set |
| `ProductNotFoundException_SetsProductId` | Create ProductNotFoundException | Contains product ID |
| `CategoryNotFoundException_SetsCategoryId` | Create CategoryNotFoundException | Contains category ID |
| `OrderNotFoundException_SetsOrderId` | Create OrderNotFoundException | Contains order ID |
| `UserNotFoundException_SetsUserId` | Create UserNotFoundException | Contains user ID |
| `DuplicateEmailException_SetsEmail` | Create DuplicateEmailException | Contains email |
| `InvalidCredentialsException_SetsCorrectMessage` | Create InvalidCredentialsException | Correct message |
| `AllExceptions_InheritFromCorrectBase` | Check exception hierarchy | Correct inheritance |

---

## Integration Tests

### Repository Integration Tests

**Base Class:** `IntegrationTestBase.cs`

```csharp
public abstract class IntegrationTestBase : IDisposable
{
    protected AppDbContext DbContext { get; }
    protected IUnitOfWork UnitOfWork { get; }

    protected IntegrationTestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        DbContext = new AppDbContext(options);
        UnitOfWork = new UnitOfWork(DbContext);
    }

    public void Dispose() => DbContext.Dispose();
}
```

### 1. ProductRepository Integration Tests

**File:** `Integration/Repositories/ProductRepositoryTests.cs`

| Test Case | Description |
|-----------|-------------|
| `GetByIdAsync_WithIncludes_LoadsRelatedEntities` | Verify eager loading |
| `GetByCategoryAsync_ReturnsCorrectProducts` | Filter by category |
| `SearchAsync_ReturnsMatchingProducts` | Full-text search |
| `GetWithPaginationAsync_ReturnsPaginatedResults` | Pagination works |
| `AddAsync_PersistsToDatabase` | Entity saved to DB |
| `UpdateAsync_UpdatesDatabase` | Entity updated in DB |
| `DeleteAsync_RemovesFromDatabase` | Entity removed from DB |

### 2. OrderRepository Integration Tests

**File:** `Integration/Repositories/OrderRepositoryTests.cs`

| Test Case | Description |
|-----------|-------------|
| `GetByIdAsync_IncludesOrderItems` | Verify includes |
| `GetByUserIdAsync_ReturnsUserOrders` | Filter by user |
| `GetByOrderNumberAsync_ReturnsCorrectOrder` | Find by order number |
| `GetWithDateRangeAsync_FiltersCorrectly` | Date range filter |

### 3. Controller Integration Tests

**File:** `Integration/Controllers/AuthControllerTests.cs`

| Test Case | Description |
|-----------|-------------|
| `Register_ValidData_Returns201` | Successful registration |
| `Register_DuplicateEmail_Returns409` | Conflict response |
| `Login_ValidCredentials_Returns200WithToken` | Successful login |
| `Login_InvalidCredentials_Returns401` | Unauthorized response |
| `RefreshToken_ValidToken_Returns200` | Token refresh works |

**File:** `Integration/Controllers/ProductsControllerTests.cs`

| Test Case | Description |
|-----------|-------------|
| `GetProducts_NoAuth_Returns200` | Public endpoint accessible |
| `GetProductBySlug_ExistingProduct_Returns200` | Product retrieved |
| `GetProductBySlug_NonExistent_Returns404` | Not found response |
| `CreateProduct_AdminRole_Returns201` | Admin can create |
| `CreateProduct_NoAuth_Returns401` | Requires authentication |
| `CreateProduct_UserRole_Returns403` | Requires admin role |
| `UpdateProduct_ValidData_Returns200` | Product updated |
| `DeleteProduct_AdminRole_Returns204` | Product deleted |

**File:** `Integration/Controllers/OrdersControllerTests.cs`

| Test Case | Description |
|-----------|-------------|
| `GetOrders_AuthenticatedUser_ReturnsUserOrders` | User sees own orders |
| `GetOrderById_OwnOrder_Returns200` | User can view own order |
| `GetOrderById_OtherUserOrder_Returns404` | Cannot view other's order |
| `CreateOrder_WithValidCart_Returns201` | Order created |
| `CreateOrder_EmptyCart_Returns400` | Empty cart rejected |
| `CancelOrder_PendingOrder_Returns200` | Order cancelled |
| `CancelOrder_ShippedOrder_Returns400` | Cannot cancel shipped |

---

## End-to-End Tests

### 1. Authentication Flow Tests

**File:** `EndToEnd/AuthenticationFlowTests.cs`

| Test Case | Description |
|-----------|-------------|
| `CompleteRegistrationFlow` | Register → Verify Email → Login |
| `PasswordResetFlow` | Request Reset → Reset Password → Login |
| `TokenRefreshFlow` | Login → Wait → Refresh Token → Access Protected Resource |
| `LogoutFlow` | Login → Access Resource → Invalidate Token |

### 2. Shopping Flow Tests

**File:** `EndToEnd/ShoppingFlowTests.cs`

| Test Case | Description |
|-----------|-------------|
| `BrowseAndAddToCart` | Browse Products → Add to Cart → View Cart |
| `GuestToUserCartMigration` | Add as Guest → Login → Cart Preserved |
| `ApplyPromoCode` | Add Items → Apply Code → Verify Discount |
| `InvalidPromoCodeRejection` | Apply Invalid Code → Error Shown → Order Total Unchanged |
| `CartQuantityUpdate` | Add Item → Update Quantity → Total Updates |
| `CartItemRemoval` | Add Items → Remove One → Cart Updated |
| `WishlistManagement` | Add to Wishlist → Remove → Move to Cart |

### 3. Order Flow Tests

**File:** `EndToEnd/OrderFlowTests.cs`

| Test Case | Description |
|-----------|-------------|
| `CompleteOrderFlow` | Cart → Checkout → Payment → Order Confirmation |
| `OrderWithPromoCode` | Cart → Apply Code → Checkout → Discount Applied |
| `OrderCancellation` | Create Order → Cancel → Refund Initiated |
| `OrderStatusTracking` | Create → View Status → Status Updates |
| `GuestCheckout` | Add to Cart → Guest Checkout → Email Confirmation |
| `InventoryDeduction` | Order → Stock Reduced → Verify Inventory |
| `PaymentFailureRecovery` | Checkout → Payment Fails → Retry → Success |
| `MultiplePaymentMethods` | Checkout with Card → Checkout with PayPal |

### 4. Admin Flow Tests

**File:** `EndToEnd/AdminFlowTests.cs`

| Test Case | Description |
|-----------|-------------|
| `ProductManagement` | Create → Update → Deactivate Product |
| `CategoryManagement` | Create → Update → Delete Category |
| `OrderManagement` | View Orders → Update Status → Ship Order |
| `PromoCodeManagement` | Create → Validate → Deactivate Code |
| `InventoryManagement` | Adjust Stock → View Logs → Low Stock Alert |
| `UserManagement` | View Users → Update Role → Deactivate User |
| `DashboardAccess` | View Stats → Filter by Date → Export Data |

---

## Test Data Management

### Test Data Factory

**File:** `Helpers/TestDataFactory.cs`

```csharp
public static class TestDataFactory
{
    private static readonly Faker _faker = new();

    public static User CreateUser(string? email = null, UserRole role = UserRole.Customer)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            Role = role,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Product CreateProduct(Guid? categoryId = null, decimal? price = null)
    {
        var name = _faker.Commerce.ProductName();
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            Description = _faker.Commerce.ProductDescription(),
            Price = price ?? _faker.Random.Decimal(10, 1000),
            Stock = _faker.Random.Int(0, 100),
            CategoryId = categoryId ?? Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Category CreateCategory(string? name = null)
    {
        var categoryName = name ?? _faker.Commerce.Categories(1)[0];
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            Slug = categoryName.ToLower().Replace(" ", "-"),
            Description = _faker.Lorem.Sentence(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static PromoCode CreatePromoCode(
        string? code = null,
        string discountType = "percentage",
        decimal discountValue = 10)
    {
        return new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code ?? _faker.Random.AlphaNumeric(8).ToUpper(),
            DiscountType = discountType,
            DiscountValue = discountValue,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Order CreateOrder(Guid userId, decimal total = 100)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{_faker.Random.Int(1000, 9999)}",
            UserId = userId,
            TotalAmount = total,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

### Mock Helpers

**File:** `Helpers/MockHelpers.cs`

```csharp
public static class MockHelpers
{
    public static Mock<IUnitOfWork> CreateMockUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync()).Returns(Task.FromResult(1));
        mock.Setup(u => u.BeginTransactionAsync())
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());
        return mock;
    }

    public static Mock<IRepository<T>> CreateMockRepository<T>(List<T>? items = null)
        where T : BaseEntity
    {
        items ??= new List<T>();
        var mock = new Mock<IRepository<T>>();

        mock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(items);
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => items.FirstOrDefault(i => i.Id == id));
        mock.Setup(r => r.AddAsync(It.IsAny<T>()))
            .Callback<T>(item => items.Add(item));
        mock.Setup(r => r.UpdateAsync(It.IsAny<T>()));
        mock.Setup(r => r.DeleteAsync(It.IsAny<T>()))
            .Callback<T>(item => items.Remove(item));

        return mock;
    }

    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}
```

---

## Coverage Goals

### Target Coverage by Layer

| Layer | Target Coverage | Priority |
|-------|-----------------|----------|
| Application Services | 90%+ | Critical |
| Domain Entities | 85%+ | High |
| Controllers | 80%+ | High |
| Repositories | 75%+ | Medium |
| Exception Handling | 100% | Critical |

### Critical Paths (Must Have 100% Coverage)

1. **Authentication Flow**
   - Registration
   - Login/Logout
   - Token refresh
   - Password reset

2. **Order Processing**
   - Cart to order conversion
   - Payment processing
   - Inventory deduction
   - Order status transitions

3. **Promo Code Validation**
   - All validation rules
   - Discount calculation
   - Usage tracking

4. **Exception Handling**
   - All domain exceptions thrown correctly
   - Global exception handler maps to HTTP status codes

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1)

- [ ] Set up test project with required packages
- [ ] Create test folder structure
- [ ] Implement `TestDataFactory`
- [ ] Implement `MockHelpers`
- [ ] Create base test classes

### Phase 2: Unit Tests - Critical Services (Week 2)

- [ ] AuthService tests (19 tests)
- [ ] CartService tests (17 tests)
- [ ] OrderService tests (16 tests)
- [ ] PromoCodeService tests (28 tests)
- [ ] PaymentService tests (10 tests)

### Phase 3: Unit Tests - Remaining Services (Week 3)

- [ ] ProductService tests (19 tests)
- [ ] CategoryService tests (12 tests)
- [ ] ReviewService tests (13 tests)
- [ ] WishlistService tests (9 tests)
- [ ] InventoryService tests (9 tests)
- [ ] UserService tests (9 tests)
- [ ] DashboardService tests (6 tests)
- [ ] Exception tests (11 tests)

### Phase 4: Integration Tests (Week 4)

- [ ] Repository integration tests
- [ ] Controller integration tests with WebApplicationFactory
- [ ] Service + Repository integration tests

### Phase 5: End-to-End Tests (Week 5)

- [ ] Authentication flow tests
- [ ] Shopping flow tests
- [ ] Order flow tests
- [ ] Admin flow tests

### Phase 6: CI/CD Integration (Week 6)

- [ ] Configure test execution in CI pipeline
- [ ] Set up code coverage reporting
- [ ] Configure test parallelization
- [ ] Add test result badges to README

---

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test src/backend/ECommerce.Tests

# Run with coverage
dotnet test src/backend/ECommerce.Tests --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test src/backend/ECommerce.Tests --filter "Category=Unit"

# Run specific test class
dotnet test src/backend/ECommerce.Tests --filter "FullyQualifiedName~AuthServiceTests"

# Run with verbose output
dotnet test src/backend/ECommerce.Tests --verbosity detailed
```

### Visual Studio

1. Open Test Explorer (Ctrl+E, T)
2. Build solution
3. Run all tests or select specific tests

### VS Code

1. Install ".NET Core Test Explorer" extension
2. Tests appear in Testing sidebar
3. Click run button on individual tests or folders

---

## Test Naming Convention

```
[MethodName]_[Scenario]_[ExpectedResult]
```

Examples:
- `CreateOrderAsync_EmptyCart_ThrowsEmptyCartException`
- `ValidatePromoCodeAsync_ExpiredCode_ReturnsInvalid`
- `GetProductBySlug_ExistingProduct_ReturnsProduct`

---

## Continuous Integration

### GitHub Actions Workflow

```yaml
name: Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Restore dependencies
      run: dotnet restore src/backend

    - name: Build
      run: dotnet build src/backend --no-restore

    - name: Test with coverage
      run: dotnet test src/backend/ECommerce.Tests --no-build --collect:"XPlat Code Coverage"

    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v4
      with:
        file: ./src/backend/ECommerce.Tests/TestResults/**/coverage.cobertura.xml
```

---

## Appendix: Test Count Summary

| Category | Test Count |
|----------|------------|
| AuthService | 19 |
| ProductService | 19 |
| CategoryService | 12 |
| CartService | 17 |
| OrderService | 16 |
| ReviewService | 13 |
| WishlistService | 9 |
| PaymentService | 10 |
| PromoCodeService | 28 |
| InventoryService | 9 |
| UserService | 9 |
| DashboardService | 6 |
| Exceptions | 11 |
| **Unit Tests Total** | **178** |
| Repository Integration | 15 |
| Controller Integration | 20 |
| Service Integration | 10 |
| **Integration Tests Total** | **45** |
| Authentication E2E | 4 |
| Shopping E2E | 7 |
| Order E2E | 8 |
| Admin E2E | 7 |
| **E2E Tests Total** | **26** |
| **GRAND TOTAL** | **249** |
