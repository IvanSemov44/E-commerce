import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import HeaderMobileMenu from './HeaderMobileMenu';

type MockState = {
  auth: {
    isAuthenticated: boolean;
    user: { firstName: string; email: string } | null;
  };
};

let mockState: MockState = {
  auth: { isAuthenticated: false, user: null },
};

const mockHeaderData = { cartItemCount: 0, wishlistItemCount: 0 };

vi.mock('@/shared/lib/store', () => ({
  useAppSelector: (selector: (state: MockState) => unknown) => selector(mockState),
}));

vi.mock('../useHeaderData', () => ({
  useHeaderData: () => mockHeaderData,
}));

vi.mock('@/app/ThemeToggle', () => ({
  ThemeToggle: () => <div />,
}));

vi.mock('@/shared/components/icons', () => ({
  HeartIcon: () => null,
  ShoppingCartIcon: () => null,
  UserIcon: () => null,
  LogoutIcon: () => null,
  PackageIcon: () => null,
  DocumentIcon: () => null,
}));

const defaultProps = {
  onClose: vi.fn(),
  onLogout: vi.fn(),
};

const renderMenu = (overrides = {}) =>
  render(
    <BrowserRouter>
      <HeaderMobileMenu {...defaultProps} {...overrides} />
    </BrowserRouter>
  );

describe('HeaderMobileMenu', () => {
  beforeEach(() => {
    mockState = { auth: { isAuthenticated: false, user: null } };
    mockHeaderData.cartItemCount = 0;
    mockHeaderData.wishlistItemCount = 0;
    vi.clearAllMocks();
  });

  it('renders sign in and sign up links for unauthenticated user', () => {
    renderMenu();
    expect(screen.getByText(/Sign In/i)).toBeInTheDocument();
    expect(screen.getByText(/Sign Up/i)).toBeInTheDocument();
  });

  it('renders user info for authenticated user', () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'John', email: 'john@example.com' },
    };
    renderMenu();
    expect(screen.getByText('John')).toBeInTheDocument();
    expect(screen.getByText('john@example.com')).toBeInTheDocument();
  });

  it('does not show sign in / sign up for authenticated user', () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'John', email: 'john@example.com' },
    };
    renderMenu();
    expect(screen.queryByText(/Sign In/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Sign Up/i)).not.toBeInTheDocument();
  });

  it('displays cart badge when items exist', () => {
    mockHeaderData.cartItemCount = 5;
    renderMenu();
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('displays 99+ when cart count exceeds 99', () => {
    mockHeaderData.cartItemCount = 150;
    renderMenu();
    expect(screen.getByText('99+')).toBeInTheDocument();
  });
});
