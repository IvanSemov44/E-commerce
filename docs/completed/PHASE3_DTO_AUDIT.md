Phase 3 — DTO Consolidation Audit

Scope
- Identify DTOs that should be centralized under `ECommerce.Application.DTOs.Common` (or equivalent) and list missing validators to add (prioritized by risk).

High-priority consolidation targets (safe, non-breaking)
- `AddressDto` (currently in `DTOs/Orders`) → move to `DTOs/Common/AddressDto.cs` and update:
  - `MappingProfile` (replace `ECommerce.Application.DTOs.Orders.AddressDto` usages)
  - All services/controllers referencing `AddressDto`
- Shared infrastructural DTOs (ensure they live in `DTOs/Common`):
  - `PaginatedResult<T>` (already present under `DTOs/Common`) — ensure controllers use it consistently.
  - `ErrorDetails` and `HealthCheckResponseDto` (move to `DTOs/Common` if not already centralized).
- Consider centralizing other small shared DTO types used across domains:
  - `CategoryDto` is already at root `DTOs` — keep or move to `DTOs/Common` consistently.
  - `Address`-related types used by Orders/Users/Checkout should reference the single `AddressDto`.

Immediate non-breaking renames / normalization (recommendations)
- Keep request/response role separation but postpone large rename sweep. For now:
  - Prefer `*RequestDto` for incoming request DTOs and `*ResponseDto` for outgoing shapes.
  - Example candidates to rename in a follow-up PR: `AddToCartDto` → `AddToCartRequestDto`, `UserDto` → `UserResponseDto`.

Exact missing validators (prioritised)
1. Security (high) — must add validators first
   - `RefreshTokenRequest` (Auth/AuthRequestDtos.cs)
   - `ForgotPasswordRequest` (Auth/AuthRequestDtos.cs)
   - `ResetPasswordRequest` (Auth/AuthRequestDtos.cs)
   - `ChangePasswordRequest` (Auth/AuthRequestDtos.cs)
   - `VerifyEmailRequest` (Auth/AuthRequestDtos.cs)
2. Payments (high — financial risk)
   - `ProcessPaymentDto` (Payments/PaymentDtos.cs)
   - `RefundPaymentDto` (Payments/PaymentDtos.cs)
3. Category & Catalog (medium)
   - `CreateCategoryDto` (DTOs/CategoryDto.cs)
   - `UpdateCategoryDto` (DTOs/CategoryDto.cs)
4. Inventory (medium)
   - `AdjustStockRequest` (if present in `DTOs/Inventory`) — validate amounts, productId
   - `StockCheckRequest` (if present)
5. Orders & Reviews (medium)
   - `UpdateOrderStatusDto` (DTOs/Orders/OrderDtos.cs)
   - `CreateReviewDto`, `UpdateReviewDto` (DTOs/Reviews)
6. User profile (medium)
   - `UpdateProfileDto` (DTOs/Users)
7. Promo codes & wishlist
   - `ValidatePromoCodeDto` (PromoCodes) — ensure presence of validator
   - `UpdatePromoCodeDto` (PromoCodes)
   - `AddToWishlistDto` (Wishlist/WishlistDtos.cs)

Validators already present (non-exhaustive list found)
- `AddToCartDtoValidator`, `UpdateCartItemDtoValidator` (Cart)
- `CreateOrderDtoValidator`, `CreateOrderItemDtoValidator`, `AddressDtoValidator` (Orders)
- `ProductQueryDtoValidator`, `CreateProductDtoValidator`, `UpdateProductDtoValidator` (Products)
- `CreatePromoCodeDtoValidator` (PromoCodes)
- `RegisterDtoValidator`, `LoginDtoValidator` (Auth)

Notes & approach
- Priority: Add security and payments validators first (non-breaking, contained changes). Then address Category/Inventory/Orders validators.
- Centralize `AddressDto` first — low-risk, highly beneficial. Implement via:
  1. Create `ECommerce.Application.DTOs.Common/AddressDto.cs` and move class.
  2. Update `MappingProfile` to use the common `AddressDto` mapping (already exists but may require `using` changes).
  3. Update `using` targets across `OrderService`, controllers, and validators.
  4. Run `dotnet test` and fix compiler/test issues.
- For validator additions, follow existing pattern (FluentValidation) and register via `Program.cs` where `AddValidatorsFromAssemblyContaining<AddToCartDtoValidator>()` already exists.

Small, safe first tasks I can do now (pick one)
- `move-address` — Move `AddressDto` to `DTOs/Common` and update `MappingProfile` + compile/tests.
- `add-auth-validators` — Add validators for the five Auth request DTOs (Refresh/Forgot/Reset/Change/Verify) and tests.
- `report-complete` — I’ll stop here; you can pick what to run next.

Commands I will run when making changes (example)

```bash
git checkout -b phase3/dto-consolidation/move-address
# edit files
dotnet build src/backend/ECommerce.sln
dotnet test src/backend/ECommerce.sln --no-build
```

Deliverable
- `docs/PHASE3_DTO_AUDIT.md` (this file) with prioritized consolidation and missing-validator list.

Next? Reply with one of: `move-address`, `add-auth-validators`, or `report-complete`.