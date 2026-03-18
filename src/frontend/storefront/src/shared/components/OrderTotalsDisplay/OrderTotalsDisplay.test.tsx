import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { OrderTotalsDisplay } from './OrderTotalsDisplay';

describe('OrderTotalsDisplay', () => {
  it('displays subtotal label and value', () => {
    render(<OrderTotalsDisplay subtotal={100} shipping={10} tax={8} total={118} />);
    expect(screen.getByText('Subtotal')).toBeInTheDocument();
    expect(screen.getByText('$100.00')).toBeInTheDocument();
  });

  it('displays shipping cost', () => {
    render(<OrderTotalsDisplay subtotal={100} shipping={10} tax={8} total={118} />);
    expect(screen.getByText('Shipping')).toBeInTheDocument();
    expect(screen.getByText('$10.00')).toBeInTheDocument();
  });

  it('displays "Free" when shipping is zero', () => {
    render(<OrderTotalsDisplay subtotal={100} shipping={0} tax={8} total={108} />);
    expect(screen.getByText('Free')).toBeInTheDocument();
  });

  it('displays tax', () => {
    render(<OrderTotalsDisplay subtotal={100} shipping={10} tax={8} total={118} />);
    expect(screen.getByText('Tax')).toBeInTheDocument();
    expect(screen.getByText('$8.00')).toBeInTheDocument();
  });

  it('displays total', () => {
    render(<OrderTotalsDisplay subtotal={100} shipping={10} tax={8} total={118} />);
    expect(screen.getByText('Total')).toBeInTheDocument();
    expect(screen.getByText('$118.00')).toBeInTheDocument();
  });

  it('shows discount row when discount > 0', () => {
    render(<OrderTotalsDisplay subtotal={100} shipping={10} tax={8} total={98} discount={20} />);
    expect(screen.getByText('Discount')).toBeInTheDocument();
    expect(screen.getByText('-$20.00')).toBeInTheDocument();
  });

  it('hides discount row when discount is 0', () => {
    render(<OrderTotalsDisplay subtotal={100} shipping={10} tax={8} total={118} discount={0} />);
    expect(screen.queryByText('Discount')).not.toBeInTheDocument();
  });

  it('hides discount row when discount is not provided', () => {
    render(<OrderTotalsDisplay subtotal={100} shipping={10} tax={8} total={118} />);
    expect(screen.queryByText('Discount')).not.toBeInTheDocument();
  });

  it('applies custom className to root element', () => {
    const { container } = render(
      <OrderTotalsDisplay
        subtotal={100}
        shipping={10}
        tax={8}
        total={118}
        className="custom-class"
      />
    );
    expect(container.firstChild).toHaveClass('custom-class');
  });

  it('formats prices with two decimal places', () => {
    render(<OrderTotalsDisplay subtotal={99.9} shipping={5.5} tax={3.33} total={108.73} />);
    expect(screen.getByText('$99.90')).toBeInTheDocument();
    expect(screen.getByText('$5.50')).toBeInTheDocument();
    expect(screen.getByText('$3.33')).toBeInTheDocument();
    expect(screen.getByText('$108.73')).toBeInTheDocument();
  });
});
