import { screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { renderWithProviders as render } from '@/shared/lib/test/test-utils';
import { OrderSuccess } from './OrderSuccess';

describe('OrderSuccess', () => {
  it('renders success message', () => {
    render(<OrderSuccess orderNumber="12345" email="customer@example.com" isGuestOrder={false} />);

    expect(screen.getByRole('heading', { level: 2 })).toBeInTheDocument();
  });

  it('displays order number', () => {
    render(
      <OrderSuccess orderNumber="ORD-2024-001" email="customer@example.com" isGuestOrder={false} />
    );

    expect(screen.getByText(/ORD-2024-001/)).toBeInTheDocument();
  });

  it('renders confirmation message section', () => {
    render(<OrderSuccess orderNumber="12345" email="test@example.com" isGuestOrder={false} />);

    expect(screen.getByText(/confirmation email/i)).toBeInTheDocument();
  });

  it('renders action links to orders and products', () => {
    render(<OrderSuccess orderNumber="12345" email="customer@example.com" isGuestOrder={false} />);

    expect(screen.getByRole('link', { name: 'View Orders' })).toHaveAttribute('href', '/orders');
    expect(screen.getByRole('link', { name: 'Continue Shopping' })).toHaveAttribute(
      'href',
      '/products'
    );
  });

  it('renders success icon', () => {
    const { container } = render(
      <OrderSuccess orderNumber="12345" email="customer@example.com" isGuestOrder={false} />
    );

    expect(container.querySelector('svg')).toBeInTheDocument();
  });

  it('shows guest CTA with sign-up link when isGuestOrder is true', () => {
    render(<OrderSuccess orderNumber="12345" email="guest@example.com" isGuestOrder={true} />);

    expect(screen.getByRole('link', { name: /auth.signUp/i })).toHaveAttribute('href', '/register');
  });

  it('does not show guest CTA when isGuestOrder is false', () => {
    render(<OrderSuccess orderNumber="12345" email="member@example.com" isGuestOrder={false} />);

    expect(screen.queryByRole('link', { name: /auth.signUp/i })).not.toBeInTheDocument();
  });

  it('displays the email in the confirmation text', () => {
    render(<OrderSuccess orderNumber="12345" email="specific@example.com" isGuestOrder={false} />);

    expect(screen.getByText(/specific@example\.com/)).toBeInTheDocument();
  });
});
