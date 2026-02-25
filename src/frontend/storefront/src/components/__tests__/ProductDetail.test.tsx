import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProductDetail from '../../pages/ProductDetail';

// Mock the custom hook
const mockUseProductDetails = vi.fn();
vi.mock('../../hooks/useProductDetails', () => ({
  default: (slug: string) => mockUseProductDetails(slug),
}));

// Mock child components to simplify testing
vi.mock('../../pages/components/ProductDetail', () => ({
  ProductImageGallery: ({ images, productName }: { images: string[], productName: string }) => (
    <div data-testid="image-gallery">Image Gallery for {productName}</div>
  ),
  ProductInfo: ({ name }: { name: string }) => <div data-testid="product-info">{name}</div>,
  ProductActions: ({ onAddToCart, onToggleWishlist }: any) => (
    <div data-testid="product-actions">
      <button onClick={onAddToCart}>Add to Cart</button>
      <button onClick={onToggleWishlist}>Toggle Wishlist</button>
    </div>
  ),
}));

vi.mock('../ReviewList', () => ({
  default: () => <div data-testid="review-list">Reviews</div>,
}));

vi.mock('../ReviewForm', () => ({
  default: () => <div data-testid="review-form">Review Form</div>,
}));

vi.mock('../ErrorAlert', () => ({
  default: ({ message }: { message: string }) => <div role="alert">{message}</div>,
}));

vi.mock('../EmptyState', () => ({
  default: ({ title }: { title: string }) => <div>{title}</div>,
}));

vi.mock('../Skeletons', () => ({
  ProductSkeleton: () => <div data-testid="product-skeleton">Loading...</div>,
}));

vi.mock('../ui/Card', () => ({
  default: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

describe('ProductDetail Page', () => {
  const defaultMockReturn = {
    product: {
      id: '1',
      name: 'Test Product',
      description: 'Desc',
      price: 100,
      images: [],
      stockQuantity: 10,
    },
    productLoading: false,
    productError: null,
    reviews: [],
    reviewsLoading: false,
    reviewsError: null,
    refetchReviews: vi.fn(),
    isInWishlist: false,
    addingToWishlist: false,
    removingFromWishlist: false,
    toggleWishlist: vi.fn(),
    quantity: 1,
    setQuantity: vi.fn(),
    addedToCart: false,
    cartError: null,
    setCartError: vi.fn(),
    cartItem: null,
    addingToCartBackend: false,
    addToCart: vi.fn(),
    isAuthenticated: false,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mockUseProductDetails.mockReturnValue(defaultMockReturn);
  });

  const renderPage = (slug = 'test-product') => {
    return render(
      <MemoryRouter initialEntries={[`/products/${slug}`]}>
        <Routes>
          <Route path="/products/:slug" element={<ProductDetail />} />
        </Routes>
      </MemoryRouter>
    );
  };

  it('renders loading skeleton when loading', () => {
    mockUseProductDetails.mockReturnValue({
      ...defaultMockReturn,
      productLoading: true,
      product: null,
    });

    renderPage();
    expect(screen.getByTestId('product-skeleton')).toBeInTheDocument();
  });

  it('renders error alert when there is an error', () => {
    mockUseProductDetails.mockReturnValue({
      ...defaultMockReturn,
      product: null,
      productError: { message: 'Error' },
    });

    renderPage();
    expect(screen.getByText(/Failed to load product/i)).toBeInTheDocument();
  });

  it('renders empty state when product is not found', () => {
    mockUseProductDetails.mockReturnValue({
      ...defaultMockReturn,
      product: null,
    });

    renderPage();
    expect(screen.getByText(/Product not found/i)).toBeInTheDocument();
  });

  it('renders product details when data is available', () => {
    renderPage();
    expect(screen.getByTestId('image-gallery')).toBeInTheDocument();
    expect(screen.getByTestId('product-info')).toHaveTextContent('Test Product');
    expect(screen.getByTestId('product-actions')).toBeInTheDocument();
    expect(screen.getByTestId('review-list')).toBeInTheDocument();
  });

  it('renders review form only when authenticated', () => {
    // Not authenticated
    renderPage();
    expect(screen.queryByTestId('review-form')).not.toBeInTheDocument();

    // Authenticated
    mockUseProductDetails.mockReturnValue({
      ...defaultMockReturn,
      isAuthenticated: true,
    });
    
    // Re-render
    renderPage();
    expect(screen.getByTestId('review-form')).toBeInTheDocument();
  });
});
