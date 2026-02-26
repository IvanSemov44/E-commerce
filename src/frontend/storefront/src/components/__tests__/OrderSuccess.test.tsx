import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import OrderSuccess from '../../pages/components/Checkout/OrderSuccess';

const renderWithRouter = (component: React.ReactElement) => {
  return render(<BrowserRouter>{component}</BrowserRouter>);
};

describe('OrderSuccess', () => {
  const defaultProps = {
    orderNumber: 'ORD-12345',
    email: 'test@example.com',
  };

  it('renders success title', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} />);
    expect(screen.getByText('Order Placed Successfully!')).toBeInTheDocument();
  });

  it('renders thank you message', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} />);
    expect(screen.getByText('Thank you for your purchase.')).toBeInTheDocument();
  });

  it('renders order number', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} />);
    expect(screen.getByText('Order Number: ORD-12345')).toBeInTheDocument();
  });

  it('renders confirmation email message with provided email', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} email="user@test.com" />);
    expect(screen.getByText(/confirmation email has been sent to user@test.com/)).toBeInTheDocument();
  });

  it('renders default email message when email is not provided', () => {
    renderWithRouter(<OrderSuccess orderNumber="ORD-123" email="" />);
    expect(screen.getByText(/confirmation email has been sent to your email/)).toBeInTheDocument();
  });

  it('renders Continue Shopping button', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} />);
    expect(screen.getByRole('link', { name: /continue shopping/i })).toBeInTheDocument();
  });

  it('renders Return Home button', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} />);
    expect(screen.getByRole('link', { name: /return home/i })).toBeInTheDocument();
  });

  it('does not render guest prompt when isGuestOrder is false', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} isGuestOrder={false} />);
    expect(screen.queryByText('Create an Account')).not.toBeInTheDocument();
  });

  it('renders guest prompt when isGuestOrder is true', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} isGuestOrder={true} />);
    expect(screen.getByText('Create an Account')).toBeInTheDocument();
    expect(screen.getByText(/Create an account to track your orders/)).toBeInTheDocument();
  });

  it('renders Create Account button in guest prompt', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} isGuestOrder={true} />);
    expect(screen.getByRole('link', { name: /create account/i })).toBeInTheDocument();
  });

  it('has correct href for Continue Shopping link', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} />);
    const link = screen.getByRole('link', { name: /continue shopping/i });
    expect(link).toHaveAttribute('href', '/products');
  });

  it('has correct href for Return Home link', () => {
    renderWithRouter(<OrderSuccess {...defaultProps} />);
    const link = screen.getByRole('link', { name: /return home/i });
    expect(link).toHaveAttribute('href', '/');
  });
});
