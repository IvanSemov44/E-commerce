import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import Header from './Header';

const dispatchMock = vi.fn();
const navigateMock = vi.fn();
const getCartQueryMock = vi.fn();
const getWishlistQueryMock = vi.fn();

let mockState: {
  auth: {
    isAuthenticated: boolean;
    user: { firstName: string; email: string } | null;
  };
  cart: {
    items: Array<{ quantity: number }>;
  };
};

vi.mock('@/shared/lib/store', () => ({
  useAppSelector: (selector: (state: typeof mockState) => unknown) => selector(mockState),
  useAppDispatch: () => dispatchMock,
}));

vi.mock('@/features/cart/api/cartApi', () => ({
  useGetCartQuery: (...args: unknown[]) => getCartQueryMock(...args),
}));

vi.mock('@/features/wishlist/api/wishlistApi', () => ({
  useGetWishlistQuery: (...args: unknown[]) => getWishlistQueryMock(...args),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock('../icons', () => {
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

vi.mock('../ThemeToggle', () => ({
  ThemeToggle: ({ size }: { size: string }) => <div data-testid="theme-toggle">{size}</div>,
}));

vi.mock('../LanguageSwitcher', () => ({
  LanguageSwitcher: ({ size }: { size: string }) => <div data-testid="language-switcher">{size}</div>,
}));

vi.mock('../SearchBar', () => ({
  SearchBar: ({ size }: { size: string }) => <div data-testid="search-bar">{size}</div>,
}));

vi.mock('../ui/Button', () => ({
  default: ({ children }: { children: React.ReactNode }) => <button>{children}</button>,
}));

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
      auth: {
        isAuthenticated: false,
        user: null,
      },
      cart: {
        items: [{ quantity: 2 }, { quantity: 1 }],
      },
    };

    getCartQueryMock.mockReturnValue({ data: undefined });
    getWishlistQueryMock.mockReturnValue({ data: undefined });
  });

  it('renders public navigation for unauthenticated user', () => {
    renderHeader();

    expect(screen.getByText('E-Shop')).toBeInTheDocument();
    expect(screen.getByText('nav.products')).toBeInTheDocument();
    expect(screen.queryByText('nav.orders')).not.toBeInTheDocument();
    expect(screen.getByText('nav.signIn')).toBeInTheDocument();
    expect(screen.getByText('Sign Up')).toBeInTheDocument();
  });

  it('shows local cart count when unauthenticated', () => {
    renderHeader();
    expect(screen.getAllByText('3').length).toBeGreaterThan(0);
  });

  it('renders authenticated links and backend counts', () => {
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

    expect(screen.getByText('nav.orders')).toBeInTheDocument();
    expect(screen.getAllByText('7').length).toBeGreaterThan(0);
    expect(screen.getAllByText('3').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: 'nav.signIn' })).not.toBeInTheDocument();
  });

  it('opens user menu and logs out from desktop menu', () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'Ivan', email: 'ivan@example.com' },
    };

    renderHeader();

    const userMenuButton = document.querySelector('button[aria-label="User menu"]') as HTMLButtonElement;
    fireEvent.click(userMenuButton);

    expect(screen.getByText('Account')).toBeInTheDocument();
    expect(screen.getByText('ivan@example.com')).toBeInTheDocument();

    fireEvent.click(screen.getAllByText('Logout')[0]);

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
    fireEvent.click(screen.getAllByRole('button', { name: /logout/i })[0]);

    expect(dispatchMock).toHaveBeenCalledWith(expect.objectContaining({ type: 'auth/logout' }));
    expect(navigateMock).toHaveBeenCalledWith('/');
  });

  it('requests cart and wishlist with authenticated skip flags', () => {
    mockState.auth = {
      isAuthenticated: true,
      user: { firstName: 'Ivan', email: 'ivan@example.com' },
    };

    renderHeader();

    expect(getCartQueryMock).toHaveBeenCalledWith(undefined, expect.objectContaining({ skip: false }));
    expect(getWishlistQueryMock).toHaveBeenCalledWith(undefined, expect.objectContaining({ skip: false }));
  });
});

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});
