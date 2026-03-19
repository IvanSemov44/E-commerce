import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import * as wishlistApi from '@/features/wishlist/api';
import { WishlistPage } from './WishlistPage';

vi.mock('@/features/wishlist/api', () => ({
  useGetWishlistQuery: vi.fn(),
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
    if (!data)
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
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, Link: ({ children }: { children: React.ReactNode }) => <>{children}</> };
});

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

describe('WishlistPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('shows skeleton while loading', () => {
    vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
      data: undefined,
      isLoading: true,
      error: undefined,
    } as never);

    renderWithProviders(<WishlistPage />);
    expect(screen.getByTestId('wishlist-skeleton')).toBeInTheDocument();
  });

  it('shows empty state when wishlist has no data', () => {
    vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
      data: undefined,
      isLoading: false,
      error: undefined,
    } as never);

    renderWithProviders(<WishlistPage />);
    expect(screen.getByText('wishlist.empty')).toBeInTheDocument();
  });

  it('renders a WishlistCard for each item', () => {
    vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
      data: mockWishlist,
      isLoading: false,
      error: undefined,
    } as never);

    renderWithProviders(<WishlistPage />);
    expect(screen.getAllByTestId('wishlist-card')).toHaveLength(2);
    expect(screen.getByText('Widget')).toBeInTheDocument();
    expect(screen.getByText('Gadget')).toBeInTheDocument();
  });

  it('shows error state when query fails', () => {
    vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
      data: undefined,
      isLoading: false,
      error: { status: 500 },
    } as never);

    renderWithProviders(<WishlistPage />);
    expect(screen.getByTestId('query-error')).toBeInTheDocument();
  });

  it('shows empty state when wishlist has zero items', () => {
    vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
      data: { id: 'w1', itemCount: 0, items: [] },
      isLoading: false,
      error: undefined,
    } as never);

    renderWithProviders(<WishlistPage />);
    expect(screen.queryAllByTestId('wishlist-card')).toHaveLength(0);
  });

  it('shows continue shopping button in empty state', () => {
    vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
      data: undefined,
      isLoading: false,
      error: undefined,
    } as never);

    renderWithProviders(<WishlistPage />);
    expect(screen.getByText('wishlist.continueShopping')).toBeInTheDocument();
  });

  it('renders page title', () => {
    vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
      data: mockWishlist,
      isLoading: false,
      error: undefined,
    } as never);

    renderWithProviders(<WishlistPage />);
    expect(screen.getByText('wishlist.title')).toBeInTheDocument();
  });
});
