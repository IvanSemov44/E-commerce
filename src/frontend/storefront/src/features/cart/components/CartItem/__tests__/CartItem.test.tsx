import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';
import CartItem from '../CartItem';
import type { CartItem as CartItemType } from '../../../slices/cartSlice';

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
      <CartItem
        item={mockItem}
        onUpdateQuantity={vi.fn()}
        onRemove={vi.fn()}
        {...props}
      />
    </BrowserRouter>
  );
};

describe('CartItem', () => {
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

  it('calls onUpdateQuantity when quantity buttons are clicked', async () => {
    const user = userEvent.setup();
    const onUpdateQuantity = vi.fn();
    renderCartItem({ onUpdateQuantity });

    const increaseButton = screen.getByRole('button', { name: '+' });
    await user.click(increaseButton);

    expect(onUpdateQuantity).toHaveBeenCalledWith('1', 3);

    const decreaseButton = screen.getByRole('button', { name: '−' });
    await user.click(decreaseButton);

    expect(onUpdateQuantity).toHaveBeenCalledWith('1', 1);
  });

  it('calls onRemove when remove button is clicked', async () => {
    const user = userEvent.setup();
    const onRemove = vi.fn();
    renderCartItem({ onRemove });

    const removeButton = screen.getByRole('button', { name: /remove/i });
    await user.click(removeButton);

    expect(onRemove).toHaveBeenCalledWith('1');
  });

  it('disables increase button when max stock is reached', () => {
    const maxStockItem = { ...mockItem, quantity: 10, maxStock: 10 };
    renderCartItem({ item: maxStockItem });

    const increaseButton = screen.getByRole('button', { name: '+' });
    expect(increaseButton).toBeDisabled();
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

    const imageLinks = screen.getAllByRole('link', { name: /test product/i });
    expect(imageLinks.length).toBeGreaterThan(0);
  });

  it('handles item without compareAtPrice', () => {
    const itemWithoutCompare = { ...mockItem, compareAtPrice: undefined };
    renderCartItem({ item: itemWithoutCompare });

    expect(screen.getByText('$29.99')).toBeInTheDocument();
    expect(screen.queryByText('$39.99')).not.toBeInTheDocument();
  });
});
