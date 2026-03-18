import { screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import * as paymentsApi from '@/features/checkout/api/paymentsApi';
import { PaymentMethodSelector } from './PaymentMethodSelector';

vi.mock('@/features/checkout/api/paymentsApi', () => ({
  useGetPaymentMethodsQuery: vi.fn(() => ({
    data: { methods: ['credit_card', 'debit_card', 'paypal', 'apple_pay'] },
    isLoading: false,
  })),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: { defaultValue?: string }) => opts?.defaultValue ?? key,
  }),
}));

describe('PaymentMethodSelector', () => {
  beforeEach(() => {
    vi.mocked(paymentsApi.useGetPaymentMethodsQuery).mockReturnValue({
      data: { methods: ['credit_card', 'debit_card', 'paypal', 'apple_pay'] },
      isLoading: false,
    } as never);
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

  it('shows loading skeleton while fetching', () => {
    vi.mocked(paymentsApi.useGetPaymentMethodsQuery).mockReturnValue({
      data: undefined,
      isLoading: true,
    } as never);

    renderWithProviders(<PaymentMethodSelector selectedMethod="" onMethodChange={vi.fn()} />);

    expect(screen.queryByRole('radio')).not.toBeInTheDocument();
    expect(document.querySelector('[aria-busy="true"]')).toBeInTheDocument();
  });

  it('renders nothing when methods list is empty', () => {
    vi.mocked(paymentsApi.useGetPaymentMethodsQuery).mockReturnValue({
      data: { methods: [] },
      isLoading: false,
    } as never);

    const { container } = renderWithProviders(
      <PaymentMethodSelector selectedMethod="" onMethodChange={vi.fn()} />
    );

    expect(container.firstChild).toBeNull();
  });

  it('renders fallback icon for unknown payment method', () => {
    vi.mocked(paymentsApi.useGetPaymentMethodsQuery).mockReturnValue({
      data: { methods: ['unknown_method'] },
      isLoading: false,
    } as never);

    renderWithProviders(
      <PaymentMethodSelector selectedMethod="unknown_method" onMethodChange={vi.fn()} />
    );

    expect(screen.getByRole('radio', { name: /unknown method/i })).toBeInTheDocument();
  });
});
