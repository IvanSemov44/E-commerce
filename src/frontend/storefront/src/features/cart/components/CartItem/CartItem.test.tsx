import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import { CartItem } from './CartItem';
import type { CartItem as CartItemType } from '@/features/cart/types';

const mockUpdate = vi.fn();
const mockRemove = vi.fn();
const mockSuccess = vi.fn();
const mockShowError = vi.fn();

vi.mock('@/features/cart/hooks', () => ({
  useCartOperations: () => ({
    update: mockUpdate,
    remove: mockRemove,
    add: vi.fn(),
  }),
}));

vi.mock('@/shared/hooks', () => ({
  useToast: () => ({ success: mockSuccess, error: mockShowError }),
}));

const mockItem: CartItemType = {
  id: '1',
  slug: 'test-product',
  name: 'Test Product',
  price: 29.99,
  compareAtPrice: 39.99,
  quantity: 2,
  maxStock: 10,
  image: 'http://example.com/image.jpg',
};

const renderCartItem = (props: Partial<React.ComponentProps<typeof CartItem>> = {}) => {
  return render(
    <BrowserRouter>
      <CartItem item={mockItem} {...props} />
    </BrowserRouter>
  );
};

describe('CartItem', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUpdate.mockResolvedValue(undefined);
    mockRemove.mockResolvedValue(undefined);
  });

  it('renders item details correctly', () => {
    renderCartItem();

    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('$29.99')).toBeInTheDocument();
    expect(screen.getByText('$39.99')).toBeInTheDocument();
  });

  it('displays correct quantity', () => {
    renderCartItem();

    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('calculates and displays subtotal correctly', () => {
    renderCartItem();

    expect(screen.getByText('$59.98')).toBeInTheDocument();
  });

  it('calls update with incremented quantity when + clicked', async () => {
    const user = userEvent.setup();
    renderCartItem();

    await user.click(screen.getByRole('button', { name: 'Increase quantity' }));

    expect(mockUpdate).toHaveBeenCalledWith('1', 3);
  });

  it('calls update with decremented quantity when − clicked', async () => {
    const user = userEvent.setup();
    renderCartItem();

    await user.click(screen.getByRole('button', { name: 'Decrease quantity' }));

    expect(mockUpdate).toHaveBeenCalledWith('1', 1);
  });

  it('calls remove when Remove button clicked', async () => {
    const user = userEvent.setup();
    renderCartItem();

    await user.click(screen.getByRole('button', { name: /remove/i }));

    expect(mockRemove).toHaveBeenCalledWith('1');
  });

  it('shows success toast after update', async () => {
    const user = userEvent.setup();
    renderCartItem();

    await user.click(screen.getByRole('button', { name: 'Increase quantity' }));

    expect(mockSuccess).toHaveBeenCalledWith('Cart updated');
  });

  it('shows error toast when update fails', async () => {
    mockUpdate.mockRejectedValue(new Error('network error'));
    const user = userEvent.setup();
    renderCartItem();

    await user.click(screen.getByRole('button', { name: 'Increase quantity' }));

    expect(mockShowError).toHaveBeenCalledWith('Failed to update cart');
  });

  it('shows success toast after remove', async () => {
    const user = userEvent.setup();
    renderCartItem();

    await user.click(screen.getByRole('button', { name: /remove/i }));

    expect(mockSuccess).toHaveBeenCalledWith('Item removed from cart');
  });

  it('shows error toast when remove fails', async () => {
    mockRemove.mockRejectedValue(new Error('network error'));
    const user = userEvent.setup();
    renderCartItem();

    await user.click(screen.getByRole('button', { name: /remove/i }));

    expect(mockShowError).toHaveBeenCalledWith('Failed to remove item');
  });

  it('disables increase button when max stock is reached', () => {
    renderCartItem({ item: { ...mockItem, quantity: 10, maxStock: 10 } });

    expect(screen.getByRole('button', { name: 'Increase quantity' })).toBeDisabled();
    expect(screen.getByText('Max stock reached')).toBeInTheDocument();
  });

  it('renders read-only quantity when readOnly is true', () => {
    renderCartItem({ readOnly: true });

    expect(screen.getByText('Quantity: 2')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Decrease quantity' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Increase quantity' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /remove/i })).not.toBeInTheDocument();
  });

  it('handles item without compareAtPrice', () => {
    renderCartItem({ item: { ...mockItem, compareAtPrice: undefined } });

    expect(screen.getByText('$29.99')).toBeInTheDocument();
    expect(screen.queryByText('$39.99')).not.toBeInTheDocument();
  });

  it('enables increase button when quantity is below max', () => {
    renderCartItem({ item: { ...mockItem, quantity: 5, maxStock: 10 } });

    expect(screen.getByRole('button', { name: 'Increase quantity' })).not.toBeDisabled();
    expect(screen.queryByText('Max stock reached')).not.toBeInTheDocument();
  });

  it('uses default image when image is empty', () => {
    renderCartItem({ item: { ...mockItem, image: '' } });

    expect(screen.getAllByRole('img').length).toBeGreaterThan(0);
  });

  it('handles zero price correctly', () => {
    renderCartItem({ item: { ...mockItem, price: 0, quantity: 1 } });

    expect(screen.getAllByText('$0.00').length).toBeGreaterThan(0);
  });

  it('handles very large price correctly', () => {
    renderCartItem({ item: { ...mockItem, price: 9999.99, quantity: 1 } });

    expect(screen.getAllByText('$9999.99').length).toBeGreaterThan(0);
  });

  it('does not show max stock warning in read-only mode', () => {
    renderCartItem({ item: { ...mockItem, quantity: 10, maxStock: 10 }, readOnly: true });

    expect(screen.queryByText('Max stock reached')).not.toBeInTheDocument();
  });

  it('both product name and image link to product page', () => {
    renderCartItem();

    const links = screen.getAllByRole('link', { name: /test product/i });
    expect(links.length).toBe(2);
    links.forEach((link) => {
      expect(link).toHaveAttribute('href', '/products/test-product');
    });
  });

  it('shows strikethrough price only with compareAtPrice', () => {
    renderCartItem();

    expect(screen.getByText('$39.99').className).toContain('strikethrough');
  });
});
