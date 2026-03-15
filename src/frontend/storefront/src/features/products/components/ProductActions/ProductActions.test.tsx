import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { ProductActions } from './ProductActions';
import type { ProductActionsProps } from './ProductActions.types';

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

const defaultPreloadedState = {
  auth: authAuthenticated,
  cart: emptyCart,
};

const defaultProps: ProductActionsProps = {
  productId: 'test-product',
  stockQuantity: 10,
  lowStockThreshold: 3,
  cart: {
    quantity: 1,
    addedToCart: false,
    isLoading: false,
    error: null,
  },
  wishlist: {
    isInWishlist: false,
    isAdding: false,
    isRemoving: false,
  },
  onQuantityChange: vi.fn(),
  onAddToCart: vi.fn(),
  onToggleWishlist: vi.fn(),
  onDismissError: vi.fn(),
};

const render = (
  props: Partial<ProductActionsProps> = {},
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  preloadedState: any = defaultPreloadedState
) =>
  renderWithProviders(<ProductActions {...defaultProps} {...props} />, {
    preloadedState,
    withRouter: false,
  });

describe('ProductActions', () => {
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

  it('calls onQuantityChange when quantity changes', async () => {
    const user = userEvent.setup();
    const onQuantityChange = vi.fn();
    render({ onQuantityChange });

    const increaseButton = screen.getByRole('button', { name: '+' });
    await user.click(increaseButton);

    expect(onQuantityChange).toHaveBeenCalledWith(2);
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

  it('shows added confirmation', () => {
    render({ cart: { ...defaultProps.cart, addedToCart: true } });

    expect(screen.getByText(/✓ added to cart/i)).toBeInTheDocument();
  });

  it('renders wishlist button when authenticated', () => {
    render({}, { auth: authAuthenticated, cart: emptyCart });

    expect(screen.getByRole('button', { name: /wishlist/i })).toBeInTheDocument();
  });

  it('does not render wishlist button when not authenticated', () => {
    render({}, { auth: authUnauthenticated, cart: emptyCart });

    expect(screen.queryByRole('button', { name: /wishlist/i })).not.toBeInTheDocument();
  });

  it('shows error message when cart error exists', () => {
    render({ cart: { ...defaultProps.cart, error: 'Failed to add to cart' } });

    expect(screen.getByText('Failed to add to cart')).toBeInTheDocument();
  });

  it('calls onDismissError when error is dismissed', async () => {
    const user = userEvent.setup();
    const onDismissError = vi.fn();
    render({ cart: { ...defaultProps.cart, error: 'Failed to add' }, onDismissError });

    const dismissButton = screen.getByRole('button', { name: /close|dismiss|×/i });
    await user.click(dismissButton);

    expect(onDismissError).toHaveBeenCalled();
  });

  it('disables increase button at max stock', () => {
    render({ stockQuantity: 10, cart: { ...defaultProps.cart, quantity: 10 } });

    const increaseButton = screen.getByRole('button', { name: '+' });
    expect(increaseButton).toBeDisabled();
  });
});
