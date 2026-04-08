import { screen } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
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
    setupProductsHandlers();
  });

  it('renders the page header', () => {
    render();
    expect(screen.getByText(/discover/i)).toBeInTheDocument();
  });

  it('shows skeleton while loading', async () => {
    server.use(
      http.get('/api/products', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100));
        return HttpResponse.json({
          success: true,
          data: mockPaginatedResult,
        });
      })
    );
    render();
    const skeletonItems = document.querySelectorAll('[aria-busy="true"]');
    expect(skeletonItems.length).toBeGreaterThan(0);
  });

  it('shows error state when query fails', () => {
    server.use(
      http.get('/api/products', () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Server error', code: 'INTERNAL_ERROR' } },
          { status: 500 }
        );
      })
    );
    render();
    expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
  });

  it('shows empty state with no filters active', () => {
    server.use(
      http.get('/api/products', () => {
        return HttpResponse.json({
          success: true,
          data: { ...mockPaginatedResult, items: [], totalCount: 0 },
        });
      })
    );
    render('/');
    expect(screen.getByText(/no products/i)).toBeInTheDocument();
  });

  it('shows "no matches" empty state when filters are active', () => {
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
