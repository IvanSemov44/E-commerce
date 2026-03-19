import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductActions } from './ProductActions';
import type { ProductDetail } from '@/shared/types';

// Mock the smart hooks so we control all cart/wishlist behavior
vi.mock('./ProductActions.hooks', () => ({
  useCartActions: vi.fn(),
  useWishlistActions: vi.fn(),
}));

const defaultCartHook = {
  quantity: 1,
  setQuantity: vi.fn(),
  addedToCart: false,
  cartError: null,
  dismissCartError: vi.fn(),
  addToCart: vi.fn(),
  isAdding: false,
};

const defaultWishlistHook = {
  isInWishlist: false,
  toggleWishlist: vi.fn(),
  isAdding: false,
  isRemoving: false,
};

const authAuthenticated = {
  isAuthenticated: true,
  user: { id: '1', email: 'test@test.com', firstName: 'Test', lastName: 'User', role: 'customer' },
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

const emptyCart = { items: [], lastUpdated: 0 };

const makeProduct = (overrides: Partial<ProductDetail> = {}): ProductDetail => ({
  id: 'test-product',
  name: 'Test Product',
  slug: 'test-product',
  price: 29.99,
  images: [{ id: 'img-1', url: '/test.jpg', altText: 'Test', isPrimary: true }],
  stockQuantity: 10,
  lowStockThreshold: 3,
  averageRating: 4.5,
  reviewCount: 12,
  isActive: true,
  reviews: [],
  ...overrides,
});

const render = (
  productOverrides: Partial<ProductDetail> = {},
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  preloadedState: any = { auth: authAuthenticated, cart: emptyCart }
) =>
  renderWithProviders(<ProductActions product={makeProduct(productOverrides)} />, {
    preloadedState,
    withRouter: false,
  });

describe('ProductActions', () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook });
    vi.mocked(hooks.useWishlistActions).mockReturnValue({ ...defaultWishlistHook });
  });

  it('displays in stock status', () => {
    render();

    expect(screen.getByText('10 in stock')).toBeInTheDocument();
  });

  it('displays low stock warning', () => {
    render({ stockQuantity: 2 });

    expect(screen.getByText(/only 2 left/i)).toBeInTheDocument();
  });

  it('displays out of stock message', () => {
    render({ stockQuantity: 0 });

    expect(screen.getAllByText(/out of stock/i).length).toBeGreaterThan(0);
  });

  it('renders quantity controls', () => {
    render();

    expect(screen.getByDisplayValue('1')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '−' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '+' })).toBeInTheDocument();
  });

  it('calls setQuantity when quantity increases', async () => {
    const user = userEvent.setup();
    const setQuantity = vi.fn();
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook, setQuantity });

    render();

    await user.click(screen.getByRole('button', { name: '+' }));

    expect(setQuantity).toHaveBeenCalledWith(2);
  });

  it('shows cart hint when item is in cart', () => {
    render(
      {},
      {
        auth: authAuthenticated,
        cart: {
          items: [
            {
              id: 'test-product',
              quantity: 2,
              name: 'Test',
              slug: 'test-product',
              price: 10,
              maxStock: 10,
              image: '/test.jpg',
            },
          ],
          lastUpdated: 0,
        },
      }
    );

    expect(screen.getByText(/2 in cart/i)).toBeInTheDocument();
  });

  it('renders add to cart button', () => {
    render();

    expect(screen.getByRole('button', { name: /add to cart/i })).toBeInTheDocument();
  });

  it('disables add to cart when out of stock', () => {
    render({ stockQuantity: 0 });

    const button = screen.getByRole('button', { name: /out of stock/i });
    expect(button).toBeDisabled();
  });

  it('shows added confirmation', async () => {
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook, addedToCart: true });

    render();

    expect(screen.getByText(/added to cart/i)).toBeInTheDocument();
  });

  it('renders wishlist button when authenticated', () => {
    render({}, { auth: authAuthenticated, cart: emptyCart });

    expect(screen.getByRole('button', { name: /wishlist/i })).toBeInTheDocument();
  });

  it('does not render wishlist button when not authenticated', () => {
    render({}, { auth: authUnauthenticated, cart: emptyCart });

    expect(screen.queryByRole('button', { name: /wishlist/i })).not.toBeInTheDocument();
  });

  it('shows error message when cart error exists', async () => {
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({
      ...defaultCartHook,
      cartError: 'Failed to add to cart',
    });

    render();

    expect(screen.getByText('Failed to add to cart')).toBeInTheDocument();
  });

  it('calls dismissCartError when error is dismissed', async () => {
    const user = userEvent.setup();
    const dismissCartError = vi.fn();
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({
      ...defaultCartHook,
      cartError: 'Failed to add',
      dismissCartError,
    });

    render();

    const dismissButton = screen.getByRole('button', { name: /close|dismiss|×/i });
    await user.click(dismissButton);

    expect(dismissCartError).toHaveBeenCalled();
  });

  it('disables increase button at max stock', async () => {
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook, quantity: 10 });

    render({ stockQuantity: 10 });

    const increaseButton = screen.getByRole('button', { name: '+' });
    expect(increaseButton).toBeDisabled();
  });

  it('calls setQuantity with minimum of 1 when decreasing at quantity 1', async () => {
    const user = userEvent.setup();
    const setQuantity = vi.fn();
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({
      ...defaultCartHook,
      quantity: 1,
      setQuantity,
    });

    render();

    await user.click(screen.getByRole('button', { name: '−' }));

    expect(setQuantity).toHaveBeenCalledWith(1);
  });

  it('shows in wishlist button text when item is in wishlist', async () => {
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useWishlistActions).mockReturnValue({
      ...defaultWishlistHook,
      isInWishlist: true,
    });

    render();

    expect(screen.getByRole('button', { name: /in wishlist/i })).toBeInTheDocument();
  });

  it('calls toggleWishlist when wishlist button is clicked', async () => {
    const user = userEvent.setup();
    const toggleWishlist = vi.fn();
    const hooks = await import('./ProductActions.hooks');
    vi.mocked(hooks.useWishlistActions).mockReturnValue({ ...defaultWishlistHook, toggleWishlist });

    render();

    await user.click(screen.getByRole('button', { name: /wishlist/i }));

    expect(toggleWishlist).toHaveBeenCalled();
  });
});
