import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import OrderTotals from '../../pages/components/OrderDetail/OrderTotals';

describe('OrderTotals', () => {
  it('renders order summary title', () => {
    render(<OrderTotals />);
    expect(screen.getByText('Order Summary')).toBeInTheDocument();
  });

  it('renders subtotal formatted correctly', () => {
    render(<OrderTotals subtotal={100.00} />);
    expect(screen.getByText('$100.00')).toBeInTheDocument();
  });

  it('renders shipping amount formatted correctly', () => {
    render(<OrderTotals shippingAmount={15.50} />);
    expect(screen.getByText('$15.50')).toBeInTheDocument();
  });

  it('renders tax amount formatted correctly', () => {
    render(<OrderTotals taxAmount={8.75} />);
    expect(screen.getByText('$8.75')).toBeInTheDocument();
  });

  it('renders grand total formatted correctly', () => {
    render(<OrderTotals totalAmount={124.25} />);
    expect(screen.getByText('$124.25')).toBeInTheDocument();
  });

  it('renders discount amount when provided and positive', () => {
    render(<OrderTotals discountAmount={10.00} subtotal={100} />);
    expect(screen.getByText('-$10.00')).toBeInTheDocument();
    expect(screen.getByText('Saved')).toBeInTheDocument();
  });

  it('does not render discount when amount is zero', () => {
    render(<OrderTotals discountAmount={0} subtotal={100} />);
    expect(screen.queryByText('Discount')).not.toBeInTheDocument();
  });

  it('does not render discount when amount is undefined', () => {
    render(<OrderTotals discountAmount={undefined} subtotal={100} />);
    expect(screen.queryByText('Discount')).not.toBeInTheDocument();
  });

  it('renders $0.00 for undefined values', () => {
    render(<OrderTotals />);
    // Should render multiple $0.00 for subtotal, shipping, tax, and total
    const zeroValues = screen.getAllByText('$0.00');
    expect(zeroValues.length).toBeGreaterThan(0);
  });

  it('renders all line items with correct labels', () => {
    render(<OrderTotals subtotal={50.00} shippingAmount={5.00} taxAmount={4.50} />);
    expect(screen.getByText('Subtotal')).toBeInTheDocument();
    expect(screen.getByText('Shipping')).toBeInTheDocument();
    expect(screen.getByText('Tax')).toBeInTheDocument();
    expect(screen.getByText('Total')).toBeInTheDocument();
  });

  it('handles decimal precision correctly', () => {
    render(<OrderTotals subtotal={33.33} shippingAmount={5.55} taxAmount={3.11} totalAmount={42.00} />);
    expect(screen.getByText('$33.33')).toBeInTheDocument();
    expect(screen.getByText('$5.55')).toBeInTheDocument();
    expect(screen.getByText('$3.11')).toBeInTheDocument();
    expect(screen.getByText('$42.00')).toBeInTheDocument();
  });
});
