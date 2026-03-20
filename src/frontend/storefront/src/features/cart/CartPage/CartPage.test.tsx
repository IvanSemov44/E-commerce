import { screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { CartPage } from './CartPage';
import * as useCartModule from '@/features/cart/hooks/useCart';

vi.mock('@/features/cart/hooks/useCart', () => ({
  useCart: vi.fn(),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('@/shared/hooks', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/shared/hooks')>();
  return { ...actual, usePerformanceMonitor: vi.fn() };
});

const defaultTotals = { subtotal: 0, shipping: 0, tax: 0, total: 0 };

const mockItem = {
  id: '1',
  name: 'Test Product',
  slug: 'test-product',
  price: 29.99,
  quantity: 2,
  maxStock: 10,
  image: '/test.jpg',
};

const defaultPreloadedState = {
  auth: { isAuthenticated: false, user: null, loading: false, error: null, initialized: true },
  cart: { items: [], lastUpdated: 0 },
};

const render = () => renderWithProviders(<CartPage />, { preloadedState: defaultPreloadedState });

describe('CartPage', () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    const hooks = await import('@/features/cart/hooks/useCart');
    vi.mocked(hooks.useCart).mockReturnValue({
      displayItems: [],
      totals: defaultTotals,
      isLoading: false,
      isAuthenticated: false,
    });
  });

  it('shows skeleton while loading', () => {
    vi.mocked(useCartModule.useCart).mockReturnValue({
      displayItems: [],
      totals: defaultTotals,
      isLoading: true,
      isAuthenticated: false,
    });

    render();

    const skeletonItems = document.querySelectorAll('[aria-busy="true"]');
    expect(skeletonItems.length).toBeGreaterThan(0);
  });

  it('shows empty state when cart has no items', () => {
    vi.mocked(useCartModule.useCart).mockReturnValue({
      displayItems: [],
      totals: defaultTotals,
      isLoading: false,
      isAuthenticated: false,
    });

    render();

    expect(screen.getByText('cart.emptyCart')).toBeInTheDocument();
  });

  it('renders cart items when loaded', () => {
    vi.mocked(useCartModule.useCart).mockReturnValue({
      displayItems: [mockItem],
      totals: { subtotal: 59.98, shipping: 0, tax: 5.4, total: 65.38 },
      isLoading: false,
      isAuthenticated: false,
    });

    render();

    expect(screen.getByText('Test Product')).toBeInTheDocument();
  });

  it('renders cart summary when items are present', () => {
    vi.mocked(useCartModule.useCart).mockReturnValue({
      displayItems: [mockItem],
      totals: { subtotal: 59.98, shipping: 10, tax: 5.4, total: 75.38 },
      isLoading: false,
      isAuthenticated: false,
    });

    render();

    expect(screen.getByRole('link', { name: 'cart.proceedToCheckout' })).toBeInTheDocument();
  });

  it('does not render cart summary when cart is empty', () => {
    vi.mocked(useCartModule.useCart).mockReturnValue({
      displayItems: [],
      totals: defaultTotals,
      isLoading: false,
      isAuthenticated: false,
    });

    render();

    expect(screen.queryByRole('link', { name: 'cart.proceedToCheckout' })).not.toBeInTheDocument();
  });

  it('shows continue shopping link in empty state', () => {
    vi.mocked(useCartModule.useCart).mockReturnValue({
      displayItems: [],
      totals: defaultTotals,
      isLoading: false,
      isAuthenticated: false,
    });

    render();

    expect(screen.getByRole('link', { name: 'cart.continueShopping' })).toBeInTheDocument();
  });

  it('renders multiple items', () => {
    const items = [
      { ...mockItem, id: '1', name: 'Product 1' },
      { ...mockItem, id: '2', name: 'Product 2' },
    ];

    vi.mocked(useCartModule.useCart).mockReturnValue({
      displayItems: items,
      totals: { subtotal: 119.96, shipping: 10, tax: 10.8, total: 140.76 },
      isLoading: false,
      isAuthenticated: false,
    });

    render();

    expect(screen.getByText('Product 1')).toBeInTheDocument();
    expect(screen.getByText('Product 2')).toBeInTheDocument();
  });

  it('renders authenticated user state', () => {
    vi.mocked(useCartModule.useCart).mockReturnValue({
      displayItems: [mockItem],
      totals: { subtotal: 59.98, shipping: 10, tax: 5.4, total: 75.38 },
      isLoading: false,
      isAuthenticated: true,
    });

    render();

    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'cart.proceedToCheckout' })).toBeInTheDocument();
  });
});
