import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { CartItemList } from './CartItemList';
import type { CartItem } from '@/features/cart/types';

// CartItem's Redux/RTK Query dependencies are irrelevant to CartItemList behavior.
// Mock CartItem to isolate the list component tests.
vi.mock('@/features/cart/components/CartItem/CartItem', () => ({
  CartItem: ({ item }: { item: CartItem }) => <div data-testid="cart-item">{item.name}</div>,
}));

const mockItems: CartItem[] = [
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
  beforeEach(() => {
    vi.clearAllMocks();
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

    const items = screen.getAllByTestId('cart-item');
    expect(items[0]).toHaveTextContent('Laptop');
    expect(items[1]).toHaveTextContent('Mouse');
  });

  it('uses singular item text when cart has one item', () => {
    const singleItem = [mockItems[0]];

    render(
      <BrowserRouter>
        <CartItemList items={singleItem} />
      </BrowserRouter>
    );

    const heading = screen.getByRole('heading');
    expect(heading.textContent).toContain('1');
    expect(heading.textContent).toMatch(/product/i);
  });
});
