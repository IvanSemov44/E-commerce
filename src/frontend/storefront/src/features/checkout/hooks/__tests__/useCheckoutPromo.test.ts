import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act } from '@testing-library/react';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import * as checkoutApi from '@/features/checkout/api';
import { useCheckoutPromo } from '../useCheckoutPromo';

const mockValidateMutation = vi.fn();

vi.mock('@/features/checkout/api', () => ({
  useValidatePromoCodeMutation: vi.fn(() => [mockValidateMutation]),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

describe('useCheckoutPromo', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(checkoutApi.useValidatePromoCodeMutation).mockReturnValue([
      mockValidateMutation,
    ] as never);
  });

  it('initialises with empty promo code and no validation', () => {
    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    expect(result.current.promoCode).toBe('');
    expect(result.current.promoCodeValidation).toBeNull();
    expect(result.current.promoState.isValidating).toBe(false);
  });

  it('sets promo code via promoState.onChange', () => {
    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('SAVE10');
    });

    expect(result.current.promoCode).toBe('SAVE10');
    expect(result.current.promoState.code).toBe('SAVE10');
  });

  it('sets invalid validation when applying empty code', async () => {
    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    await act(async () => {
      await result.current.promoState.onApply();
    });

    expect(result.current.promoCodeValidation?.isValid).toBe(false);
    expect(result.current.promoCodeValidation?.message).toBe('checkout.promoCodeRequired');
    expect(mockValidateMutation).not.toHaveBeenCalled();
  });

  it('calls mutation and sets valid validation on success', async () => {
    mockValidateMutation.mockReturnValue({
      unwrap: () => Promise.resolve({ isValid: true, discountAmount: 10, message: 'Applied!' }),
    });

    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('SAVE10');
    });

    await act(async () => {
      await result.current.promoState.onApply();
    });

    expect(result.current.promoCodeValidation?.isValid).toBe(true);
    expect(result.current.promoCodeValidation?.discountAmount).toBe(10);
    expect(result.current.promoState.validation?.isValid).toBe(true);
  });

  it('sets error validation when mutation throws', async () => {
    mockValidateMutation.mockReturnValue({
      unwrap: () => Promise.reject(new Error('network error')),
    });

    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('BAD');
    });

    await act(async () => {
      await result.current.promoState.onApply();
    });

    expect(result.current.promoCodeValidation?.isValid).toBe(false);
    expect(result.current.promoCodeValidation?.message).toBe('checkout.promoCodeValidationFailed');
  });

  it('clears code and validation on promoState.onRemove', async () => {
    mockValidateMutation.mockReturnValue({
      unwrap: () => Promise.resolve({ isValid: true, discountAmount: 5, message: 'OK' }),
    });

    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('CODE');
    });
    await act(async () => {
      await result.current.promoState.onApply();
    });

    act(() => {
      result.current.promoState.onRemove();
    });

    expect(result.current.promoCode).toBe('');
    expect(result.current.promoCodeValidation).toBeNull();
    expect(result.current.promoState.code).toBe('');
    expect(result.current.promoState.validation).toBeNull();
  });

  it('clears validation when the user edits the code after applying', async () => {
    mockValidateMutation.mockReturnValue({
      unwrap: () => Promise.resolve({ isValid: true, discountAmount: 10, message: 'Applied!' }),
    });

    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('SAVE10');
    });
    await act(async () => {
      await result.current.promoState.onApply();
    });
    expect(result.current.promoCodeValidation?.isValid).toBe(true);

    act(() => {
      result.current.promoState.onChange('NEWCODE');
    });

    expect(result.current.promoCodeValidation).toBeNull();
    expect(result.current.promoState.validation).toBeNull();
  });

  it('treats whitespace-only code as empty — skips mutation', async () => {
    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('   ');
    });

    await act(async () => {
      await result.current.promoState.onApply();
    });

    expect(mockValidateMutation).not.toHaveBeenCalled();
    expect(result.current.promoCodeValidation?.isValid).toBe(false);
    expect(result.current.promoCodeValidation?.message).toBe('checkout.promoCodeRequired');
  });

  it('sets invalid validation when server returns isValid false', async () => {
    mockValidateMutation.mockReturnValue({
      unwrap: () => Promise.resolve({ isValid: false, discountAmount: 0, message: 'Code expired' }),
    });

    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('EXPIRED');
    });

    await act(async () => {
      await result.current.promoState.onApply();
    });

    expect(result.current.promoCodeValidation?.isValid).toBe(false);
    expect(result.current.promoCodeValidation?.message).toBe('Code expired');
    expect(result.current.promoCodeValidation?.discountAmount).toBe(0);
  });

  it('sets isValidating true during mutation and false after', async () => {
    let resolveUnwrap!: (value: unknown) => void;
    mockValidateMutation.mockReturnValue({
      unwrap: () =>
        new Promise((res) => {
          resolveUnwrap = res;
        }),
    });

    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('SLOW');
    });

    // Start apply without awaiting
    let applyPromise: Promise<void>;
    act(() => {
      applyPromise = result.current.promoState.onApply();
    });

    expect(result.current.promoState.isValidating).toBe(true);

    // Resolve and wait
    await act(async () => {
      resolveUnwrap({ isValid: true, discountAmount: 5, message: 'OK' });
      await applyPromise;
    });

    expect(result.current.promoState.isValidating).toBe(false);
  });
});
