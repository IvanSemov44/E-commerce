import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import { describe, it, expect } from 'vitest';
import OrderCard from './OrderCard';
import type { Order } from './OrderCard.types';

const mockOrder: Order = {
  id: '123',
  orderNumber: 'ORD-2024-001',
  status: 'Pending',
  totalAmount: 299.99,
  createdAt: '2024-03-01T10:00:00Z',
  items: [{ productName: 'Product 1' }, { productName: 'Product 2' }, { productName: 'Product 3' }],
};

const renderOrderCard = (order: Order = mockOrder) => {
  return render(
    <BrowserRouter>
      <OrderCard order={order} />
    </BrowserRouter>
  );
};

describe('OrderCard', () => {
  it('renders order number', () => {
    renderOrderCard();

    expect(screen.getByText(/ORD-2024-001/i)).toBeInTheDocument();
  });

  it('renders order status', () => {
    renderOrderCard();

    expect(screen.getByText(/pending/i)).toBeInTheDocument();
  });

  it('renders total amount', () => {
    renderOrderCard();

    expect(screen.getByText('$299.99')).toBeInTheDocument();
  });

  it('renders formatted date', () => {
    renderOrderCard();

    expect(screen.getByText(/mar/i)).toBeInTheDocument();
  });

  it('renders items count for multiple items', () => {
    renderOrderCard();

    expect(screen.getByText(/orders\.multipleItems|items/i)).toBeInTheDocument();
  });

  it('renders items count for single item', () => {
    const singleItemOrder = {
      ...mockOrder,
      items: [{ productName: 'Product 1' }],
    };
    renderOrderCard(singleItemOrder);

    expect(screen.getByText(/orders\.oneItem|1 item/i)).toBeInTheDocument();
  });

  it('shows preview of first two items', () => {
    renderOrderCard();

    expect(screen.getByText(/Product 1, Product 2/i)).toBeInTheDocument();
  });

  it('shows "more" indicator when more than 2 items', () => {
    renderOrderCard();

    expect(screen.getByText(/\+1/)).toBeInTheDocument();
  });

  it('links to order detail page', () => {
    renderOrderCard();

    const link = screen.getByRole('link');
    expect(link).toHaveAttribute('href', '/orders/123');
  });

  it('renders view details button', () => {
    renderOrderCard();

    expect(screen.getByText(/view details/i)).toBeInTheDocument();
  });

  it('handles empty items array', () => {
    const emptyOrder = { ...mockOrder, items: [] };
    renderOrderCard(emptyOrder);

    expect(screen.queryByText(/Product/)).not.toBeInTheDocument();
  });
});
