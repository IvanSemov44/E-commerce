import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider } from '@/app/providers/ThemeProvider';
import HeaderMobileMenu from './HeaderMobileMenu';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

const defaultProps = {
  isAuthenticated: false,
  user: null,
  cartItemCount: 0,
  wishlistItemCount: 0,
  onClose: vi.fn(),
  onLogout: vi.fn(),
};

const renderMenu = (props = {}) => {
  return render(
    <ThemeProvider>
      <BrowserRouter>
        <HeaderMobileMenu {...defaultProps} {...props} />
      </BrowserRouter>
    </ThemeProvider>
  );
};

describe('HeaderMobileMenu', () => {
  it('renders sign in and sign up buttons for unauthenticated user', () => {
    renderMenu();
    expect(screen.getByText(/Sign In/i)).toBeInTheDocument();
    expect(screen.getByText(/Sign Up/i)).toBeInTheDocument();
  });

  it('renders user info for authenticated user', () => {
    const user = { firstName: 'John', email: 'john@example.com', role: 'Customer' };
    renderMenu({ isAuthenticated: true, user });
    expect(screen.getByText(/John/)).toBeInTheDocument();
    expect(screen.getByText(/john@example.com/)).toBeInTheDocument();
  });

  it('displays cart badge when items exist', () => {
    renderMenu({ cartItemCount: 5 });
    expect(screen.getByText(/5/)).toBeInTheDocument();
  });

  it('displays 99+ when cart count exceeds 99', () => {
    renderMenu({ cartItemCount: 150 });
    expect(screen.getByText(/99+/)).toBeInTheDocument();
  });
});
