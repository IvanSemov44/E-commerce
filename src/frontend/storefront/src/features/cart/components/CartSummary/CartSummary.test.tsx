import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import { describe, it, expect } from 'vitest';
import { CartSummary } from '../CartSummary';

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

    expect(screen.getByText('cart.freeShippingRemaining')).toBeInTheDocument();
  });

  it('does not show free shipping message when above threshold', () => {
    renderCartSummary({ subtotal: 200, freeShippingThreshold: 150 });

    expect(screen.queryByText('cart.freeShippingRemaining')).not.toBeInTheDocument();
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

  it('does not show free shipping message when subtotal equals threshold', () => {
    renderCartSummary({ subtotal: 150, freeShippingThreshold: 150 });

    expect(screen.queryByText('cart.freeShippingRemaining')).not.toBeInTheDocument();
  });

  it('displays zero tax correctly', () => {
    renderCartSummary({ tax: 0 });

    expect(screen.getByText('$0.00')).toBeInTheDocument();
  });

  it('displays large subtotal with correct formatting', () => {
    renderCartSummary({ subtotal: 9999.99 });

    expect(screen.getByText('$9999.99')).toBeInTheDocument();
  });

  it('calculates and displays correct total with all values', () => {
    renderCartSummary({ subtotal: 100, shipping: 15, tax: 9.2, total: 124.2 });

    expect(screen.getByText('$124.20')).toBeInTheDocument();
  });

  it('checkout button has correct href path', () => {
    renderCartSummary();

    const checkoutButton = screen.getByRole('link', { name: /proceed to checkout/i });
    expect(checkoutButton).toHaveAttribute('href', '/checkout');
  });

  it('continue shopping button has correct href path', () => {
    renderCartSummary();

    const shopButton = screen.getByRole('link', { name: /continue shopping/i });
    expect(shopButton).toHaveAttribute('href', '/products');
  });
});
