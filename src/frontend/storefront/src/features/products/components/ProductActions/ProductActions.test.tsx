import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductActions } from './ProductActions';
import type { ProductDetail } from '@/shared/types';

// Mock the smart hooks so we control all cart/wishlist behavior
vi.mock('@/features/products/hooks', () => ({
  useCartActions: vi.fn(),
  useWishlistToggle: vi.fn(),
}));

const defaultCartHook = {
  quantity: 1,
  setQuantity: vi.fn(),
  addedToCart: false,
  cartError: null,
  dismissCartError: vi.fn(),
  addToCart: vi.fn(),
  isAdding: false,
  isInStock: true,
};

const defaultWishlistHook = {
  isInWishlist: false,
  toggleWishlist: vi.fn(),
  isWishlistLoading: false,
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

type PreloadedState = NonNullable<Parameters<typeof renderWithProviders>[1]>['preloadedState'];

const render = (
  productOverrides: Partial<ProductDetail> = {},
  preloadedState: PreloadedState = { auth: authAuthenticated, cart: emptyCart }
) =>
  renderWithProviders(<ProductActions product={makeProduct(productOverrides)} />, {
    preloadedState,
    withRouter: false,
  });

describe('ProductActions', () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook });
    vi.mocked(hooks.useWishlistToggle).mockReturnValue({ ...defaultWishlistHook });
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
    const hooks = await import('@/features/products/hooks');
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
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook, addedToCart: true });

    render();

    expect(screen.getByText(/added to cart/i)).toBeInTheDocument();
  });

  it('calls addToCart when add to cart button is clicked', async () => {
    const user = userEvent.setup();
    const addToCart = vi.fn();
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook, addToCart });

    render();

    await user.click(screen.getByRole('button', { name: /add to cart/i }));

    expect(addToCart).toHaveBeenCalled();
  });

  it('disables add to cart when isAdding is true', async () => {
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook, isAdding: true });

    render();

    expect(screen.getByRole('button', { name: /add to cart/i })).toBeDisabled();
  });

  it('disables add to cart when addedToCart is true', async () => {
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook, addedToCart: true });

    render();

    expect(screen.getByRole('button', { name: /added to cart/i })).toBeDisabled();
  });

  it('does not show low stock warning when out of stock', () => {
    render({ stockQuantity: 0, lowStockThreshold: 3 });

    expect(screen.queryByText(/only/i)).not.toBeInTheDocument();
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
    const hooks = await import('@/features/products/hooks');
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
    const hooks = await import('@/features/products/hooks');
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
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useCartActions).mockReturnValue({ ...defaultCartHook, quantity: 10 });

    render({ stockQuantity: 10 });

    const increaseButton = screen.getByRole('button', { name: '+' });
    expect(increaseButton).toBeDisabled();
  });

  it('calls setQuantity with minimum of 1 when decreasing at quantity 1', async () => {
    const user = userEvent.setup();
    const setQuantity = vi.fn();
    const hooks = await import('@/features/products/hooks');
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
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useWishlistToggle).mockReturnValue({
      ...defaultWishlistHook,
      isInWishlist: true,
    });

    render();

    expect(screen.getByRole('button', { name: /in wishlist/i })).toBeInTheDocument();
  });

  it('disables wishlist button when isAdding is true', async () => {
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useWishlistToggle).mockReturnValue({ ...defaultWishlistHook, isAdding: true });

    render();

    expect(screen.getByRole('button', { name: /wishlist/i })).toBeDisabled();
  });

  it('disables wishlist button when isRemoving is true', async () => {
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useWishlistToggle).mockReturnValue({
      ...defaultWishlistHook,
      isRemoving: true,
    });

    render();

    expect(screen.getByRole('button', { name: /wishlist/i })).toBeDisabled();
  });

  it('calls toggleWishlist when wishlist button is clicked', async () => {
    const user = userEvent.setup();
    const toggleWishlist = vi.fn();
    const hooks = await import('@/features/products/hooks');
    vi.mocked(hooks.useWishlistToggle).mockReturnValue({ ...defaultWishlistHook, toggleWishlist });

    render();

    await user.click(screen.getByRole('button', { name: /wishlist/i }));

    expect(toggleWishlist).toHaveBeenCalled();
  });
});
