import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import CartSummary from '../../pages/components/Cart/CartSummary';

const renderWithRouter = (component: React.ReactElement) => {
  return render(<BrowserRouter>{component}</BrowserRouter>);
};

describe('CartSummary', () => {
  const defaultProps = {
    subtotal: 75.00,
    shipping: 0,
    tax: 6.00,
    total: 81.00,
    freeShippingThreshold: 50,
  };

  it('renders Order Summary title', () => {
    renderWithRouter(<CartSummary {...defaultProps} />);
    expect(screen.getByText('Order Summary')).toBeInTheDocument();
  });

  it('renders subtotal correctly', () => {
    renderWithRouter(<CartSummary {...defaultProps} />);
    expect(screen.getByText('$75.00')).toBeInTheDocument();
  });

  it('renders shipping as FREE when shipping is 0', () => {
    renderWithRouter(<CartSummary {...defaultProps} shipping={0} />);
    expect(screen.getByText('FREE')).toBeInTheDocument();
  });

  it('renders shipping cost when not free', () => {
    renderWithRouter(<CartSummary {...defaultProps} shipping={10.00} />);
    expect(screen.getByText('$10.00')).toBeInTheDocument();
  });

  it('renders tax correctly', () => {
    renderWithRouter(<CartSummary {...defaultProps} />);
    expect(screen.getByText('$6.00')).toBeInTheDocument();
  });

  it('renders grand total correctly', () => {
    renderWithRouter(<CartSummary {...defaultProps} />);
    expect(screen.getByText('$81.00')).toBeInTheDocument();
  });

  it('renders free shipping message when near threshold', () => {
    renderWithRouter(<CartSummary {...defaultProps} subtotal={45} freeShippingThreshold={50} />);
    expect(screen.getByText(/Add \$/)).toBeInTheDocument();
    expect(screen.getByText(/more for free shipping/)).toBeInTheDocument();
  });

  it('does not render free shipping message when already at threshold', () => {
    renderWithRouter(<CartSummary {...defaultProps} subtotal={60} freeShippingThreshold={50} />);
    expect(screen.queryByText(/more for free shipping/)).not.toBeInTheDocument();
  });

  it('renders Proceed to Checkout button', () => {
    renderWithRouter(<CartSummary {...defaultProps} />);
    expect(screen.getByRole('link', { name: /proceed to checkout/i })).toBeInTheDocument();
  });

  it('renders Continue Shopping button', () => {
    renderWithRouter(<CartSummary {...defaultProps} />);
    expect(screen.getByRole('link', { name: /continue shopping/i })).toBeInTheDocument();
  });

  it('has correct href for checkout link', () => {
    renderWithRouter(<CartSummary {...defaultProps} />);
    const link = screen.getByRole('link', { name: /proceed to checkout/i });
    expect(link).toHaveAttribute('href', '/checkout');
  });

  it('has correct href for products link', () => {
    renderWithRouter(<CartSummary {...defaultProps} />);
    const link = screen.getByRole('link', { name: /continue shopping/i });
    expect(link).toHaveAttribute('href', '/products');
  });
});
