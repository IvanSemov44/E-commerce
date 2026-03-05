import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import OrderItemsList from './OrderItemsList';

describe('OrderItemsList', () => {
  const mockItems = [
    {
      id: '1',
      productName: 'Product 1',
      productImageUrl: 'image1.jpg',
      quantity: 2,
      unitPrice: 29.99,
      totalPrice: 59.98,
    },
    {
      id: '2',
      productName: 'Product 2',
      productImageUrl: 'image2.jpg',
      quantity: 1,
      unitPrice: 49.99,
      totalPrice: 49.99,
    },
  ];

  it('renders ordered items', () => {
    render(<OrderItemsList items={mockItems} />);

    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('displays product images', () => {
    render(<OrderItemsList items={mockItems} />);

    const images = screen.getAllByRole('img');
    expect(images.length).toBeGreaterThan(0);
  });

  it('displays quantity and unit price', () => {
    render(<OrderItemsList items={mockItems} />);

    expect(screen.getByText(/qty: 2 x \$29.99/i)).toBeInTheDocument();
    expect(screen.getByText(/qty: 1 x \$49.99/i)).toBeInTheDocument();
  });

  it('renders empty list when items array is empty', () => {
    const { container } = render(<OrderItemsList items={[]} />);

    expect(container.firstChild?.childNodes.length).toBe(0);
  });

  it('renders items without images', () => {
    const itemsWithoutImages = [
      {
        id: '1',
        productName: 'Product 1',
        productImageUrl: '',
        quantity: 2,
        unitPrice: 29.99,
        totalPrice: 59.98,
      },
    ];

    render(<OrderItemsList items={itemsWithoutImages} />);

    expect(screen.getByText('Product 1')).toBeInTheDocument();
  });
});
