import { screen } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductDetailPage } from './ProductDetailPage';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

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

let mockProductData = {
  product: mockProduct,
  reviews: mockReviews,
  isLoading: false,
  error: null,
};

vi.mock('@/features/products/hooks', () => ({
  useProductData: () => mockProductData,
  useCartActions: () => ({
    addToCart: vi.fn(),
    isAddingToCart: false,
  }),
  useWishlistToggle: (_: string) => {
    void _;
    return {
      toggleWishlist: vi.fn(),
      isWishlistLoading: false,
      isInWishlist: false,
    };
  },
}));

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
  preloadedState: Record<string, unknown> = {
    auth: authAuthenticated,
    cart: { items: [], lastUpdated: 0 },
  }
) =>
  renderWithProviders(
    <MemoryRouter initialEntries={['/products/detailed-product']}>
      <Routes>
        <Route path="/products/:slug" element={<ProductDetailPage />} />
      </Routes>
    </MemoryRouter>,
    { preloadedState, withRouter: false }
  );

const setupProductHandlers = (product = mockProduct, reviews = mockReviews) => {
  server.use(
    http.get('/api/products/slug/detailed-product', () => {
      return HttpResponse.json({
        success: true,
        data: product,
      });
    }),
    http.get('/api/products/p1/reviews', () => {
      return HttpResponse.json({
        success: true,
        data: reviews,
      });
    }),
    http.get('/api/wishlist', () => {
      return HttpResponse.json({ success: true, data: { id: 'w1', items: [], itemCount: 0 } });
    })
  );
};

describe('ProductDetailPage', () => {
  beforeEach(() => {
    mockProductData = {
      product: mockProduct,
      reviews: mockReviews,
      isLoading: false,
      error: null,
    };
    setupProductHandlers();
  });

  it('shows skeleton while product is loading', async () => {
    mockProductData = {
      product: undefined,
      reviews: undefined,
      isLoading: true,
      error: null,
    };
    render();
    const skeletonItems = document.querySelectorAll('[aria-busy="true"]');
    expect(skeletonItems.length).toBeGreaterThan(0);
  });

  it('shows error state when product query fails', async () => {
    mockProductData = {
      product: undefined,
      reviews: undefined,
      isLoading: false,
      error: { message: 'Not found', status: 404 },
    };
    render();
    expect(await screen.findByText(/failed to load/i)).toBeInTheDocument();
  });

  it('renders product name when loaded', async () => {
    render();
    expect(await screen.findByText('Detailed Product')).toBeInTheDocument();
  });

  it('renders product image gallery', async () => {
    render();
    const img = await screen.findByRole('img', { name: 'Detailed Product' });
    expect(img).toBeInTheDocument();
  });

  it('shows review form when authenticated', async () => {
    render({ auth: authAuthenticated, cart: { items: [], lastUpdated: 0 } });
    expect(await screen.findByText(/write a review/i)).toBeInTheDocument();
  });

  it('hides review form when not authenticated', () => {
    render({ auth: authUnauthenticated, cart: { items: [], lastUpdated: 0 } });
    expect(screen.queryByText(/write a review/i)).not.toBeInTheDocument();
  });

  it('renders review list with reviews', async () => {
    render();
    expect(await screen.findByText('Excellent product!')).toBeInTheDocument();
    expect(await screen.findByText(/alice/i)).toBeInTheDocument();
  });

  it('shows reviews skeleton while reviews are loading', async () => {
    mockProductData = {
      product: mockProduct,
      reviews: undefined,
      isLoading: true,
      error: null,
    };
    render();
    const skeletonItems = document.querySelectorAll('[aria-busy="true"]');
    expect(skeletonItems.length).toBeGreaterThan(0);
  });
});
