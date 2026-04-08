import { screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import { PaymentMethodSelector } from './PaymentMethodSelector';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: { defaultValue?: string }) => opts?.defaultValue ?? key,
  }),
}));

const mockMethods = ['credit_card', 'debit_card', 'paypal', 'apple_pay'];

const setupPaymentMethodsHandlers = (methods = mockMethods) => {
  server.use(
    http.get('/api/payments/methods', () => {
      return HttpResponse.json({
        success: true,
        data: { methods },
      });
    })
  );
};

describe('PaymentMethodSelector', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    server.resetHandlers();
    setupPaymentMethodsHandlers();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  it('renders available payment methods', () => {
    renderWithProviders(
      <PaymentMethodSelector selectedMethod="credit_card" onMethodChange={vi.fn()} />
    );

    expect(screen.getByText(/credit card/i)).toBeInTheDocument();
    expect(screen.getByText(/debit card/i)).toBeInTheDocument();
    expect(screen.getByText(/paypal/i)).toBeInTheDocument();
    expect(screen.getByText(/apple pay/i)).toBeInTheDocument();
  });

  it('marks the selected method as checked', () => {
    renderWithProviders(<PaymentMethodSelector selectedMethod="paypal" onMethodChange={vi.fn()} />);

    expect(screen.getByRole('radio', { name: /paypal/i })).toBeChecked();
    expect(screen.getByRole('radio', { name: /credit card/i })).not.toBeChecked();
  });

  it('calls onMethodChange when a method is selected', () => {
    const onMethodChange = vi.fn();
    renderWithProviders(
      <PaymentMethodSelector selectedMethod="credit_card" onMethodChange={onMethodChange} />
    );

    fireEvent.click(screen.getByRole('radio', { name: /debit card/i }));

    expect(onMethodChange).toHaveBeenCalledWith('debit_card');
  });

  it('renders a fieldset grouping the radio buttons', () => {
    renderWithProviders(
      <PaymentMethodSelector selectedMethod="credit_card" onMethodChange={vi.fn()} />
    );

    expect(screen.getByRole('group')).toBeInTheDocument();
  });

  it('shows loading skeleton while fetching', async () => {
    server.use(
      http.get('/api/checkout/payment-methods', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100));
        return HttpResponse.json({ success: true, data: { methods: mockMethods } });
      })
    );
    renderWithProviders(<PaymentMethodSelector selectedMethod="" onMethodChange={vi.fn()} />);

    expect(document.querySelector('[aria-busy="true"]')).toBeInTheDocument();
  });

  it('renders nothing when methods list is empty', () => {
    setupPaymentMethodsHandlers([]);
    const { container } = renderWithProviders(
      <PaymentMethodSelector selectedMethod="" onMethodChange={vi.fn()} />
    );

    expect(container.firstChild).toBeNull();
  });

  it('renders fallback icon for unknown payment method', () => {
    setupPaymentMethodsHandlers(['unknown_method']);
    renderWithProviders(
      <PaymentMethodSelector selectedMethod="unknown_method" onMethodChange={vi.fn()} />
    );

    expect(screen.getByRole('radio', { name: /unknown method/i })).toBeInTheDocument();
  });
});
