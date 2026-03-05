import { screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { renderWithProviders as render } from '@/shared/lib/test/test-utils';
import CheckoutAuthBanner from './CheckoutAuthBanner';

describe('CheckoutAuthBanner', () => {
  it('renders guest checkout banner when not authenticated', () => {
    render(<CheckoutAuthBanner />, {
      preloadedState: {
        auth: { isAuthenticated: false, user: null, loading: false, error: null },
      },
    });

    expect(screen.getByText(/guest checkout/i)).toBeInTheDocument();
    expect(screen.getByText(/checking out as a guest/i)).toBeInTheDocument();
  });

  it('renders sign in button for guest users', () => {
    render(<CheckoutAuthBanner />, {
      preloadedState: {
        auth: { isAuthenticated: false, user: null, loading: false, error: null },
      },
    });

    expect(screen.getByRole('link')).toHaveAttribute('href', '/login?redirect=/checkout');
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('renders authenticated banner when user is logged in', () => {
    render(<CheckoutAuthBanner />, {
      preloadedState: {
        auth: {
          isAuthenticated: true,
          user: { id: '1', firstName: 'John', lastName: 'Doe', email: 'john@example.com' },
          loading: false,
          error: null,
        },
      },
    });

    expect(screen.getByText(/welcome back, john/i)).toBeInTheDocument();
    expect(screen.getByText(/john@example.com/i)).toBeInTheDocument();
  });

  it('shows sign out button when authenticated', () => {
    render(<CheckoutAuthBanner />, {
      preloadedState: {
        auth: {
          isAuthenticated: true,
          user: { id: '1', firstName: 'John', lastName: 'Doe', email: 'john@example.com' },
          loading: false,
          error: null,
        },
      },
    });

    expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument();
  });

  it('does not show sign in link when authenticated', () => {
    render(<CheckoutAuthBanner />, {
      preloadedState: {
        auth: {
          isAuthenticated: true,
          user: { id: '1', firstName: 'John', lastName: 'Doe', email: 'john@example.com' },
          loading: false,
          error: null,
        },
      },
    });

    const links = screen.queryAllByRole('link');
    const signInLink = links.find(link => link.getAttribute('href')?.includes('/login'));
    expect(signInLink).toBeUndefined();
  });
});
