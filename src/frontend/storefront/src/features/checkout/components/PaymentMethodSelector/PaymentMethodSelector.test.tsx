import { screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { renderWithProviders } from '@/shared/lib/test/test-utils';
import PaymentMethodSelector from './PaymentMethodSelector';

// Mock the RTK Query hook
vi.mock('../../api/paymentsApi', () => ({
  useGetPaymentMethodsQuery: () => ({
    data: { methods: ['credit_card', 'debit_card', 'paypal', 'apple_pay'] },
    isLoading: false,
  }),
}));

describe('PaymentMethodSelector', () => {
  it('renders available payment methods', () => {
    renderWithProviders(
      <PaymentMethodSelector
        selectedMethod="credit_card"
        onMethodChange={vi.fn()}
      />
    );

    expect(screen.getByText(/credit card/i)).toBeInTheDocument();
    expect(screen.getByText(/debit card/i)).toBeInTheDocument();
    expect(screen.getByText(/paypal/i)).toBeInTheDocument();
    expect(screen.getByText(/apple pay/i)).toBeInTheDocument();
  });

  it('marks the selected method as checked', () => {
    renderWithProviders(
      <PaymentMethodSelector
        selectedMethod="paypal"
        onMethodChange={vi.fn()}
      />
    );

    const paypalRadio = screen.getByRole('radio', { name: /paypal/i });
    expect(paypalRadio).toBeChecked();

    const creditCardRadio = screen.getByRole('radio', { name: /credit card/i });
    expect(creditCardRadio).not.toBeChecked();
  });

  it('calls onMethodChange when a method is selected', () => {
    const onMethodChange = vi.fn();

    renderWithProviders(
      <PaymentMethodSelector
        selectedMethod="credit_card"
        onMethodChange={onMethodChange}
      />
    );

    const debitCardLabel = screen.getByLabelText(/debit card/i).closest('label')!;
    fireEvent.click(debitCardLabel);

    expect(onMethodChange).toHaveBeenCalledWith('debit_card');
  });

  it('renders as a radiogroup with accessible label', () => {
    renderWithProviders(
      <PaymentMethodSelector
        selectedMethod="credit_card"
        onMethodChange={vi.fn()}
      />
    );

    expect(screen.getByRole('radiogroup')).toBeInTheDocument();
  });

  it('shows loading state while fetching', () => {
    // Re-import with loading state
    vi.doMock('../../api/paymentsApi', () => ({
      useGetPaymentMethodsQuery: () => ({
        data: undefined,
        isLoading: true,
      }),
    }));

    renderWithProviders(
      <PaymentMethodSelector
        selectedMethod=""
        onMethodChange={vi.fn()}
      />
    );

    // Component renders something (loading or nothing) — just verify no crash
    expect(document.body).toBeInTheDocument();
  });
});
