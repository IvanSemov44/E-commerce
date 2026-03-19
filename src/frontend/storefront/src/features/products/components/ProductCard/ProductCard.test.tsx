import { screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductCard } from './ProductCard';

const mockAddToWishlist = vi.fn();
const mockRemoveFromWishlist = vi.fn();
const mockAddToCartBackend = vi.fn();
const mockGetWishlist = vi.fn();

vi.mock('@/features/wishlist/api', () => ({
  useGetWishlistQuery: (...args: unknown[]) => ({ data: mockGetWishlist(...args) }),
  useAddToWishlistMutation: () => [mockAddToWishlist, { isLoading: false }],
  useRemoveFromWishlistMutation: () => [mockRemoveFromWishlist, { isLoading: false }],
}));

vi.mock('@/features/cart/api', () => ({
  useAddToCartMutation: () => [mockAddToCartBackend, { isLoading: false }],
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key}:${JSON.stringify(opts)}` : key,
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

beforeEach(() => {
  vi.resetAllMocks();
  mockGetWishlist.mockReturnValue({ id: '', items: [], itemCount: 0 });
  mockAddToWishlist.mockReturnValue({ unwrap: () => Promise.resolve() });
  mockRemoveFromWishlist.mockReturnValue({ unwrap: () => Promise.resolve() });
  mockAddToCartBackend.mockReturnValue({ unwrap: () => Promise.resolve() });
});

describe('ProductCard', () => {
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

  it('links to product detail page', () => {
    renderCard();
    expect(screen.getByRole('link', { name: /View details for Test Product/i })).toHaveAttribute(
      'href',
      '/products/test-product'
    );
  });

  it('shows sold out overlay when out of stock', () => {
    renderCard({ stockQuantity: 0 });
    expect(screen.getByText(/products\.soldOut/i)).toBeInTheDocument();
  });

  it('shows discount badge when discount is >= 10%', () => {
    renderCard({ price: 99.99, compareAtPrice: 129.99 }); // ~23%
    expect(screen.getByText('-23%')).toBeInTheDocument();
  });

  it('does not show discount badge when discount is < 10%', () => {
    renderCard({ price: 99.99, compareAtPrice: 104.99 }); // ~5%
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

  it('disables quick add button when out of stock', () => {
    renderCard({ stockQuantity: 0 });
    expect(screen.getByRole('button', { name: /quick add to cart/i })).toBeDisabled();
  });

  it('adds to local cart when not authenticated', async () => {
    const { store } = renderCard();
    fireEvent.click(screen.getByRole('button', { name: /quick add to cart/i }));
    await waitFor(() => expect(store.getState().cart.items).toHaveLength(1));
    expect(store.getState().cart.items[0].id).toBe('123');
    expect(mockAddToCartBackend).not.toHaveBeenCalled();
  });

  it('calls backend addToCart when authenticated', async () => {
    renderCard({}, true);
    fireEvent.click(screen.getByRole('button', { name: /quick add to cart/i }));
    await waitFor(() =>
      expect(mockAddToCartBackend).toHaveBeenCalledWith({ productId: '123', quantity: 1 })
    );
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
    mockGetWishlist.mockReturnValue({ id: '', items: [{ productId: '123' }], itemCount: 1 });
    renderCard({}, true);
    expect(screen.getByRole('button', { name: /remove from wishlist/i })).toBeInTheDocument();
  });

  it('calls addToWishlist on toggle when not in wishlist', async () => {
    renderCard({}, true);
    fireEvent.click(screen.getByRole('button', { name: /add to wishlist/i }));
    await waitFor(() => expect(mockAddToWishlist).toHaveBeenCalledWith('123'));
  });

  it('calls removeFromWishlist on toggle when in wishlist', async () => {
    mockGetWishlist.mockReturnValue({ id: '', items: [{ productId: '123' }], itemCount: 1 });
    renderCard({}, true);
    fireEvent.click(screen.getByRole('button', { name: /remove from wishlist/i }));
    await waitFor(() => expect(mockRemoveFromWishlist).toHaveBeenCalledWith('123'));
  });
});
