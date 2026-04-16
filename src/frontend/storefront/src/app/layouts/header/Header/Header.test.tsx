import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, act } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { Header } from './Header';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const dispatchMock = vi.fn();
const navigateMock = vi.fn();

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

vi.mock('@/features/auth/api/authApi', () => ({
  useLogoutMutation: () => [vi.fn().mockResolvedValue({}), { isLoading: false }],
}));

let mockCartData: { items: Array<{ quantity: number }> } | undefined = undefined;
let mockWishlistData: { items: Array<{ id: string }> } | undefined = undefined;

vi.mock('@/features/cart/api', () => ({
  useGetCartQuery: (_arg: unknown, options?: { skip?: boolean }) => {
    if (options?.skip) return { data: undefined, isLoading: false };
    return { data: mockCartData, isLoading: false };
  },
}));

vi.mock('@/features/wishlist/api', () => ({
  useGetWishlistQuery: (_arg: unknown, options?: { skip?: boolean }) => {
    if (options?.skip) return { data: undefined, isLoading: false };
    return { data: mockWishlistData, isLoading: false };
  },
}));

vi.mock('@/features/auth/slices/authSlice', async () => {
  const actual = await vi.importActual<typeof import('@/features/auth/slices/authSlice')>(
    '@/features/auth/slices/authSlice'
  );
  return {
    ...actual,
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

vi.mock('../HeaderUserMenu', () => ({
  HeaderUserMenu: ({
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

vi.mock('../HeaderMobileMenu', () => ({
  HeaderMobileMenu: ({ onClose, onLogout }: { onClose: () => void; onLogout: () => void }) => (
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

const setupApiHandlers = (cartData = null, wishlistData = null) => {
  server.use(
    http.get('/api/cart', () => {
      if (!cartData) {
        return HttpResponse.json({ success: false, data: null }, { status: 404 });
      }
      return HttpResponse.json({ success: true, data: cartData });
    }),
    http.post('/api/auth/logout', () => {
      return HttpResponse.json({ success: true });
    }),
    http.get('/api/wishlist', () => {
      if (!wishlistData) {
        return HttpResponse.json({ success: false, data: null }, { status: 404 });
      }
      return HttpResponse.json({ success: true, data: wishlistData });
    })
  );
};

function renderHeader() {
  return renderWithProviders(
    <MemoryRouter>
      <Header />
    </MemoryRouter>,
    { withRouter: false }
  );
}

describe('Header', () => {
  beforeEach(() => {
    server.resetHandlers();
    vi.clearAllMocks();

    mockState = {
      auth: { isAuthenticated: false, user: null },
      cart: { items: [{ quantity: 2 }, { quantity: 1 }] },
    };

    mockCartData = undefined;
    mockWishlistData = undefined;

    setupApiHandlers(null, null);
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
    mockCartData = { items: [{ quantity: 4 }, { quantity: 3 }] };
    mockWishlistData = { items: [{ id: '1' }, { id: '2' }, { id: '3' }] };

    renderHeader();

    expect(screen.getByText('Orders')).toBeInTheDocument();
    expect(screen.getAllByText('7').length).toBeGreaterThan(0);
    expect(screen.getAllByText('3').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: 'Sign In' })).not.toBeInTheDocument();
  });

  it('opens user menu and logs out from desktop menu', async () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'Ivan', email: 'ivan@example.com' },
    };
    setupApiHandlers({ items: [] }, { items: [] });

    renderHeader();

    fireEvent.click(screen.getByRole('button', { name: /user menu/i, hidden: true }));

    expect(screen.getByText('Account')).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole('button', { name: /sign out/i, hidden: true })[0]);

    await act(async () => {});

    expect(dispatchMock).toHaveBeenCalledWith(expect.objectContaining({ type: 'auth/logout' }));
  });

  it('toggles mobile menu and shows mobile auth links for guests', () => {
    renderHeader();

    fireEvent.click(screen.getByRole('button', { name: /toggle menu/i }));

    expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /sign up/i })).toBeInTheDocument();
  });

  it('logs out from mobile menu for authenticated user', async () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'Ivan', email: 'ivan@example.com' },
    };
    setupApiHandlers({ items: [] }, { items: [] });

    renderHeader();

    fireEvent.click(screen.getByRole('button', { name: /toggle menu/i }));
    fireEvent.click(screen.getAllByRole('button', { name: /sign out/i })[0]);

    await act(async () => {});

    expect(dispatchMock).toHaveBeenCalledWith(expect.objectContaining({ type: 'auth/logout' }));
  });
});
