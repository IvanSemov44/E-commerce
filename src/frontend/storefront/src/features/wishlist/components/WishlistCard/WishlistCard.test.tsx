import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { WishlistCard } from './WishlistCard';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

const mockHandleError = vi.fn();
vi.mock('@/shared/hooks', () => ({
  useApiErrorHandler: vi.fn(() => ({ handleError: mockHandleError })),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

const defaultProps = {
  productId: 'p1',
  productName: 'Test Product',
  price: 29.99,
  image: '/test.jpg',
};

const setupHandlers = () => {
  server.use(
    http.delete('/api/wishlist/remove/123', () => {
      return HttpResponse.json({
        success: true,
        data: { id: 'w1', items: [], itemCount: 0 },
      });
    }),
    http.post('/api/wishlist/add', async ({ request }) => {
      const body = await request.json();
      return HttpResponse.json({
        success: true,
        data: { id: 'w1', items: [{ id: 'item-1', productId: body.productId }], itemCount: 1 },
      });
    })
  );
};

describe('WishlistCard', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mockHandleError.mockReset();
    setupHandlers();
  });

  it('renders product name', () => {
    renderWithProviders(<WishlistCard {...defaultProps} />);
    expect(screen.getByText('Test Product')).toBeInTheDocument();
  });

  it('renders product image when provided', () => {
    renderWithProviders(<WishlistCard {...defaultProps} />);
    expect(screen.getByAltText('Test Product')).toHaveAttribute('src', '/test.jpg');
  });

  it('renders placeholder icon when image is empty string', () => {
    renderWithProviders(<WishlistCard {...defaultProps} image="" />);
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(document.querySelector('svg')).toBeInTheDocument();
  });

  it('renders placeholder icon when image is undefined', () => {
    renderWithProviders(<WishlistCard {...defaultProps} image={undefined} />);
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(document.querySelector('svg')).toBeInTheDocument();
  });

  it('renders formatted price', () => {
    renderWithProviders(<WishlistCard {...defaultProps} price={29.99} />);
    expect(screen.getByText('$29.99')).toBeInTheDocument();
  });

  it('renders Add to Cart and Remove buttons', () => {
    renderWithProviders(<WishlistCard {...defaultProps} />);
    expect(screen.getByText('wishlist.addToCart')).toBeInTheDocument();
    expect(screen.getByText('wishlist.remove')).toBeInTheDocument();
  });

  it('calls removeFromWishlist when Remove is clicked', async () => {
    renderWithProviders(<WishlistCard {...defaultProps} />);
    fireEvent.click(screen.getByText('wishlist.remove'));
    await waitFor(() => expect(mockHandleError).not.toHaveBeenCalled());
  });

  it('calls addToCart when Add to Cart is clicked', async () => {
    renderWithProviders(<WishlistCard {...defaultProps} />);
    fireEvent.click(screen.getByText('wishlist.addToCart'));
    await waitFor(() => expect(mockHandleError).not.toHaveBeenCalled());
  });

  it('disables Add to Cart and shows out of stock when isAvailable is false', () => {
    renderWithProviders(<WishlistCard {...defaultProps} isAvailable={false} />);
    expect(screen.getByText('wishlist.addToCart').closest('button')).toBeDisabled();
    expect(screen.getByText('wishlist.outOfStock')).toBeInTheDocument();
  });

  it('shows compareAtPrice when provided', () => {
    renderWithProviders(<WishlistCard {...defaultProps} compareAtPrice={49.99} />);
    expect(screen.getByText('$49.99')).toBeInTheDocument();
  });

  it('does not show compareAtPrice when not provided', () => {
    renderWithProviders(<WishlistCard {...defaultProps} />);
    expect(screen.queryByText(/\$49/)).not.toBeInTheDocument();
  });

  it('calls handleError when remove fails', async () => {
    server.use(
      http.delete('/api/wishlist/remove/123', () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Remove failed', code: 'INTERNAL_ERROR' } },
          { status: 500 }
        );
      })
    );

    renderWithProviders(<WishlistCard {...defaultProps} />);
    fireEvent.click(screen.getByText('wishlist.remove'));

    await waitFor(() => expect(mockHandleError).toHaveBeenCalled());
  });

  it('calls handleError when addToCart fails', async () => {
    server.use(
      http.post('/api/cart/add-item', () => {
        return HttpResponse.json(
          {
            success: false,
            errorDetails: { message: 'Add to cart failed', code: 'INTERNAL_ERROR' },
          },
          { status: 500 }
        );
      })
    );

    renderWithProviders(<WishlistCard {...defaultProps} />);
    fireEvent.click(screen.getByText('wishlist.addToCart'));

    await waitFor(() => expect(mockHandleError).toHaveBeenCalled());
  });
});
