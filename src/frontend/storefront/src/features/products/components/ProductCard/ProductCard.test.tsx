import { screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductCard } from './ProductCard';

// Mock the OptimizedImage component to simplify testing
vi.mock('../../../../../components/ui/OptimizedImage', () => ({
  default: ({ src, alt }: { src: string; alt: string }) => (
    <img src={src} alt={alt} data-testid="optimized-image" />
  ),
}));

// Mock RTK Query hooks
const mockAddToWishlist = vi.fn();
const mockRemoveFromWishlist = vi.fn();
const mockAddToCartBackend = vi.fn();
const mockGetWishlist = vi.fn();

vi.mock('@/features/wishlist/api', () => ({
  useGetWishlistQuery: (...args: unknown[]) => {
    const result = mockGetWishlist(...args);
    return { data: result };
  },
  useAddToWishlistMutation: () => [mockAddToWishlist, { isLoading: false }],
  useRemoveFromWishlistMutation: () => [mockRemoveFromWishlist, { isLoading: false }],
}));

vi.mock('@/features/cart/api', () => ({
  useAddToCartMutation: () => [mockAddToCartBackend, { isLoading: false }],
}));

describe('ProductCard', () => {
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

  const renderComponent = (product = mockProduct, isAuthenticated = false) =>
    renderWithProviders(<ProductCard {...product} />, {
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
    vi.clearAllMocks();
    mockGetWishlist.mockReturnValue({ id: '', items: [], itemCount: 0 }); // Default not in wishlist
    mockAddToWishlist.mockReturnValue({ unwrap: () => Promise.resolve() });
    mockRemoveFromWishlist.mockReturnValue({ unwrap: () => Promise.resolve() });
    mockAddToCartBackend.mockReturnValue({ unwrap: () => Promise.resolve() });
  });

  it('renders product details correctly', () => {
    renderComponent();

    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('$99.99')).toBeInTheDocument();
    expect(screen.getByText('$129.99')).toBeInTheDocument(); // Original price
    expect(screen.getByRole('img')).toHaveAttribute('src', '/test-image.jpg');
  });

  it('shows "Out of Stock" badge when stock is 0', () => {
    renderComponent({ ...mockProduct, stockQuantity: 0 });
    expect(screen.getByText(/Sold Out/i)).toBeInTheDocument();
  });

  it('navigates to product detail page on click', () => {
    renderComponent();
    const link = screen.getByRole('link', { name: /View details for Test Product/i });
    expect(link).toHaveAttribute('href', '/products/test-product');
  });

  it('dispatches local addToCart action when not authenticated', () => {
    const { store } = renderComponent();

    const addToCartBtn = screen.getByRole('button', { name: /quick add to cart/i });
    fireEvent.click(addToCartBtn);

    // Check that the item was added to the cart state
    const cartState = store.getState().cart;
    expect(cartState.items).toHaveLength(1);
    expect(cartState.items[0].id).toBe('123');
    expect(cartState.items[0].quantity).toBe(1);
    expect(mockAddToCartBackend).not.toHaveBeenCalled();
  });

  it('calls backend addToCart mutation when authenticated', async () => {
    renderComponent(mockProduct, true); // Authenticated

    const addToCartBtn = screen.getByRole('button', { name: /quick add to cart/i });
    fireEvent.click(addToCartBtn);

    expect(mockAddToCartBackend).toHaveBeenCalledWith({ productId: '123', quantity: 1 });
  });

  it('disables add to cart button when out of stock', () => {
    renderComponent({ ...mockProduct, stockQuantity: 0 });

    const addToCartBtn = screen.getByRole('button', { name: /quick add to cart/i });
    expect(addToCartBtn).toBeDisabled();
  });

  it('handles wishlist toggle when authenticated', async () => {
    renderComponent(mockProduct, true);

    const wishlistBtn = screen.getByRole('button', { name: /add to wishlist/i });
    fireEvent.click(wishlistBtn);

    expect(mockAddToWishlist).toHaveBeenCalledWith('123');
  });

  it('removes from wishlist if already in wishlist', async () => {
    mockGetWishlist.mockReturnValue({ id: '', items: [{ productId: '123' }], itemCount: 1 }); // Already in wishlist
    renderComponent(mockProduct, true);

    const wishlistBtn = screen.getByRole('button', { name: /remove from wishlist/i });
    fireEvent.click(wishlistBtn);

    expect(mockRemoveFromWishlist).toHaveBeenCalledWith('123');
  });

  it('does not show wishlist button when not authenticated', () => {
    renderComponent(mockProduct, false);

    expect(screen.queryByRole('button', { name: /add to wishlist/i })).not.toBeInTheDocument();
  });
});
