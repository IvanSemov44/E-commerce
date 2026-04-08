import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { WishlistPage } from './WishlistPage';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

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

const setupWishlistHandlers = (wishlist = mockWishlist) => {
  server.use(
    http.get('/api/wishlist', () => {
      return HttpResponse.json({
        success: true,
        data: wishlist,
      });
    })
  );
};

describe('WishlistPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    setupWishlistHandlers();
  });

  it('shows skeleton while loading', async () => {
    server.use(
      http.get('/api/wishlist', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100));
        return HttpResponse.json({ success: true, data: mockWishlist });
      })
    );
    renderWithProviders(<WishlistPage />);
    expect(screen.getByTestId('wishlist-skeleton')).toBeInTheDocument();
  });

  it('shows empty state when wishlist has no data', () => {
    server.use(
      http.get('/api/wishlist', () => {
        return HttpResponse.json({ success: false, data: null }, { status: 404 });
      })
    );
    renderWithProviders(<WishlistPage />);
    expect(screen.getByText('wishlist.empty')).toBeInTheDocument();
  });

  it('renders a WishlistCard for each item', () => {
    renderWithProviders(<WishlistPage />);
    expect(screen.getAllByTestId('wishlist-card')).toHaveLength(2);
    expect(screen.getByText('Widget')).toBeInTheDocument();
    expect(screen.getByText('Gadget')).toBeInTheDocument();
  });

  it('shows error state when query fails', () => {
    server.use(
      http.get('/api/wishlist', () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Server error', code: 'INTERNAL_ERROR' } },
          { status: 500 }
        );
      })
    );
    renderWithProviders(<WishlistPage />);
    expect(screen.getByTestId('query-error')).toBeInTheDocument();
  });

  it('shows empty state when wishlist has zero items', () => {
    server.use(
      http.get('/api/wishlist', () => {
        return HttpResponse.json({
          success: true,
          data: { id: 'w1', itemCount: 0, items: [] },
        });
      })
    );
    renderWithProviders(<WishlistPage />);
    expect(screen.queryAllByTestId('wishlist-card')).toHaveLength(0);
  });

  it('shows continue shopping button in empty state', () => {
    server.use(
      http.get('/api/wishlist', () => {
        return HttpResponse.json({ success: false, data: null }, { status: 404 });
      })
    );
    renderWithProviders(<WishlistPage />);
    expect(screen.getByText('wishlist.continueShopping')).toBeInTheDocument();
  });

  it('renders page title', () => {
    renderWithProviders(<WishlistPage />);
    expect(screen.getByText('wishlist.title')).toBeInTheDocument();
  });
});
