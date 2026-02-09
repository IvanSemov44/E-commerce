# Storefront Refactoring Plan

> **Purpose:** This document provides a step-by-step execution plan to eliminate code duplication, improve maintainability, and establish reusable patterns across the storefront application. Each phase includes exact file paths, complete code examples, and checkboxes to track progress.

---

## 📊 Progress Summary

**Current Status:** Phase 1 ✅ COMPLETED  
**Date Started:** 2026-02-05  
**Build Status:** ✅ TypeScript compilation successful (0 errors)

### Phase 1 Completion Details
- ✅ Created QueryRenderer component (`src/frontend/storefront/src/components/QueryRenderer.tsx`)
- ✅ Refactored Home.tsx to use QueryRenderer
- ✅ Refactored Products.tsx to use QueryRenderer  
- ✅ Refactored Wishlist.tsx to use QueryRenderer
- ✅ Refactored OrderHistory.tsx to use QueryRenderer
- ✅ Fixed TypeScript errors in wishlistApi.ts
- ✅ Build passed with 394.40 kB bundle size (gzip: 121.95 kB)

**Impact of Phase 1:**
- Eliminated ~15-20 lines of repeated conditional rendering per page
- Reduced 4 pages by ~70 total lines of code
- Standardized loading/error/empty state patterns
- Improved type safety with generic QueryRenderer<T>

---

## 📋 Refactoring Summary

| Issue | Impact | Solution |
|-------|--------|----------|
| Repeated loading/error/empty state logic | **HIGH** - Duplicated in 10+ components | Create `<QueryRenderer>` component |
| Pagination logic duplication | **HIGH** - Repeated in Products.tsx and lists | Create `<PaginatedView>` component |
| Form state management | **MEDIUM** - 150+ lines of duplicate form code | Create `useForm()` custom hook |
| Inline styles | **MEDIUM** - Maintenance burden, no theming | Move to CSS Modules |
| Cart item rendering | **LOW** - Repeated twice | Extract `<CartItemCard>` component |
| API hook verbosity | **LOW** - ProductDetail.tsx cluttered | Create `useProductDetails()` hook |
| Scattered constants | **LOW** - DEFAULT_PRODUCT_IMAGE duplicated | Centralize in constants.ts |

---

## 🎯 Goals

1. **DRY Principle:** Eliminate all duplicate patterns
2. **Consistency:** Standardize loading/error/empty states across all pages
3. **Maintainability:** Changes in one place propagate everywhere
4. **Performance:** Reusable components optimize re-renders
5. **Developer Experience:** Less boilerplate = faster feature development

---

## Phase 1 — Create QueryRenderer Component

**Status:** ✅ **COMPLETED**

**Problem:** Every page repeats this pattern:
```tsx
{error ? (
  <ErrorAlert message="..." />
) : isLoading ? (
  <LoadingSkeleton count={4} />
) : data?.length > 0 ? (
  <div>{...render data...}</div>
) : (
  <EmptyState title="No items" />
)}
```

**Solution:** Extract into a reusable `<QueryRenderer>` component.

### [ ] 1.1 — Create QueryRenderer Component

**File:** `src/frontend/storefront/src/components/QueryRenderer.tsx`

**Status:** ✅ COMPLETED

```tsx
import ErrorAlert from './ErrorAlert';
import LoadingSkeleton from './LoadingSkeleton';
import EmptyState from './EmptyState';

interface QueryRendererProps<T> {
  isLoading: boolean;
  error: any;
  data: T | undefined;
  isEmpty?: (data: T) => boolean;
  loadingSkeleton?: {
    count?: number;
    type?: 'card' | 'list' | 'text';
  };
  emptyState?: {
    icon?: React.ReactNode;
    title: string;
    description?: string;
    action?: React.ReactNode;
  };
  errorMessage?: string;
  children: (data: T) => React.ReactNode;
}

export default function QueryRenderer<T>({
  isLoading,
  error,
  data,
  isEmpty = (data) => !data || (Array.isArray(data) && data.length === 0),
  loadingSkeleton = { count: 4, type: 'card' },
  emptyState,
  errorMessage = 'Failed to load data. Please try again.',
  children,
}: QueryRendererProps<T>) {
  if (error) {
    return <ErrorAlert message={errorMessage} />;
  }

  if (isLoading) {
    return (
      <LoadingSkeleton
        count={loadingSkeleton.count}
        type={loadingSkeleton.type}
      />
    );
  }

  if (!data || isEmpty(data)) {
    if (!emptyState) return null;
    
    return (
      <EmptyState
        icon={emptyState.icon}
        title={emptyState.title}
        description={emptyState.description}
        action={emptyState.action}
      />
    );
  }

  return <>{children(data)}</>;
}
```

### [ ] 1.2 — Refactor Home.tsx to Use QueryRenderer

**File:** `src/frontend/storefront/src/pages/Home.tsx`

**Status:** ✅ COMPLETED

**Before:**
```tsx
{error ? (
  <ErrorAlert message="Failed to load featured products. Please try again later." />
) : isLoading ? (
  <div className={styles.grid}>
    <LoadingSkeleton count={4} type="card" />
  </div>
) : featured && featured.length > 0 ? (
  <div className={styles.grid}>
    {featured.map((product) => (
      <ProductCard key={product.id} {...product} />
    ))}
  </div>
) : (
  <EmptyState
    icon={<svg>...</svg>}
    title="No featured products available"
  />
)}
```

**After:**
```tsx
<QueryRenderer
  isLoading={isLoading}
  error={error}
  data={featured}
  errorMessage="Failed to load featured products. Please try again later."
  emptyState={{
    icon: (
      <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
      </svg>
    ),
    title: "No featured products available"
  }}
>
  {(featured) => (
    <div className={styles.grid}>
      {featured.map((product) => (
        <ProductCard
          key={product.id}
          id={product.id}
          name={product.name}
          slug={product.slug}
          price={product.price}
          compareAtPrice={product.compareAtPrice}
          imageUrl={product.images[0]?.url}
          rating={Math.round(product.averageRating)}
          reviewCount={product.reviewCount}
        />
      ))}
    </div>
  )}
</QueryRenderer>
```

**Add import:**
```tsx
import QueryRenderer from '../components/QueryRenderer';
```

### [ ] 1.3 — Refactor Products.tsx Product List Section

**File:** `src/frontend/storefront/src/pages/Products.tsx`

**Status:** ✅ COMPLETED

Replace the main product rendering block with QueryRenderer.

### [ ] 1.4 — Refactor Wishlist.tsx

**File:** `src/frontend/storefront/src/pages/Wishlist.tsx`

**Status:** ✅ COMPLETED

Apply same pattern to wishlist items.

### [ ] 1.5 — Refactor OrderHistory.tsx

**File:** `src/frontend/storefront/src/pages/OrderHistory.tsx`

**Status:** ✅ COMPLETED

Apply same pattern to order history list.

**Lines reduced:** ~50-70 lines across all pages

---

## Phase 2 — Create PaginatedView Component

**Problem:** Pagination state and controls repeated in every paginated view.

### [ ] 2.1 — Create PaginatedView Component

**File:** `src/frontend/storefront/src/components/PaginatedView.tsx`

```tsx
import { ReactNode } from 'react';
import Button from './ui/Button';
import styles from './PaginatedView.module.css';

interface PaginatedViewProps<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  renderItem: (item: T) => ReactNode;
  gridClassName?: string;
  showPageNumbers?: boolean;
}

export default function PaginatedView<T extends { id: string }>({
  items,
  totalCount,
  currentPage,
  pageSize,
  onPageChange,
  renderItem,
  gridClassName,
  showPageNumbers = true,
}: PaginatedViewProps<T>) {
  const totalPages = Math.ceil(totalCount / pageSize);
  const hasNextPage = currentPage < totalPages;
  const hasPrevPage = currentPage > 1;

  return (
    <>
      <div className={gridClassName}>
        {items.map(renderItem)}
      </div>

      <div className={styles.pagination}>
        <Button
          variant="secondary"
          onClick={() => onPageChange(currentPage - 1)}
          disabled={!hasPrevPage}
        >
          Previous
        </Button>
        
        {showPageNumbers && (
          <span className={styles.pageInfo}>
            Page {currentPage} of {totalPages}
          </span>
        )}
        
        <Button
          variant="secondary"
          onClick={() => onPageChange(currentPage + 1)}
          disabled={!hasNextPage}
        >
          Next
        </Button>
      </div>
    </>
  );
}
```

### [ ] 2.2 — Create PaginatedView.module.css

**File:** `src/frontend/storefront/src/components/PaginatedView.module.css`

```css
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin-top: 2rem;
  padding: 1rem 0;
}

.pageInfo {
  color: #475569;
  font-size: 0.875rem;
  font-weight: 500;
}
```

### [ ] 2.3 — Refactor Products.tsx to Use PaginatedView

**File:** `src/frontend/storefront/src/pages/Products.tsx`

**Before:**
```tsx
<div className={styles.grid}>
  {result.items.map((product) => (
    <ProductCard key={product.id} {...product} />
  ))}
</div>

<div className={styles.pagination}>
  <Button
    variant="secondary"
    onClick={() => setPage(Math.max(1, page - 1))}
    disabled={page === 1}
  >
    Previous
  </Button>
  <span className={styles.pageNumber}>Page {page} of {Math.ceil(result.totalCount / 12)}</span>
  <Button
    variant="secondary"
    onClick={() => setPage(page + 1)}
    disabled={!result || result.items.length < 12}
  >
    Next
  </Button>
</div>
```

**After:**
```tsx
<PaginatedView
  items={result.items}
  totalCount={result.totalCount}
  currentPage={page}
  pageSize={12}
  onPageChange={setPage}
  gridClassName={styles.grid}
  renderItem={(product) => (
    <ProductCard
      key={product.id}
      id={product.id}
      name={product.name}
      slug={product.slug}
      price={product.price}
      compareAtPrice={product.compareAtPrice}
      imageUrl={product.images[0]?.url}
      rating={Math.round(product.averageRating)}
      reviewCount={product.reviewCount}
    />
  )}
/>
```

**Lines reduced:** ~20 lines in Products.tsx

---

## Phase 3 — Create useForm Custom Hook

**Problem:** Form state management repeated in Login, Register, Checkout, Profile.

### [ ] 3.1 — Create useForm Hook

**File:** `src/frontend/storefront/src/hooks/useForm.ts`

```tsx
import { useState, ChangeEvent, FormEvent } from 'react';

interface UseFormOptions<T> {
  initialValues: T;
  validate?: (values: T) => Partial<Record<keyof T, string>>;
  onSubmit: (values: T) => void | Promise<void>;
}

export default function useForm<T extends Record<string, any>>({
  initialValues,
  validate,
  onSubmit,
}: UseFormOptions<T>) {
  const [values, setValues] = useState<T>(initialValues);
  const [errors, setErrors] = useState<Partial<Record<keyof T, string>>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setValues((prev) => ({ ...prev, [name]: value }));
    
    // Clear error for this field
    if (errors[name as keyof T]) {
      setErrors((prev) => ({ ...prev, [name]: undefined }));
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    
    // Run validation
    if (validate) {
      const validationErrors = validate(values);
      if (Object.keys(validationErrors).length > 0) {
        setErrors(validationErrors);
        return;
      }
    }

    setIsSubmitting(true);
    setErrors({});

    try {
      await onSubmit(values);
    } finally {
      setIsSubmitting(false);
    }
  };

  const reset = () => {
    setValues(initialValues);
    setErrors({});
    setIsSubmitting(false);
  };

  const setFieldValue = (name: keyof T, value: any) => {
    setValues((prev) => ({ ...prev, [name]: value }));
  };

  const setFieldError = (name: keyof T, error: string) => {
    setErrors((prev) => ({ ...prev, [name]: error }));
  };

  return {
    values,
    errors,
    isSubmitting,
    handleChange,
    handleSubmit,
    reset,
    setFieldValue,
    setFieldError,
  };
}
```

### [ ] 3.2 — Create hooks Directory

**Action:** Create directory if it doesn't exist:
```
src/frontend/storefront/src/hooks/
```

### [ ] 3.3 — Refactor Login.tsx to Use useForm

**File:** `src/frontend/storefront/src/pages/Login.tsx`

**Before:**
```tsx
const [formData, setFormData] = useState({ email: '', password: '' });
const [errors, setErrors] = useState({ email: '', password: '' });
const [isSubmitting, setIsSubmitting] = useState(false);

const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
  setFormData({ ...formData, [e.target.name]: e.target.value });
  setErrors({ ...errors, [e.target.name]: '' });
};

const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  // validation logic...
  setIsSubmitting(true);
  // submit logic...
  setIsSubmitting(false);
};
```

**After:**
```tsx
const form = useForm({
  initialValues: { email: '', password: '' },
  validate: (values) => {
    const errors: any = {};
    if (!values.email) errors.email = 'Email is required';
    if (!values.password) errors.password = 'Password is required';
    return errors;
  },
  onSubmit: async (values) => {
    try {
      const result = await login(values).unwrap();
      // success logic...
    } catch (err: any) {
      form.setFieldError('email', err?.data?.message || 'Login failed');
    }
  },
});
```

**Add import:**
```tsx
import useForm from '../hooks/useForm';
```

**Lines reduced:** ~30 lines in Login.tsx

### [ ] 3.4 — Refactor Register.tsx to Use useForm

Apply same pattern to registration form with additional fields.

### [ ] 3.5 — Refactor Profile.tsx to Use useForm

Apply to profile update form.

**Total lines reduced:** ~100+ lines across all form pages

---

## Phase 4 — Move Inline Styles to CSS Modules

**Problem:** Inline styles scattered across components make theming and maintenance difficult.

### [ ] 4.1 — Create StarRating Component

**File:** `src/frontend/storefront/src/components/StarRating.tsx`

```tsx
import styles from './StarRating.module.css';

interface StarRatingProps {
  rating: number;
  maxStars?: number;
  onRatingChange?: (rating: number) => void;
  size?: 'sm' | 'md' | 'lg';
  readonly?: boolean;
}

export default function StarRating({
  rating,
  maxStars = 5,
  onRatingChange,
  size = 'md',
  readonly = false,
}: StarRatingProps) {
  const stars = Array.from({ length: maxStars }, (_, i) => i + 1);

  return (
    <div className={`${styles.starRating} ${styles[size]}`}>
      {stars.map((star) => (
        <button
          key={star}
          type="button"
          onClick={() => !readonly && onRatingChange?.(star)}
          disabled={readonly}
          className={`${styles.star} ${star <= rating ? styles.filled : styles.empty}`}
          title={`${star} stars`}
        >
          ★
        </button>
      ))}
    </div>
  );
}
```

### [ ] 4.2 — Create StarRating.module.css

**File:** `src/frontend/storefront/src/components/StarRating.module.css`

```css
.starRating {
  display: flex;
  gap: 0.25rem;
}

.star {
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  transition: color 0.2s;
}

.star:disabled {
  cursor: default;
}

.star.filled {
  color: #ffc107;
}

.star.empty {
  color: #ddd;
}

.star:not(:disabled):hover {
  transform: scale(1.1);
}

.sm .star {
  font-size: 1rem;
}

.md .star {
  font-size: 1.5rem;
}

.lg .star {
  font-size: 2rem;
}
```

### [ ] 4.3 — Refactor ReviewForm.tsx to Use StarRating

**File:** `src/frontend/storefront/src/components/ReviewForm.tsx`

**Before:**
```tsx
<div style={{ display: 'flex', gap: '0.5rem' }}>
  {[1, 2, 3, 4, 5].map((star) => (
    <button
      key={star}
      type="button"
      onClick={() => setRating(star)}
      style={{
        background: 'none',
        border: 'none',
        fontSize: '1.5rem',
        cursor: 'pointer',
        color: star <= rating ? '#ffc107' : '#ddd',
        padding: 0,
      }}
      title={`${star} stars`}
    >
      ★
    </button>
  ))}
</div>
```

**After:**
```tsx
<StarRating
  rating={rating}
  onRatingChange={setRating}
  size="md"
/>
```

### [ ] 4.4 — Refactor ReviewList.tsx to Use StarRating

Replace star rendering with `<StarRating rating={review.rating} readonly />`.

**Lines reduced:** ~40 lines

---

## Phase 5 — Extract Cart Item Component

**Problem:** Cart item rendering duplicated in Cart.tsx and Checkout.tsx.

### [ ] 5.1 — Create CartItemCard Component

**File:** `src/frontend/storefront/src/components/CartItemCard.tsx`

```tsx
import { Link } from 'react-router-dom';
import Card from './ui/Card';
import Button from './ui/Button';
import { DEFAULT_PRODUCT_IMAGE } from '../utils/constants';
import styles from './CartItemCard.module.css';

interface CartItemCardProps {
  id: string;
  productId: string;
  slug: string;
  name: string;
  price: number;
  quantity: number;
  imageUrl?: string;
  onQuantityChange?: (productId: string, quantity: number) => void;
  onRemove?: (productId: string) => void;
  readonly?: boolean;
}

export default function CartItemCard({
  id,
  productId,
  slug,
  name,
  price,
  quantity,
  imageUrl,
  onQuantityChange,
  onRemove,
  readonly = false,
}: CartItemCardProps) {
  return (
    <Card variant="default" padding="sm">
      <div className={styles.cartItem}>
        <Link to={`/products/${slug}`} className={styles.imageLink}>
          <img
            src={imageUrl || DEFAULT_PRODUCT_IMAGE}
            alt={name}
            className={styles.image}
            onError={(e) => {
              e.currentTarget.src = DEFAULT_PRODUCT_IMAGE;
            }}
          />
        </Link>

        <div className={styles.details}>
          <Link to={`/products/${slug}`} className={styles.name}>
            {name}
          </Link>
          <div className={styles.price}>${price.toFixed(2)}</div>
        </div>

        <div className={styles.actions}>
          {!readonly && onQuantityChange && (
            <div className={styles.quantity}>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => onQuantityChange(productId, quantity - 1)}
                disabled={quantity <= 1}
              >
                −
              </Button>
              <span className={styles.quantityValue}>{quantity}</span>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => onQuantityChange(productId, quantity + 1)}
              >
                +
              </Button>
            </div>
          )}

          {readonly && (
            <div className={styles.quantityReadonly}>
              Qty: {quantity}
            </div>
          )}

          <div className={styles.subtotal}>
            ${(price * quantity).toFixed(2)}
          </div>

          {!readonly && onRemove && (
            <Button
              variant="destructive"
              size="sm"
              onClick={() => onRemove(productId)}
            >
              Remove
            </Button>
          )}
        </div>
      </div>
    </Card>
  );
}
```

### [ ] 5.2 — Create CartItemCard.module.css

**File:** `src/frontend/storefront/src/components/CartItemCard.module.css`

```css
.cartItem {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.imageLink {
  flex-shrink: 0;
}

.image {
  width: 80px;
  height: 80px;
  object-fit: cover;
  border-radius: 0.5rem;
}

.details {
  flex: 1;
  min-width: 0;
}

.name {
  font-weight: 600;
  color: #0f172a;
  text-decoration: none;
  display: block;
  margin-bottom: 0.5rem;
}

.name:hover {
  color: #1976d2;
}

.price {
  color: #475569;
  font-size: 0.875rem;
}

.actions {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-shrink: 0;
}

.quantity {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.quantityValue {
  min-width: 2rem;
  text-align: center;
  font-weight: 600;
}

.quantityReadonly {
  color: #475569;
  font-size: 0.875rem;
}

.subtotal {
  font-weight: 600;
  color: #0f172a;
  min-width: 5rem;
  text-align: right;
}

@media (max-width: 768px) {
  .cartItem {
    flex-direction: column;
    align-items: flex-start;
  }

  .actions {
    width: 100%;
    justify-content: space-between;
  }
}
```

### [ ] 5.3 — Refactor Cart.tsx to Use CartItemCard

Replace CartItem component usage with CartItemCard.

### [ ] 5.4 — Refactor Checkout.tsx to Use CartItemCard

Use CartItemCard in readonly mode for order review.

**Lines reduced:** ~80 lines

---

## Phase 6 — Create Product Details Custom Hook

**Problem:** ProductDetail.tsx has too many API hooks at the top.

### [ ] 6.1 — Create useProductDetails Hook

**File:** `src/frontend/storefront/src/hooks/useProductDetails.ts`

```tsx
import { useState } from 'react';
import { useGetProductBySlugQuery } from '../store/api/productApi';
import { useGetProductReviewsQuery } from '../store/api/reviewsApi';
import {
  useAddToWishlistMutation,
  useRemoveFromWishlistMutation,
  useCheckInWishlistQuery,
} from '../store/api/wishlistApi';
import { useAddToCartMutation } from '../store/api/cartApi';
import { useAppSelector } from '../store/hooks';

export default function useProductDetails(slug: string) {
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const [quantity, setQuantity] = useState(1);

  const {
    data: product,
    isLoading: productLoading,
    error: productError,
  } = useGetProductBySlugQuery(slug);

  const {
    data: reviews,
    isLoading: reviewsLoading,
    error: reviewsError,
    refetch: refetchReviews,
  } = useGetProductReviewsQuery(product?.id || '', {
    skip: !product?.id,
  });

  const { data: isInWishlist, refetch: refetchWishlist } = useCheckInWishlistQuery(
    product?.id || '',
    {
      skip: !product?.id || !isAuthenticated,
    }
  );

  const [addToWishlist, { isLoading: addingToWishlist }] = useAddToWishlistMutation();
  const [removeFromWishlist, { isLoading: removingFromWishlist }] =
    useRemoveFromWishlistMutation();
  const [addToCartBackend, { isLoading: addingToCartBackend }] = useAddToCartMutation();

  const toggleWishlist = async () => {
    if (!product || !isAuthenticated) return;

    try {
      if (isInWishlist) {
        await removeFromWishlist(product.id).unwrap();
      } else {
        await addToWishlist(product.id).unwrap();
      }
      await refetchWishlist();
    } catch (error) {
      console.error('Failed to toggle wishlist:', error);
    }
  };

  const addToCart = async () => {
    if (!product) return;

    try {
      if (isAuthenticated) {
        await addToCartBackend({
          productId: product.id,
          quantity,
        }).unwrap();
      } else {
        // Local cart logic
      }
    } catch (error) {
      console.error('Failed to add to cart:', error);
      throw error;
    }
  };

  return {
    product,
    productLoading,
    productError,
    reviews,
    reviewsLoading,
    reviewsError,
    refetchReviews,
    isInWishlist,
    addingToWishlist,
    removingFromWishlist,
    addingToCartBackend,
    quantity,
    setQuantity,
    toggleWishlist,
    addToCart,
    isAuthenticated,
  };
}
```

### [ ] 6.2 — Refactor ProductDetail.tsx to Use Hook

**File:** `src/frontend/storefront/src/pages/ProductDetail.tsx`

**Before (lines 1-40):**
```tsx
const { slug = '' } = useParams();
const { data: product, isLoading, error } = useGetProductBySlugQuery(slug);
const { isAuthenticated } = useAppSelector((state) => state.auth);
// ... 10+ more lines of hooks
const [quantity, setQuantity] = useState(1);
```

**After:**
```tsx
const { slug = '' } = useParams();
const {
  product,
  productLoading,
  productError,
  reviews,
  reviewsLoading,
  quantity,
  setQuantity,
  toggleWishlist,
  addToCart,
  isInWishlist,
  isAuthenticated,
  refetchReviews,
} = useProductDetails(slug);
```

**Lines reduced:** ~30 lines in ProductDetail.tsx

---

## Phase 7 — Centralize Constants

**Problem:** Constants like DEFAULT_PRODUCT_IMAGE duplicated across files.

### [ ] 7.1 — Update constants.ts

**File:** `src/frontend/storefront/src/utils/constants.ts`

```tsx
// Images
export const DEFAULT_PRODUCT_IMAGE = 'https://placehold.co/400x400/f1f5f9/64748b?text=Product';
export const DEFAULT_USER_AVATAR = 'https://placehold.co/100x100/f1f5f9/64748b?text=User';

// Pagination
export const DEFAULT_PAGE_SIZE = 12;
export const DEFAULT_PRODUCTS_PAGE_SIZE = 12;
export const DEFAULT_ORDERS_PAGE_SIZE = 10;

// Shipping
export const FREE_SHIPPING_THRESHOLD = 100;
export const STANDARD_SHIPPING_COST = 10;

// Tax
export const DEFAULT_TAX_RATE = 0.08;

// Promo codes
export const MAX_PROMO_CODE_LENGTH = 20;

// Cart
export const MAX_CART_QUANTITY = 99;
export const MIN_CART_QUANTITY = 1;

// Reviews
export const MAX_REVIEW_RATING = 5;
export const MIN_REVIEW_RATING = 1;
export const MAX_REVIEW_COMMENT_LENGTH = 1000;
```

### [ ] 7.2 — Update Imports Across All Files

Replace local constants with imports from `utils/constants.ts` in:
- ProductCard.tsx
- ProductDetail.tsx
- Cart.tsx
- Checkout.tsx
- Any other files using these values

---

## Phase 8 — Create Shared Icon Components

**Problem:** SVG icons repeated inline across components.

### [ ] 8.1 — Create Icon Components Directory

**Action:** Create `src/frontend/storefront/src/components/icons/` directory.

### [ ] 8.2 — Create Icon Components

**Files:** Create these icon components:
- `HeartIcon.tsx` (wishlist)
- `ShoppingCartIcon.tsx` (cart)
- `UserIcon.tsx` (profile)
- `SearchIcon.tsx` (search)
- `PackageIcon.tsx` (orders)
- `StarIcon.tsx` (reviews - if not using StarRating)

**Example:** `HeartIcon.tsx`
```tsx
interface IconProps {
  className?: string;
  filled?: boolean;
}

export default function HeartIcon({ className, filled = false }: IconProps) {
  return (
    <svg
      className={className}
      fill={filled ? 'currentColor' : 'none'}
      stroke="currentColor"
      viewBox="0 0 24 24"
      xmlns="http://www.w3.org/2000/svg"
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth={2}
        d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
      />
    </svg>
  );
}
```

### [ ] 8.3 — Replace Inline SVGs

Update Header, ProductCard, EmptyState components to use icon components.

---

## Phase 9 — Testing & Validation

### [ ] 9.1 — Manual Testing Checklist

- [ ] Home page loads featured products
- [ ] Products page pagination works
- [ ] Wishlist add/remove updates heart icon
- [ ] Cart quantity changes work
- [ ] Login form validation works
- [ ] Register form validation works
- [ ] Profile update form works
- [ ] Checkout form works
- [ ] Product details loads correctly
- [ ] Reviews display with star ratings

### [ ] 9.2 — Build Test

```bash
cd src/frontend/storefront
npm run build
```

Verify no TypeScript errors.

### [ ] 9.3 — Check Bundle Size

Compare before/after bundle size. Should be similar or smaller due to better tree-shaking.

---

## Execution Checklist

### Phase 1 — QueryRenderer ✅ COMPLETED
- [x] 1.1 Create QueryRenderer component
- [x] 1.2 Refactor Home.tsx
- [x] 1.3 Refactor Products.tsx
- [x] 1.4 Refactor Wishlist.tsx
- [x] 1.5 Refactor OrderHistory.tsx

### Phase 2 — PaginatedView
- [ ] 2.1 Create PaginatedView component
- [ ] 2.2 Create PaginatedView.module.css
- [ ] 2.3 Refactor Products.tsx

### Phase 3 — useForm Hook
- [ ] 3.1 Create useForm hook
- [ ] 3.2 Create hooks directory
- [ ] 3.3 Refactor Login.tsx
- [ ] 3.4 Refactor Register.tsx
- [ ] 3.5 Refactor Profile.tsx

### Phase 4 — StarRating Component
- [ ] 4.1 Create StarRating component
- [ ] 4.2 Create StarRating.module.css
- [ ] 4.3 Refactor ReviewForm.tsx
- [ ] 4.4 Refactor ReviewList.tsx

### Phase 5 — CartItemCard
- [ ] 5.1 Create CartItemCard component
- [ ] 5.2 Create CartItemCard.module.css
- [ ] 5.3 Refactor Cart.tsx
- [ ] 5.4 Refactor Checkout.tsx

### Phase 6 — useProductDetails Hook
- [ ] 6.1 Create useProductDetails hook
- [ ] 6.2 Refactor ProductDetail.tsx

### Phase 7 — Constants
- [ ] 7.1 Update constants.ts
- [ ] 7.2 Update imports across files

### Phase 8 — Icon Components
- [ ] 8.1 Create icons directory
- [ ] 8.2 Create icon components
- [ ] 8.3 Replace inline SVGs

### Phase 9 — Testing
- [ ] 9.1 Manual testing
- [ ] 9.2 Build test
- [ ] 9.3 Bundle size check

---

## Metrics

**Before Refactoring:**
- Total lines in pages: ~2,500
- Duplicate code percentage: ~30%
- Number of inline styles: 50+
- Average component size: 200 lines

**After Refactoring (Expected):**
- Total lines in pages: ~1,500 (40% reduction)
- Duplicate code percentage: <5%
- Number of inline styles: 0
- Average component size: 100 lines
- New reusable components: 8
- New custom hooks: 2

**Developer Benefits:**
- ✅ Consistent UI patterns
- ✅ Easier to add new features
- ✅ Better type safety
- ✅ Improved performance
- ✅ Easier testing
- ✅ Better maintainability

---

## Notes

- Complete phases in order - later phases depend on earlier ones
- Test after each phase before moving to the next
- Keep old code commented out initially for easy rollback
- Update this document as you progress (check boxes)
- Add any issues or improvements discovered during implementation

---

**Document Status:** Ready for execution
**Last Updated:** 2026-02-05
**Priority:** HIGH - Reduces technical debt significantly
