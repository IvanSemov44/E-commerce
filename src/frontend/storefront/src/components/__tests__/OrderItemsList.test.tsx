import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import OrderItemsList from '../../pages/components/OrderDetail/OrderItemsList';

describe('OrderItemsList', () => {
  const mockItems = [
    {
      productName: 'Test Product 1',
      productImageUrl: 'https://example.com/image1.jpg',
      quantity: 2,
      unitPrice: 29.99,
      totalPrice: 59.98,
    },
    {
      productName: 'Test Product 2',
      productImageUrl: undefined,
      quantity: 1,
      unitPrice: 15.50,
      totalPrice: 15.50,
    },
    {
      productName: 'Test Product 3',
      productImageUrl: 'https://example.com/image3.jpg',
      quantity: 3,
      unitPrice: 10.00,
      totalPrice: 30.00,
    },
  ];

  it('renders the component title', () => {
    render(<OrderItemsList items={mockItems} />);
    expect(screen.getByText('Order Items')).toBeInTheDocument();
  });

  it('renders all items', () => {
    render(<OrderItemsList items={mockItems} />);
    expect(screen.getByText('Test Product 1')).toBeInTheDocument();
    expect(screen.getByText('Test Product 2')).toBeInTheDocument();
    expect(screen.getByText('Test Product 3')).toBeInTheDocument();
  });

  it('renders product images when provided', () => {
    render(<OrderItemsList items={mockItems} />);
    const images = screen.getAllByRole('img');
    // Two items have images (first and third)
    expect(images).toHaveLength(2);
    expect(images[0]).toHaveAttribute('src', 'https://example.com/image1.jpg');
    expect(images[0]).toHaveAttribute('alt', 'Test Product 1');
    expect(images[1]).toHaveAttribute('src', 'https://example.com/image3.jpg');
    expect(images[1]).toHaveAttribute('alt', 'Test Product 3');
  });

  it('renders quantity badges', () => {
    render(<OrderItemsList items={mockItems} />);
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('renders unit prices formatted correctly', () => {
    render(<OrderItemsList items={mockItems} />);
    expect(screen.getByText('$29.99')).toBeInTheDocument();
    expect(screen.getByText('$15.50')).toBeInTheDocument();
    expect(screen.getByText('$10.00')).toBeInTheDocument();
  });

  it('renders total prices formatted correctly', () => {
    render(<OrderItemsList items={mockItems} />);
    expect(screen.getByText('$59.98')).toBeInTheDocument();
    expect(screen.getByText('$15.50')).toBeInTheDocument();
    expect(screen.getByText('$30.00')).toBeInTheDocument();
  });

  it('renders correctly when items array is empty', () => {
    render(<OrderItemsList items={[]} />);
    // Should still render title
    expect(screen.getByText('Order Items')).toBeInTheDocument();
    // No items should be rendered
    expect(screen.queryByText('Test Product 1')).not.toBeInTheDocument();
  });

  it('handles missing prices gracefully', () => {
    const itemsWithMissingPrices = [
      {
        productName: 'Product without price',
        quantity: 1,
        unitPrice: undefined,
        totalPrice: undefined,
      },
    ];
    render(<OrderItemsList items={itemsWithMissingPrices} />);
    expect(screen.getByText('$0.00')).toBeInTheDocument(); // unit price fallback
    expect(screen.getAllByText('$0.00')).toHaveLength(2); // unit and total
  });
});