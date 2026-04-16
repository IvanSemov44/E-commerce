import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { WishlistPage } from './WishlistPage';
import { server } from '@/shared/lib/test/msw-server';

const mockWishlist = {
  id: 'w1',
  itemCount: 2,
  items: [
    {
      id: 'i1',
      productId: 'p1',
      productName: 'Widget',
      price: 20,
      isAvailable: true,
      productImage: '',
    },
    {
      id: 'i2',
      productId: 'p2',
      productName: 'Gadget',
      price: 35,
      isAvailable: false,
      productImage: '/g.jpg',
    },
  ],
};

let mockWishlistState = {
  data: undefined as
    | undefined
    | {
        id: string;
        items: Array<{
          id: string;
          productId: string;
          productName: string;
          price: number;
          isAvailable: boolean;
          productImage: string;
        }>;
      },
  isLoading: false,
  error: null as null | unknown,
};

vi.mock('@/features/wishlist/api', () => ({
  useGetWishlistQuery: () => mockWishlistState,
}));

vi.mock('@/features/wishlist/components', () => ({
  WishlistCard: ({ productName }: { productName: string }) => (
    <div data-testid="wishlist-card">{productName}</div>
  ),
  WishlistSkeleton: () => <div data-testid="wishlist-skeleton" />,
}));

vi.mock('@/shared/components', () => ({
  QueryRenderer: ({
    isLoading,
    error,
    data,
    children,
    loadingSkeleton,
    emptyState,
  }: {
    isLoading: boolean;
    error: unknown;
    data: unknown;
    children: (data: unknown) => React.ReactNode;
    loadingSkeleton: { custom: React.ReactNode };
    emptyState: { icon: React.ReactNode; title: string; action: React.ReactNode };
  }) => {
    if (isLoading) return <>{loadingSkeleton.custom}</>;
    if (error) return <div data-testid="query-error">error</div>;
    const dataObj = data as { items?: Array<unknown> } | undefined;
    if (!data || (dataObj.items && dataObj.items.length === 0))
      return (
        <div>
          {emptyState.title}
          {emptyState.action}
        </div>
      );
    return <>{children(data)}</>;
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      if (key === 'wishlist.title') return 'My Wishlist';
      if (key === 'wishlist.continueShopping') return 'Continue Shopping';
      if (key === 'wishlist.empty') return 'wishlist.empty';
      return key;
    },
  }),
}));

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return {
    ...actual,
    Link: ({ children, to }: { children: React.ReactNode; to: string }) => (
      <a href={typeof to === 'string' ? to : '#'}>{children}</a>
    ),
  };
});

describe('WishlistPage', () => {
  beforeEach(() => {
    mockWishlistState = {
      data: undefined,
      isLoading: false,
      error: null,
    };
    vi.resetAllMocks();
    server.resetHandlers();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  it('renders a WishlistCard for each item', () => {
    mockWishlistState = {
      data: mockWishlist,
      isLoading: false,
      error: null,
    };
    renderWithProviders(<WishlistPage />);
    expect(screen.getAllByTestId('wishlist-card')).toHaveLength(2);
    expect(screen.getByText('Widget')).toBeInTheDocument();
    expect(screen.getByText('Gadget')).toBeInTheDocument();
  });

  it('shows skeleton while loading', async () => {
    mockWishlistState = {
      data: undefined,
      isLoading: true,
      error: null,
    };
    renderWithProviders(<WishlistPage />);
    expect(screen.getByTestId('wishlist-skeleton')).toBeInTheDocument();
  });

  it('shows empty state when wishlist has no data', () => {
    mockWishlistState = {
      data: undefined,
      isLoading: false,
      error: null,
    };
    renderWithProviders(<WishlistPage />);
    expect(screen.getByText('wishlist.empty')).toBeInTheDocument();
  });

  it('shows error state when query fails', () => {
    mockWishlistState = {
      data: undefined,
      isLoading: false,
      error: { message: 'Server error' },
    };
    renderWithProviders(<WishlistPage />);
    expect(screen.getByTestId('query-error')).toBeInTheDocument();
  });

  it('shows empty state when wishlist has zero items', () => {
    mockWishlistState = {
      data: { id: 'w1', items: [], itemCount: 0 },
      isLoading: false,
      error: null,
    };
    renderWithProviders(<WishlistPage />);
    expect(screen.getByText('wishlist.empty')).toBeInTheDocument();
  });

  it('shows continue shopping button in empty state', () => {
    mockWishlistState = {
      data: undefined,
      isLoading: false,
      error: null,
    };
    renderWithProviders(<WishlistPage />);
    expect(screen.getByRole('link', { name: /continue shopping/i })).toBeInTheDocument();
  });

  it('renders page title', () => {
    mockWishlistState = {
      data: undefined,
      isLoading: false,
      error: null,
    };
    renderWithProviders(<WishlistPage />);
    expect(screen.getByRole('heading', { name: /my wishlist/i })).toBeInTheDocument();
  });
});
