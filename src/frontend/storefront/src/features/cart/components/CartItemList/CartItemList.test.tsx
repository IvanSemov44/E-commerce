import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { CartItemList } from './CartItemList';
import type { DisplayCartItem } from '../CartItemList';

vi.mock('@/features/cart/hooks', () => ({
  useCartItemActions: vi.fn(),
}));

const mockItems: DisplayCartItem[] = [
  {
    id: '1',
    name: 'Laptop',
    slug: 'laptop',
    price: 999.99,
    quantity: 1,
    maxStock: 5,
    image: 'http://example.com/laptop.jpg',
    compareAtPrice: 1299.99,
  },
  {
    id: '2',
    name: 'Mouse',
    slug: 'mouse',
    price: 29.99,
    quantity: 2,
    maxStock: 20,
    image: 'http://example.com/mouse.jpg',
  },
];

describe('CartItemList', () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    const hooks = await import('@/features/cart/hooks');
    vi.mocked(hooks.useCartItemActions).mockReturnValue({
      handleUpdateQuantity: vi.fn(),
      handleRemove: vi.fn(),
    });
  });

  it('renders cart title', () => {
    render(
      <BrowserRouter>
        <CartItemList items={mockItems} />
      </BrowserRouter>
    );

    expect(screen.getByRole('heading')).toBeInTheDocument();
  });

  it('renders all cart items', () => {
    render(
      <BrowserRouter>
        <CartItemList items={mockItems} />
      </BrowserRouter>
    );

    expect(screen.getByText('Laptop')).toBeInTheDocument();
    expect(screen.getByText('Mouse')).toBeInTheDocument();
  });

  it('displays correct item count in title', () => {
    render(
      <BrowserRouter>
        <CartItemList items={mockItems} />
      </BrowserRouter>
    );

    expect(screen.getByRole('heading')).toHaveTextContent('2');
  });

  it('renders empty list when no items', () => {
    render(
      <BrowserRouter>
        <CartItemList items={[]} />
      </BrowserRouter>
    );

    expect(screen.queryByText('Laptop')).not.toBeInTheDocument();
  });

  it('renders items in correct order', () => {
    render(
      <BrowserRouter>
        <CartItemList items={mockItems} />
      </BrowserRouter>
    );

    const items = screen.getAllByText(/Laptop|Mouse/);
    expect(items[0]).toHaveTextContent('Laptop');
  });
});
