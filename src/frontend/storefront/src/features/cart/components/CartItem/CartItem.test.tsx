import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import { CartItem } from './CartItem';
import type { CartItem as CartItemType } from '@/features/cart/types';

vi.mock('@/features/cart/hooks', () => ({
  useCartItemActions: vi.fn(),
}));

const defaultActions = {
  handleUpdateQuantity: vi.fn(),
  handleRemove: vi.fn(),
};

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
  beforeEach(async () => {
    vi.clearAllMocks();
    const hooks = await import('@/features/cart/hooks');
    vi.mocked(hooks.useCartItemActions).mockReturnValue({ ...defaultActions });
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

  it('calls handleUpdateQuantity when quantity buttons are clicked', async () => {
    const user = userEvent.setup();
    const handleUpdateQuantity = vi.fn();
    const hooks = await import('@/features/cart/hooks');
    vi.mocked(hooks.useCartItemActions).mockReturnValue({
      ...defaultActions,
      handleUpdateQuantity,
    });

    renderCartItem();

    await user.click(screen.getByRole('button', { name: '+' }));
    expect(handleUpdateQuantity).toHaveBeenCalledWith(3);

    await user.click(screen.getByRole('button', { name: '−' }));
    expect(handleUpdateQuantity).toHaveBeenCalledWith(1);
  });

  it('calls handleRemove when remove button is clicked', async () => {
    const user = userEvent.setup();
    const handleRemove = vi.fn();
    const hooks = await import('@/features/cart/hooks');
    vi.mocked(hooks.useCartItemActions).mockReturnValue({ ...defaultActions, handleRemove });

    renderCartItem();

    await user.click(screen.getByRole('button', { name: /remove/i }));
    expect(handleRemove).toHaveBeenCalled();
  });

  it('disables increase button when max stock is reached', () => {
    renderCartItem({ item: { ...mockItem, quantity: 10, maxStock: 10 } });

    expect(screen.getByRole('button', { name: '+' })).toBeDisabled();
    expect(screen.getByText('Max stock reached')).toBeInTheDocument();
  });

  it('renders read-only quantity when readOnly is true', () => {
    renderCartItem({ readOnly: true });

    expect(screen.getByText('Quantity: 2')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: '−' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: '+' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /remove/i })).not.toBeInTheDocument();
  });

  it('renders image with product link', () => {
    renderCartItem();

    expect(screen.getAllByRole('link', { name: /test product/i }).length).toBeGreaterThan(0);
  });

  it('handles item without compareAtPrice', () => {
    renderCartItem({ item: { ...mockItem, compareAtPrice: undefined } });

    expect(screen.getByText('$29.99')).toBeInTheDocument();
    expect(screen.queryByText('$39.99')).not.toBeInTheDocument();
  });

  it('prevents quantity decrease below 1', async () => {
    const user = userEvent.setup();
    const handleUpdateQuantity = vi.fn();
    const hooks = await import('@/features/cart/hooks');
    vi.mocked(hooks.useCartItemActions).mockReturnValue({
      ...defaultActions,
      handleUpdateQuantity,
    });

    renderCartItem({ item: { ...mockItem, quantity: 1 } });

    await user.click(screen.getByRole('button', { name: '−' }));
    expect(handleUpdateQuantity).toHaveBeenCalledWith(0);
  });

  it('enables increase button when quantity is below max', () => {
    renderCartItem({ item: { ...mockItem, quantity: 5, maxStock: 10 } });

    expect(screen.getByRole('button', { name: '+' })).not.toBeDisabled();
    expect(screen.queryByText('Max stock reached')).not.toBeInTheDocument();
  });

  it('uses default image when image is empty or missing', () => {
    renderCartItem({ item: { ...mockItem, image: '' } });

    const images = screen.getAllByRole('img');
    expect(images.length).toBeGreaterThan(0);
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
    renderCartItem({
      item: { ...mockItem, quantity: 10, maxStock: 10 },
      readOnly: true,
    });

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

    const strikethroughElement = screen.getByText('$39.99');
    expect(strikethroughElement.className).toContain('strikethrough');
  });

  it('disables increase button and shows max stock warning when at max', () => {
    renderCartItem({ item: { ...mockItem, quantity: 10, maxStock: 10 } });

    const increaseButton = screen.getByRole('button', { name: '+' });
    expect(increaseButton).toBeDisabled();
    expect(screen.getByText('Max stock reached')).toBeInTheDocument();
  });
});
