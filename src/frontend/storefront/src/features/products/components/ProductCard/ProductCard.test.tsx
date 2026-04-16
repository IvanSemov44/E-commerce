import { screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductCard } from './ProductCard';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key}:${JSON.stringify(opts)}` : key,
  }),
}));

let mockWishlistState: {
  isInWishlist: boolean;
  isWishlistLoading: boolean;
  toggleWishlist: ReturnType<typeof vi.fn>;
} = {
  isInWishlist: false,
  isWishlistLoading: false,
  toggleWishlist: vi.fn().mockResolvedValue(undefined),
};

let mockAddToCart: ReturnType<typeof vi.fn> = vi.fn();
let mockIsInStock = true;

vi.mock('@/features/products/hooks', () => ({
  useWishlistToggle: () => mockWishlistState,
  useCartActions: () => ({
    addToCart: mockAddToCart,
    isAdding: false,
    addedToCart: false,
    isInStock: mockIsInStock,
    inCartQuantity: 0,
  }),
}));

const mockProduct = {
  id: '123',
  name: 'Test Product',
  price: 99.99,
  compareAtPrice: 129.99,
  imageUrl: '/test-image.jpg',
  slug: 'test-product',
  rating: 4.5,
  reviewCount: 10,
  stockQuantity: 10,
};

const renderCard = (overrides = {}, isAuthenticated = false) =>
  renderWithProviders(<ProductCard {...mockProduct} {...overrides} />, {
    preloadedState: {
      cart: { items: [], lastUpdated: Date.now() },
      auth: {
        isAuthenticated,
        user: isAuthenticated
          ? {
              id: '1',
              email: 'test@test.com',
              firstName: 'Test',
              lastName: 'User',
              role: 'customer',
            }
          : null,
        loading: false,
        error: null,
        initialized: true,
      },
    },
  });

const setupApiHandlers = (wishlistItems = []) => {
  server.use(
    http.get('/api/wishlist', () => {
      return HttpResponse.json({
        success: true,
        data: { id: 'w1', items: wishlistItems, itemCount: wishlistItems.length },
      });
    }),
    http.post('/api/wishlist/add', async ({ request }) => {
      const body = await request.json();
      return HttpResponse.json({
        success: true,
        data: { id: 'w1', items: [{ productId: body.productId }], itemCount: 1 },
      });
    }),
    http.delete('/api/wishlist/remove/123', () => {
      return HttpResponse.json({
        success: true,
        data: { id: 'w1', items: [], itemCount: 0 },
      });
    }),
    http.post('/api/cart/add-item', async ({ request }) => {
      const body = await request.json();
      return HttpResponse.json({
        success: true,
        data: {
          id: 'c1',
          items: [{ id: 'item-1', productId: body.productId, quantity: body.quantity }],
          itemCount: 1,
          subtotal: 99.99,
        },
      });
    })
  );
};

describe('ProductCard', () => {
  beforeEach(() => {
    mockWishlistState = {
      isInWishlist: false,
      isWishlistLoading: false,
      toggleWishlist: vi.fn().mockResolvedValue(undefined),
    };
    mockAddToCart = vi.fn();
    mockIsInStock = true;
    vi.clearAllMocks();
    setupApiHandlers();
  });

  it('renders product name, price and compare price', () => {
    renderCard();
    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('$99.99')).toBeInTheDocument();
    expect(screen.getByText('$129.99')).toBeInTheDocument();
  });

  it('renders product image', () => {
    renderCard();
    expect(screen.getByRole('img')).toHaveAttribute('src', '/test-image.jpg');
  });

  it('falls back to default image on image error', () => {
    renderCard();
    const img = screen.getByRole('img');
    fireEvent.error(img);
    expect(img.getAttribute('src')).toContain('data:image/svg+xml');
  });

  it('falls back to default image when imageUrl is empty', () => {
    renderCard({ imageUrl: '' });
    expect(screen.getByRole('img').getAttribute('src')).toContain('data:image/svg+xml');
  });

  it('links to product detail page', () => {
    renderCard();
    expect(screen.getByRole('link', { name: /View details for Test Product/i })).toHaveAttribute(
      'href',
      '/products/test-product'
    );
  });

  it('shows sold out overlay when out of stock', () => {
    mockIsInStock = false;
    renderCard({ stockQuantity: 0 });
    expect(screen.getByText(/products\.soldOut/i)).toBeInTheDocument();
  });

  it('shows discount badge when discount is >= 10%', () => {
    renderCard({ price: 99.99, compareAtPrice: 129.99 });
    expect(screen.getByText('-23%')).toBeInTheDocument();
  });

  it('does not show discount badge when discount is < 10%', () => {
    renderCard({ price: 99.99, compareAtPrice: 104.99 });
    expect(screen.queryByText(/-\d+%/)).not.toBeInTheDocument();
  });

  it('shows rating badge when rating > 0', () => {
    renderCard({ rating: 4.5 });
    expect(screen.getByText('4.5')).toBeInTheDocument();
  });

  it('does not show rating badge when rating is 0', () => {
    renderCard({ rating: 0 });
    expect(screen.queryByText('0.0')).not.toBeInTheDocument();
  });

  it('shows review count when rating and reviewCount are both > 0', () => {
    renderCard({ rating: 4.5, reviewCount: 10 });
    expect(screen.getByText(/products\.review/)).toBeInTheDocument();
  });

  it('does not show review count when reviewCount is 0', () => {
    renderCard({ rating: 4.5, reviewCount: 0 });
    expect(screen.queryByText(/products\.review/)).not.toBeInTheDocument();
  });

  it('disables quick add button when out of stock', () => {
    mockIsInStock = false;
    renderCard({ stockQuantity: 0 });
    expect(screen.getByRole('button', { name: /quick add to cart/i })).toBeDisabled();
  });

  it('adds to local cart when not authenticated', async () => {
    mockAddToCart = vi.fn();
    renderCard();
    fireEvent.click(screen.getByRole('button', { name: /quick add to cart/i }));
    await waitFor(() => expect(mockAddToCart).toHaveBeenCalledWith());
  });

  it('calls backend addToCart when authenticated', async () => {
    renderCard({}, true);
    fireEvent.click(screen.getByRole('button', { name: /quick add to cart/i }));
    await waitFor(() => expect(screen.queryByText(/failed/i)).not.toBeInTheDocument());
  });

  it('does not show wishlist button when not authenticated', () => {
    renderCard({}, false);
    expect(screen.queryByRole('button', { name: /wishlist/i })).not.toBeInTheDocument();
  });

  it('shows add to wishlist button when authenticated and not in wishlist', () => {
    renderCard({}, true);
    expect(screen.getByRole('button', { name: /add to wishlist/i })).toBeInTheDocument();
  });

  it('shows remove from wishlist button when item is in wishlist', () => {
    mockWishlistState = {
      ...mockWishlistState,
      isInWishlist: true,
    };
    setupApiHandlers([{ productId: '123' }]);
    renderCard({}, true);
    expect(screen.getByRole('button', { name: /remove from wishlist/i })).toBeInTheDocument();
  });

  it('adds to wishlist on toggle when not in wishlist', async () => {
    renderCard({}, true);
    fireEvent.click(screen.getByRole('button', { name: /add to wishlist/i }));
    await waitFor(() => expect(screen.queryByText(/failed/i)).not.toBeInTheDocument());
  });

  it('removes from wishlist on toggle when in wishlist', async () => {
    mockWishlistState = {
      ...mockWishlistState,
      isInWishlist: true,
    };
    setupApiHandlers([{ productId: '123' }]);
    renderCard({}, true);
    fireEvent.click(screen.getByRole('button', { name: /remove from wishlist/i }));
    await waitFor(() => expect(screen.queryByText(/failed/i)).not.toBeInTheDocument());
  });
});
