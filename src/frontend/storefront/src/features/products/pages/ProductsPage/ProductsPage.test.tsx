import { screen } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MemoryRouter } from 'react-router';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductsPage } from './ProductsPage';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

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

const mockCategories = [
  { id: 'cat-1', name: 'Electronics', slug: 'electronics', subcategories: [] },
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

let mockProductsData = {
  data: mockPaginatedResult,
  isLoading: false,
  error: null as null | unknown,
};

let mockCategoriesData = {
  data: mockCategories,
  isLoading: false,
  error: null as null | unknown,
};

vi.mock('@/features/products/api', () => ({
  useGetProductsQuery: () => mockProductsData,
  useGetTopLevelCategoriesQuery: () => mockCategoriesData,
}));

vi.mock('@/features/categories/api', () => ({
  useGetTopLevelCategoriesQuery: () => mockCategoriesData,
}));

const render = (
  initialEntry = '/',
  preloadedState: Record<string, unknown> = defaultPreloadedState
) =>
  renderWithProviders(
    <MemoryRouter initialEntries={[initialEntry]}>
      <ProductsPage />
    </MemoryRouter>,
    { preloadedState, withRouter: false }
  );

const setupProductsHandlers = (products = mockPaginatedResult, categories = mockCategories) => {
  server.use(
    http.get('/api/products', () => {
      return HttpResponse.json({
        success: true,
        data: products,
      });
    }),
    http.get('/api/categories/top-level', () => {
      return HttpResponse.json({
        success: true,
        data: categories,
      });
    })
  );
};

describe('ProductsPage', () => {
  beforeEach(() => {
    server.resetHandlers();
    mockProductsData = {
      data: mockPaginatedResult,
      isLoading: false,
      error: null,
    };
    mockCategoriesData = {
      data: mockCategories,
      isLoading: false,
      error: null,
    };
    setupProductsHandlers();
  });

  it('renders the page header', () => {
    render();
    expect(screen.getByText(/discover/i)).toBeInTheDocument();
  });

  it('shows skeleton while loading', async () => {
    mockProductsData = {
      data: mockPaginatedResult,
      isLoading: true,
      error: null,
    };
    render();
    const skeletonItems = document.querySelectorAll('[aria-busy="true"]');
    expect(skeletonItems.length).toBeGreaterThan(0);
  });

  it('shows error state when query fails', () => {
    mockProductsData = {
      data: undefined,
      isLoading: false,
      error: { message: 'Server error' },
    };
    render();
    expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
  });

  it('shows empty state with no filters active', () => {
    mockProductsData = {
      data: { ...mockPaginatedResult, items: [], totalCount: 0 },
      isLoading: false,
      error: null,
    };
    render('/');
    expect(screen.getByText(/no products/i)).toBeInTheDocument();
  });

  it('shows "no matches" empty state when filters are active', () => {
    mockProductsData = {
      data: { ...mockPaginatedResult, items: [], totalCount: 0 },
      isLoading: false,
      error: null,
    };
    render('/?search=nonexistent');
    expect(screen.getByText(/no products match/i)).toBeInTheDocument();
  });

  it('renders product grid when data is returned', () => {
    render();
    expect(screen.getByText('Test Widget')).toBeInTheDocument();
  });

  it('renders sidebar with category filter and product filters', () => {
    render();
    expect(screen.getAllByText(/all products/i).length).toBeGreaterThan(0);
  });
});
