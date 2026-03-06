import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import WishlistCard from './WishlistCard';
import { authSlice } from '@/features/auth/slices/authSlice';

// Mock RTK Query hooks
const mockRemoveFromWishlist = vi.fn();
const mockAddToCart = vi.fn();

vi.mock('../../api/wishlistApi', () => ({
  useRemoveFromWishlistMutation: () => [mockRemoveFromWishlist, { isLoading: false }],
}));

vi.mock('@/features/cart/api/cartApi', () => ({
  useAddToCartMutation: () => [mockAddToCart, { isLoading: false }],
}));

// Mock API error handler
const mockHandleError = vi.fn();
vi.mock('@/shared/hooks', () => ({
  useApiErrorHandler: () => ({
    handleError: mockHandleError,
  }),
}));

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('WishlistCard', () => {
  const mockProps = {
    productId: '123',
    productName: 'Test Product',
    image: '/test-image.jpg',
  };

  const renderComponent = (props = mockProps) => {
    const store = configureStore({
      reducer: {
        auth: authSlice.reducer,
      },
      preloadedState: {
        auth: {
          isAuthenticated: true,
          user: { id: '1', email: 'test@test.com', firstName: 'Test', lastName: 'User', role: 'customer' },
          loading: false,
          error: null,
          initialized: true,
        },
      }
    });

    return render(
      <Provider store={store}>
        <WishlistCard {...props} />
      </Provider>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mockRemoveFromWishlist.mockReturnValue({ unwrap: () => Promise.resolve() });
    mockAddToCart.mockReturnValue({ unwrap: () => Promise.resolve() });
  });

  it('renders product name correctly', () => {
    renderComponent();
    expect(screen.getByText('Test Product')).toBeInTheDocument();
  });

  it('renders product image when provided', () => {
    renderComponent();
    const image = screen.getByAltText('Test Product');
    expect(image).toBeInTheDocument();
    expect(image).toHaveAttribute('src', '/test-image.jpg');
  });

  it('renders placeholder SVG when image is not provided', () => {
    renderComponent({ ...mockProps, image: undefined });
    // Check that SVG placeholder is rendered
    const svg = document.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });

  it('renders "Add to Cart" button', () => {
    renderComponent();
    expect(screen.getByText('wishlist.addToCart')).toBeInTheDocument();
  });

  it('renders "Remove" button', () => {
    renderComponent();
    expect(screen.getByText('wishlist.remove')).toBeInTheDocument();
  });

  it('calls removeFromWishlist when remove button is clicked', async () => {
    renderComponent();
    
    const removeButton = screen.getByText('wishlist.remove');
    fireEvent.click(removeButton);

    await waitFor(() => {
      expect(mockRemoveFromWishlist).toHaveBeenCalledWith('123');
    });
  });

  it('calls addToCart when add to cart button is clicked', async () => {
    renderComponent();
    
    const addToCartButton = screen.getByText('wishlist.addToCart');
    fireEvent.click(addToCartButton);

    await waitFor(() => {
      expect(mockAddToCart).toHaveBeenCalledWith({ productId: '123', quantity: 1 });
    });
  });

  it('handles remove error gracefully', async () => {
    const error = new Error('Remove failed');
    mockRemoveFromWishlist.mockReturnValue({ 
      unwrap: () => Promise.reject(error) 
    });

    renderComponent();
    
    const removeButton = screen.getByText('wishlist.remove');
    fireEvent.click(removeButton);

    await waitFor(() => {
      expect(mockHandleError).toHaveBeenCalledWith(error, 'common.errorOccurred');
    });
  });

  it('handles add to cart error gracefully', async () => {
    const error = new Error('Add to cart failed');
    mockAddToCart.mockReturnValue({ 
      unwrap: () => Promise.reject(error) 
    });

    renderComponent();
    
    const addToCartButton = screen.getByText('wishlist.addToCart');
    fireEvent.click(addToCartButton);

    await waitFor(() => {
      expect(mockHandleError).toHaveBeenCalledWith(error, 'common.errorOccurred');
    });
  });

  it('renders with empty image string (placeholder)', () => {
    renderComponent({ ...mockProps, image: '' });
    // Empty string should render placeholder SVG
    const svg = document.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });

  it('has correct CSS classes applied', () => {
    const { container } = renderComponent();
    
    // Check for main card container
    const card = container.querySelector('[class*="card"]');
    expect(card).toBeInTheDocument();
    
    // Check for image placeholder
    const imagePlaceholder = container.querySelector('[class*="imagePlaceholder"]');
    expect(imagePlaceholder).toBeInTheDocument();
    
    // Check for content container
    const content = container.querySelector('[class*="content"]');
    expect(content).toBeInTheDocument();
    
    // Check for actions container
    const actions = container.querySelector('[class*="actions"]');
    expect(actions).toBeInTheDocument();
  });
});
