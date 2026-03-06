import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect } from 'vitest';
import CartSummary from '../CartSummary';

const renderCartSummary = (props: Partial<React.ComponentProps<typeof CartSummary>> = {}) => {
  const defaultProps = {
    subtotal: 100,
    shipping: 10,
    tax: 8,
    total: 118,
    freeShippingThreshold: 150,
  };

  return render(
    <BrowserRouter>
      <CartSummary {...defaultProps} {...props} />
    </BrowserRouter>
  );
};

describe('CartSummary', () => {
  it('renders order summary title', () => {
    renderCartSummary();

    expect(screen.getByRole('heading', { level: 2 })).toBeInTheDocument();
  });

  it('displays subtotal correctly', () => {
    renderCartSummary({ subtotal: 100 });

    expect(screen.getByText('$100.00')).toBeInTheDocument();
  });

  it('displays shipping cost', () => {
    renderCartSummary({ shipping: 10 });

    expect(screen.getByText('$10.00')).toBeInTheDocument();
  });

  it('displays free shipping when shipping is zero', () => {
    renderCartSummary({ shipping: 0 });

    expect(screen.getByText(/^free$/i)).toBeInTheDocument();
  });

  it('displays tax amount', () => {
    renderCartSummary({ tax: 8 });

    expect(screen.getByText('$8.00')).toBeInTheDocument();
  });

  it('displays grand total', () => {
    renderCartSummary({ total: 118 });

    expect(screen.getByText('$118.00')).toBeInTheDocument();
  });

  it('shows free shipping message when below threshold', () => {
    renderCartSummary({ subtotal: 100, freeShippingThreshold: 150 });

    expect(screen.getByText(/add \$50\.00 more for free shipping/i)).toBeInTheDocument();
  });

  it('does not show free shipping message when above threshold', () => {
    renderCartSummary({ subtotal: 200, freeShippingThreshold: 150 });

    expect(screen.queryByText(/add.*more for free shipping/i)).not.toBeInTheDocument();
  });

  it('does not show free shipping message when subtotal is zero', () => {
    renderCartSummary({ subtotal: 0, freeShippingThreshold: 150 });

    expect(screen.queryByText(/add.*more for free shipping/i)).not.toBeInTheDocument();
  });

  it('renders proceed to checkout button', () => {
    renderCartSummary();

    const checkoutLink = screen.getByRole('link', { name: /proceed to checkout/i });
    expect(checkoutLink).toHaveAttribute('href', '/checkout');
  });

  it('renders continue shopping button', () => {
    renderCartSummary();

    const shopLink = screen.getByRole('link', { name: /continue shopping/i });
    expect(shopLink).toHaveAttribute('href', '/products');
  });
});
