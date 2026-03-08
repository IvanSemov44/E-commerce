import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import OrderTotals from './OrderTotals';

describe('OrderTotals', () => {
  it('displays subtotal', () => {
    render(<OrderTotals subtotal={100} shippingAmount={10} taxAmount={8.8} totalAmount={118.8} />);

    expect(screen.getByText(/subtotal:/i)).toBeInTheDocument();
    expect(screen.getByText('$100.00')).toBeInTheDocument();
  });

  it('displays shipping amount', () => {
    render(<OrderTotals subtotal={100} shippingAmount={10} taxAmount={8.8} totalAmount={118.8} />);

    expect(screen.getByText(/shipping:/i)).toBeInTheDocument();
    expect(screen.getByText('$10.00')).toBeInTheDocument();
  });

  it('displays tax amount', () => {
    render(<OrderTotals subtotal={100} shippingAmount={10} taxAmount={8.8} totalAmount={118.8} />);

    expect(screen.getByText(/tax:/i)).toBeInTheDocument();
    expect(screen.getByText('$8.80')).toBeInTheDocument();
  });

  it('displays total amount', () => {
    render(<OrderTotals subtotal={100} shippingAmount={10} taxAmount={8.8} totalAmount={118.8} />);

    expect(screen.getAllByText(/total:/i).length).toBeGreaterThan(0);
    expect(screen.getByText('$118.80')).toBeInTheDocument();
  });

  it('shows discount when discountAmount > 0', () => {
    render(
      <OrderTotals
        subtotal={100}
        discountAmount={20}
        shippingAmount={10}
        taxAmount={8}
        totalAmount={98}
      />
    );

    expect(screen.getByText(/discount:/i)).toBeInTheDocument();
    expect(screen.getByText('-$20.00')).toBeInTheDocument();
  });

  it('does not show discount when discountAmount is 0', () => {
    render(
      <OrderTotals
        subtotal={100}
        discountAmount={0}
        shippingAmount={10}
        taxAmount={8.8}
        totalAmount={118.8}
      />
    );

    expect(screen.queryByText(/discount:/i)).not.toBeInTheDocument();
  });
});
