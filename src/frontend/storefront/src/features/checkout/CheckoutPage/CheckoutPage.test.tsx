import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { CheckoutPage } from './CheckoutPage';

// ── hook mocks ────────────────────────────────────────────────────────────────
vi.mock('@/features/cart/hooks', () => ({
  useCart: vi.fn(() => ({
    displayItems: [],
    totals: { subtotal: 0, shipping: 0, tax: 0, total: 0 },
    isLoading: false,
    isAuthenticated: false,
  })),
  useCartSync: vi.fn(() => ({ isLoading: false })),
}));

vi.mock('@/features/checkout/hooks/useCheckoutPromo', () => ({
  useCheckoutPromo: vi.fn(() => ({
    promoCode: '',
    promoCodeValidation: null,
    promoState: {
      code: '',
      validation: null,
      isValidating: false,
      onChange: vi.fn(),
      onApply: vi.fn(),
      onRemove: vi.fn(),
    },
  })),
}));

vi.mock('@/features/checkout/hooks/useCheckoutOrder', () => ({
  useCheckoutOrder: vi.fn(() => ({
    orderComplete: false,
    orderNumber: '',
    orderEmail: '',
    error: null,
    isGuestOrder: false,
    handleFormSubmit: vi.fn(),
  })),
}));

vi.mock('@/features/checkout/api', () => ({
  useGetPaymentMethodsQuery: vi.fn(() => ({ data: undefined })),
}));

// ── shared mocks ──────────────────────────────────────────────────────────────
vi.mock('@/shared/hooks', () => ({
  usePerformanceMonitor: vi.fn(),
}));

vi.mock('@/shared/lib/utils/telemetry', () => ({
  telemetry: { track: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, useNavigate: () => vi.fn() };
});

// ── component mocks ───────────────────────────────────────────────────────────
vi.mock('@/features/checkout/components/CheckoutForm', () => ({
  CheckoutForm: () => <div data-testid="checkout-form" />,
}));

vi.mock('@/features/checkout/components/OrderSummary', () => ({
  OrderSummary: () => <div data-testid="order-summary" />,
}));

vi.mock('@/features/checkout/components/OrderSuccess', () => ({
  OrderSuccess: ({ orderNumber }: { orderNumber: string }) => (
    <div data-testid="order-success">{orderNumber}</div>
  ),
}));

vi.mock('@/shared/components/TrustSignals', () => ({
  TrustSignals: () => <div data-testid="trust-signals" />,
}));

// ── imports after mocks ───────────────────────────────────────────────────────
import * as cartHooks from '@/features/cart/hooks';
import * as orderHook from '@/features/checkout/hooks/useCheckoutOrder';
import * as telemetry from '@/shared/lib/utils/telemetry';

const mockCartItems = [
  {
    id: 'p1',
    name: 'Widget',
    slug: 'widget',
    price: 20,
    quantity: 1,
    maxStock: 5,
    image: '',
  },
];

describe('CheckoutPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(cartHooks.useCart).mockReturnValue({
      displayItems: [],
      totals: { subtotal: 0, shipping: 0, tax: 0, total: 0 },
      isLoading: false,
      isAuthenticated: false,
    });
    vi.mocked(cartHooks.useCartSync).mockReturnValue({ isLoading: false });
    vi.mocked(orderHook.useCheckoutOrder).mockReturnValue({
      orderComplete: false,
      orderNumber: '',
      orderEmail: '',
      error: null,
      isGuestOrder: false,
      handleFormSubmit: vi.fn(),
    });
  });

  it('shows loading indicator while cart is loading', () => {
    vi.mocked(cartHooks.useCart).mockReturnValue({
      displayItems: [],
      totals: { subtotal: 0, shipping: 0, tax: 0, total: 0 },
      isLoading: true,
      isAuthenticated: false,
    });

    renderWithProviders(<CheckoutPage />);

    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('shows empty state when cart is empty', () => {
    renderWithProviders(<CheckoutPage />);

    expect(screen.getByText('cart.emptyCart')).toBeInTheDocument();
  });

  it('renders checkout form and order summary when cart has items', () => {
    vi.mocked(cartHooks.useCart).mockReturnValue({
      displayItems: mockCartItems,
      totals: { subtotal: 20, shipping: 0, tax: 0, total: 20 },
      isLoading: false,
      isAuthenticated: false,
    });

    renderWithProviders(<CheckoutPage />);

    expect(screen.getByTestId('checkout-form')).toBeInTheDocument();
    expect(screen.getByTestId('order-summary')).toBeInTheDocument();
  });

  it('renders OrderSuccess when order is complete', () => {
    vi.mocked(cartHooks.useCart).mockReturnValue({
      displayItems: mockCartItems,
      totals: { subtotal: 20, shipping: 0, tax: 0, total: 20 },
      isLoading: false,
      isAuthenticated: false,
    });

    vi.mocked(orderHook.useCheckoutOrder).mockReturnValue({
      orderComplete: true,
      orderNumber: 'ORD-999',
      orderEmail: 'x@x.com',
      error: null,
      isGuestOrder: false,
      handleFormSubmit: vi.fn(),
    });

    renderWithProviders(<CheckoutPage />);

    expect(screen.getByTestId('order-success')).toBeInTheDocument();
    expect(screen.getByText('ORD-999')).toBeInTheDocument();
  });

  it('shows OrderSuccess even when cart is empty if order is complete', () => {
    vi.mocked(orderHook.useCheckoutOrder).mockReturnValue({
      orderComplete: true,
      orderNumber: 'ORD-123',
      orderEmail: 'x@x.com',
      error: null,
      isGuestOrder: false,
      handleFormSubmit: vi.fn(),
    });

    renderWithProviders(<CheckoutPage />);

    expect(screen.getByTestId('order-success')).toBeInTheDocument();
  });

  it('shows error alert when order has an error', () => {
    vi.mocked(cartHooks.useCart).mockReturnValue({
      displayItems: mockCartItems,
      totals: { subtotal: 20, shipping: 0, tax: 0, total: 20 },
      isLoading: false,
      isAuthenticated: false,
    });

    vi.mocked(orderHook.useCheckoutOrder).mockReturnValue({
      orderComplete: false,
      orderNumber: '',
      orderEmail: '',
      error: 'Payment declined',
      isGuestOrder: false,
      handleFormSubmit: vi.fn(),
    });

    renderWithProviders(<CheckoutPage />);

    expect(screen.getByText('Payment declined')).toBeInTheDocument();
  });

  it('tracks checkout.view telemetry on mount', () => {
    renderWithProviders(<CheckoutPage />);

    expect(telemetry.telemetry.track).toHaveBeenCalledWith(
      'checkout.view',
      expect.objectContaining({ isAuthenticated: expect.any(Boolean) })
    );
  });
});
