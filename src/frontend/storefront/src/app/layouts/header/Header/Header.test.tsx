import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import Header from './Header';

const dispatchMock = vi.fn();
const navigateMock = vi.fn();
const getCartQueryMock = vi.fn();
const getWishlistQueryMock = vi.fn();

type MockState = {
  auth: {
    isAuthenticated: boolean;
    user: { firstName: string; email: string } | null;
  };
  cart: { items: Array<{ quantity: number }> };
};

let mockState: MockState;

vi.mock('@/shared/lib/store', () => ({
  useAppSelector: (selector: (state: MockState) => unknown) => selector(mockState),
  useAppDispatch: () => dispatchMock,
}));

vi.mock('@/features/auth/slices/authSlice', async () => {
  const actual = await vi.importActual<typeof import('@/features/auth/slices/authSlice')>(
    '@/features/auth/slices/authSlice'
  );
  return {
    ...actual,
    // bypass reselect memoisation so each test sees fresh state
    selectIsAuthenticated: (state: MockState) => state.auth.isAuthenticated,
    selectCurrentUser: (state: MockState) => state.auth.user,
  };
});

vi.mock('@/features/cart/slices/cartSlice', async () => {
  const actual = await vi.importActual<typeof import('@/features/cart/slices/cartSlice')>(
    '@/features/cart/slices/cartSlice'
  );
  return {
    ...actual,
    selectCartItemCount: (state: MockState) =>
      state.cart.items.reduce((sum: number, item: { quantity: number }) => sum + item.quantity, 0),
  };
});

vi.mock('@/features/cart/api/cartApi', () => ({
  useGetCartQuery: (...args: unknown[]) => getCartQueryMock(...args),
}));

vi.mock('@/features/wishlist/api/wishlistApi', () => ({
  useGetWishlistQuery: (...args: unknown[]) => getWishlistQueryMock(...args),
}));

vi.mock('@/shared/components/icons', () => {
  const Icon = ({ className }: { className?: string }) => <span className={className}>icon</span>;
  return {
    HeartIcon: Icon,
    ShoppingCartIcon: Icon,
    ChevronDownIcon: Icon,
    UserIcon: Icon,
    LogoutIcon: Icon,
    MenuIcon: Icon,
    CloseIcon: Icon,
    PackageIcon: Icon,
    DocumentIcon: Icon,
  };
});

vi.mock('@/app/ThemeToggle', () => ({
  ThemeToggle: () => <div data-testid="theme-toggle" />,
}));

vi.mock('@/app/LanguageSwitcher', () => ({
  LanguageSwitcher: () => <div data-testid="language-switcher" />,
}));

vi.mock('@/app/SearchBar', () => ({
  SearchBar: () => <div data-testid="search-bar" />,
}));

// Stub HeaderUserMenu — isolate from its own Redux + i18n dependencies
vi.mock('../HeaderUserMenu', () => ({
  default: ({
    isOpen,
    onToggle,
    onLogout,
  }: {
    isOpen: boolean;
    onToggle: () => void;
    onClose: () => void;
    onLogout: () => void;
  }) => (
    <div>
      <button aria-label="User menu" onClick={onToggle}>
        User menu
      </button>
      {isOpen && (
        <div>
          <p>Account</p>
          <button onClick={onLogout}>Sign Out</button>
        </div>
      )}
    </div>
  ),
}));

// Stub HeaderMobileMenu — isolate from its own Redux + i18n dependencies
vi.mock('../HeaderMobileMenu', () => ({
  default: ({ onClose, onLogout }: { onClose: () => void; onLogout: () => void }) => (
    <div>
      <a href="/login" onClick={onClose}>
        Sign In
      </a>
      <a href="/register" onClick={onClose}>
        Sign Up
      </a>
      <button onClick={onLogout}>Sign Out</button>
    </div>
  ),
}));

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return { ...actual, useNavigate: () => navigateMock };
});

function renderHeader() {
  return render(
    <MemoryRouter>
      <Header />
    </MemoryRouter>
  );
}

describe('Header', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockState = {
      auth: { isAuthenticated: false, user: null },
      cart: { items: [{ quantity: 2 }, { quantity: 1 }] },
    };

    getCartQueryMock.mockReturnValue({ data: undefined });
    getWishlistQueryMock.mockReturnValue({ data: undefined });
  });

  it('renders public navigation for unauthenticated user', () => {
    renderHeader();

    expect(screen.getByText('E-Shop')).toBeInTheDocument();
    expect(screen.getByText('Products')).toBeInTheDocument();
    expect(screen.queryByText('Orders')).not.toBeInTheDocument();
    expect(screen.getByText('Sign In')).toBeInTheDocument();
    expect(screen.getByText('Sign Up')).toBeInTheDocument();
  });

  it('shows local cart count when unauthenticated', () => {
    renderHeader();
    expect(screen.getAllByText('3').length).toBeGreaterThan(0);
  });

  it('renders authenticated nav links and backend counts', () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'Ivan', email: 'ivan@example.com' },
    };
    getCartQueryMock.mockReturnValue({
      data: { items: [{ quantity: 4 }, { quantity: 3 }] },
    });
    getWishlistQueryMock.mockReturnValue({
      data: { items: [{ id: '1' }, { id: '2' }, { id: '3' }] },
    });

    renderHeader();

    expect(screen.getByText('Orders')).toBeInTheDocument();
    expect(screen.getAllByText('7').length).toBeGreaterThan(0);
    expect(screen.getAllByText('3').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: 'Sign In' })).not.toBeInTheDocument();
  });

  it('opens user menu and logs out from desktop menu', () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'Ivan', email: 'ivan@example.com' },
    };

    renderHeader();

    // desktopRight has display:none by default (mobile-first CSS); use hidden:true
    fireEvent.click(screen.getByRole('button', { name: /user menu/i, hidden: true }));

    expect(screen.getByText('Account')).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole('button', { name: /sign out/i, hidden: true })[0]);

    expect(dispatchMock).toHaveBeenCalledWith(expect.objectContaining({ type: 'auth/logout' }));
    expect(navigateMock).toHaveBeenCalledWith('/');
  });

  it('toggles mobile menu and shows mobile auth links for guests', () => {
    renderHeader();

    fireEvent.click(screen.getByRole('button', { name: /toggle menu/i }));

    expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /sign up/i })).toBeInTheDocument();
  });

  it('logs out from mobile menu for authenticated user', () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'Ivan', email: 'ivan@example.com' },
    };

    renderHeader();

    fireEvent.click(screen.getByRole('button', { name: /toggle menu/i }));
    fireEvent.click(screen.getAllByRole('button', { name: /sign out/i })[0]);

    expect(dispatchMock).toHaveBeenCalledWith(expect.objectContaining({ type: 'auth/logout' }));
    expect(navigateMock).toHaveBeenCalledWith('/');
  });

  it('requests cart and wishlist with authenticated skip flags', () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'Ivan', email: 'ivan@example.com' },
    };

    renderHeader();

    expect(getCartQueryMock).toHaveBeenCalledWith(
      undefined,
      expect.objectContaining({ skip: false })
    );
    expect(getWishlistQueryMock).toHaveBeenCalledWith(
      undefined,
      expect.objectContaining({ skip: false })
    );
  });
});
