import { screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductsPage } from './ProductsPage';
import * as productApiModule from '@/features/products/api';

vi.mock('@/features/products/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/features/products/api')>();
  return {
    ...actual,
    useGetProductsQuery: vi.fn(),
    useGetTopLevelCategoriesQuery: vi.fn(),
  };
});

vi.mock('@/features/wishlist/api', () => ({
  useGetWishlistQuery: vi.fn(() => ({ data: undefined })),
  useAddToWishlistMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
  useRemoveFromWishlistMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
}));

vi.mock('@/features/cart/api', () => ({
  useAddToCartMutation: vi.fn(() => [vi.fn(), { isLoading: false }]),
}));

vi.mock('@/shared/hooks', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/shared/hooks')>();
  return { ...actual, usePerformanceMonitor: vi.fn() };
});

const mockProducts = [
  {
    id: 'p1',
    name: 'Test Widget',
    slug: 'test-widget',
    price: 19.99,
    images: [{ id: 'img-1', url: '/img.jpg', altText: 'Test', isPrimary: true }],
    stockQuantity: 5,
    averageRating: 4.2,
    reviewCount: 3,
  },
];

const mockPaginatedResult = {
  items: mockProducts,
  totalCount: 1,
  page: 1,
  pageSize: 12,
  totalPages: 1,
  hasNext: false,
  hasPrevious: false,
};

const defaultPreloadedState = {
  auth: { isAuthenticated: false, user: null, loading: false, error: null, initialized: true },
  cart: { items: [], lastUpdated: 0 },
};

const render = (
  initialEntry = '/',
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  preloadedState: any = defaultPreloadedState
) =>
  renderWithProviders(
    <MemoryRouter initialEntries={[initialEntry]}>
      <ProductsPage />
    </MemoryRouter>,
    { preloadedState, withRouter: false }
  );

describe('ProductsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(productApiModule.useGetTopLevelCategoriesQuery).mockReturnValue({
      data: [],
      isLoading: false,
      error: undefined,
    } as never);
  });

  it('renders the page header', () => {
    vi.mocked(productApiModule.useGetProductsQuery).mockReturnValue({
      data: mockPaginatedResult,
      isLoading: false,
      isFetching: false,
      error: undefined,
    } as never);

    render();

    expect(screen.getByText(/discover/i)).toBeInTheDocument();
  });

  it('shows skeleton while loading', () => {
    vi.mocked(productApiModule.useGetProductsQuery).mockReturnValue({
      data: undefined,
      isLoading: true,
      isFetching: true,
      error: undefined,
    } as never);

    render();

    // ProductsGridSkeleton renders multiple skeleton cards
    const skeletonItems = document.querySelectorAll('[aria-busy="true"]');
    expect(skeletonItems.length).toBeGreaterThan(0);
  });

  it('shows error state when query fails', () => {
    vi.mocked(productApiModule.useGetProductsQuery).mockReturnValue({
      data: undefined,
      isLoading: false,
      isFetching: false,
      error: { status: 500, data: { message: 'Server error' } },
    } as never);

    render();

    expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
  });

  it('shows empty state with no filters active', () => {
    vi.mocked(productApiModule.useGetProductsQuery).mockReturnValue({
      data: { ...mockPaginatedResult, items: [], totalCount: 0 },
      isLoading: false,
      isFetching: false,
      error: undefined,
    } as never);

    render('/');

    expect(screen.getByText(/no products/i)).toBeInTheDocument();
  });

  it('shows "no matches" empty state when filters are active', () => {
    vi.mocked(productApiModule.useGetProductsQuery).mockReturnValue({
      data: { ...mockPaginatedResult, items: [], totalCount: 0 },
      isLoading: false,
      isFetching: false,
      error: undefined,
    } as never);

    render('/?search=nonexistent');

    expect(screen.getByText(/no products match/i)).toBeInTheDocument();
  });

  it('renders product grid when data is returned', () => {
    vi.mocked(productApiModule.useGetProductsQuery).mockReturnValue({
      data: mockPaginatedResult,
      isLoading: false,
      isFetching: false,
      error: undefined,
    } as never);

    render();

    expect(screen.getByText('Test Widget')).toBeInTheDocument();
  });

  it('renders sidebar with category filter and product filters', () => {
    vi.mocked(productApiModule.useGetProductsQuery).mockReturnValue({
      data: mockPaginatedResult,
      isLoading: false,
      isFetching: false,
      error: undefined,
    } as never);

    render();

    expect(screen.getAllByText(/all products/i).length).toBeGreaterThan(0);
  });

  it('shows refetch indicator during background fetch', () => {
    vi.mocked(productApiModule.useGetProductsQuery).mockReturnValue({
      data: mockPaginatedResult,
      isLoading: false,
      isFetching: true,
      error: undefined,
    } as never);

    render();

    expect(screen.getByText(/updating/i)).toBeInTheDocument();
  });
});
