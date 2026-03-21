import { screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductDetailPage } from './ProductDetailPage';
import * as useProductDataModule from '@/features/products/hooks/useProductData';

vi.mock('@/features/products/hooks/useProductData', () => ({
  useProductData: vi.fn(),
}));

vi.mock('@/features/products/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/features/products/api')>();
  return {
    ...actual,
    useCreateReviewMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
    useGetProductReviewsQuery: vi.fn(() => ({ data: [], isLoading: false, error: undefined })),
    useGetMyReviewsQuery: vi.fn(() => ({ data: [], isLoading: false, error: undefined })),
  };
});

vi.mock('@/features/wishlist/api', () => ({
  useGetWishlistQuery: vi.fn(() => ({ data: undefined })),
  useAddToWishlistMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
  useRemoveFromWishlistMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
}));

vi.mock('@/features/cart/api', () => ({
  useAddToCartMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
  useUpdateCartItemMutation: () => [vi.fn()],
  useRemoveFromCartMutation: () => [vi.fn()],
}));

vi.mock('@/shared/hooks', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/shared/hooks')>();
  return { ...actual, usePerformanceMonitor: vi.fn() };
});

const mockProduct = {
  id: 'p1',
  name: 'Detailed Product',
  slug: 'detailed-product',
  price: 49.99,
  description: 'A detailed product description',
  images: [{ url: '/detail.jpg', altText: 'Detail', isPrimary: true, displayOrder: 0 }],
  stockQuantity: 8,
  averageRating: 4.5,
  reviewCount: 5,
  lowStockThreshold: 3,
  isActive: true,
  reviews: [],
};

const mockReviews = [
  {
    id: 'r1',
    rating: 5,
    comment: 'Excellent product!',
    userName: 'Alice',
    createdAt: '2024-01-01T00:00:00Z',
  },
];

const authAuthenticated = {
  isAuthenticated: true,
  user: { id: '1', email: 'a@b.com', firstName: 'A', lastName: 'B', role: 'customer' },
  loading: false,
  error: null,
  initialized: true,
};

const authUnauthenticated = {
  isAuthenticated: false,
  user: null,
  loading: false,
  error: null,
  initialized: true,
};

const render = (
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  preloadedState: any = { auth: authAuthenticated, cart: { items: [], lastUpdated: 0 } }
) =>
  renderWithProviders(
    <MemoryRouter initialEntries={['/products/detailed-product']}>
      <Routes>
        <Route path="/products/:slug" element={<ProductDetailPage />} />
      </Routes>
    </MemoryRouter>,
    { preloadedState, withRouter: false }
  );

describe('ProductDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows skeleton while product is loading', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: undefined,
      isLoading: true,
      error: undefined,
      reviews: undefined,
      reviewsLoading: false,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render();

    const skeletonItems = document.querySelectorAll('[aria-busy="true"]');
    expect(skeletonItems.length).toBeGreaterThan(0);
  });

  it('shows error state when product query fails', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: undefined,
      isLoading: false,
      error: { status: 404, data: { message: 'Not found' } },
      reviews: undefined,
      reviewsLoading: false,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render();

    expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
  });

  it('shows empty state when product is not found', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: null,
      isLoading: false,
      error: undefined,
      reviews: undefined,
      reviewsLoading: false,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render();

    expect(screen.getByText(/not found/i)).toBeInTheDocument();
  });

  it('renders product name when loaded', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: mockProduct,
      isLoading: false,
      error: undefined,
      reviews: [],
      reviewsLoading: false,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render();

    expect(screen.getByText('Detailed Product')).toBeInTheDocument();
  });

  it('renders product image gallery', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: mockProduct,
      isLoading: false,
      error: undefined,
      reviews: [],
      reviewsLoading: false,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render();

    const img = screen.getByRole('img', { name: 'Detailed Product' });
    expect(img).toBeInTheDocument();
  });

  it('shows review form when authenticated', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: mockProduct,
      isLoading: false,
      error: undefined,
      reviews: [],
      reviewsLoading: false,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render({ auth: authAuthenticated, cart: { items: [], lastUpdated: 0 } });

    expect(screen.getByText(/write a review/i)).toBeInTheDocument();
  });

  it('hides review form when not authenticated', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: mockProduct,
      isLoading: false,
      error: undefined,
      reviews: [],
      reviewsLoading: false,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render({ auth: authUnauthenticated, cart: { items: [], lastUpdated: 0 } });

    expect(screen.queryByText(/write a review/i)).not.toBeInTheDocument();
  });

  it('renders review list with reviews', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: mockProduct,
      isLoading: false,
      error: undefined,
      reviews: mockReviews,
      reviewsLoading: false,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render();

    expect(screen.getByText('Excellent product!')).toBeInTheDocument();
    expect(screen.getByText(/alice/i)).toBeInTheDocument();
  });

  it('shows reviews skeleton while reviews are loading', () => {
    vi.mocked(useProductDataModule.useProductData).mockReturnValue({
      product: mockProduct,
      isLoading: false,
      error: undefined,
      reviews: undefined,
      reviewsLoading: true,
      reviewsError: undefined,
      refetchReviews: vi.fn(),
    } as never);

    render();

    const skeletonItems = document.querySelectorAll('[aria-busy="true"]');
    expect(skeletonItems.length).toBeGreaterThan(0);
  });
});
