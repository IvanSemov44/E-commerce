import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import * as wishlistApi from '@/features/wishlist/api';
import * as cartApi from '@/features/cart/api';
import { WishlistCard } from './WishlistCard';

vi.mock('@/features/wishlist/api', () => ({
  useRemoveFromWishlistMutation: vi.fn(() => [vi.fn()]),
}));

vi.mock('@/features/cart/api', () => ({
  useAddToCartMutation: vi.fn(() => [vi.fn()]),
}));

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

describe('WishlistCard', () => {
  let mockRemove: ReturnType<typeof vi.fn>;
  let mockAddToCart: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.resetAllMocks();
    mockHandleError.mockReset();
    mockRemove = vi.fn().mockReturnValue({ unwrap: () => Promise.resolve() });
    mockAddToCart = vi.fn().mockReturnValue({ unwrap: () => Promise.resolve() });
    vi.mocked(wishlistApi.useRemoveFromWishlistMutation).mockReturnValue([mockRemove] as never);
    vi.mocked(cartApi.useAddToCartMutation).mockReturnValue([mockAddToCart] as never);
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
    await waitFor(() => expect(mockRemove).toHaveBeenCalledWith('p1'));
  });

  it('calls addToCart when Add to Cart is clicked', async () => {
    renderWithProviders(<WishlistCard {...defaultProps} />);
    fireEvent.click(screen.getByText('wishlist.addToCart'));
    await waitFor(() =>
      expect(mockAddToCart).toHaveBeenCalledWith({ productId: 'p1', quantity: 1 })
    );
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
    const error = new Error('Remove failed');
    mockRemove.mockReturnValue({ unwrap: () => Promise.reject(error) });

    renderWithProviders(<WishlistCard {...defaultProps} />);
    fireEvent.click(screen.getByText('wishlist.remove'));

    await waitFor(() =>
      expect(mockHandleError).toHaveBeenCalledWith(error, 'common.errorOccurred')
    );
  });

  it('calls handleError when addToCart fails', async () => {
    const error = new Error('Add to cart failed');
    mockAddToCart.mockReturnValue({ unwrap: () => Promise.reject(error) });

    renderWithProviders(<WishlistCard {...defaultProps} />);
    fireEvent.click(screen.getByText('wishlist.addToCart'));

    await waitFor(() =>
      expect(mockHandleError).toHaveBeenCalledWith(error, 'common.errorOccurred')
    );
  });
});
