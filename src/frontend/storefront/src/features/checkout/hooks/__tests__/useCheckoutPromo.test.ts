import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act } from '@testing-library/react';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { useCheckoutPromo } from '../useCheckoutPromo';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('@/shared/lib/store', () => ({
  useAppDispatch: () => vi.fn(),
}));

// Mutable unwrap so individual tests can control the resolved value
const mockValidateUnwrap = vi.fn();
const mockValidate = vi.fn(() => ({ unwrap: mockValidateUnwrap }));

vi.mock('@/features/checkout/api', () => ({
  useValidatePromoCodeMutation: () => [mockValidate, { isLoading: false }],
}));

describe('useCheckoutPromo', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockValidateUnwrap.mockResolvedValue({
      isValid: true,
      discountAmount: 10,
      message: 'Applied!',
    });
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
  });

  it('calls mutation and sets valid validation on success', async () => {
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
    mockValidateUnwrap.mockRejectedValue(new Error('Network error'));

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

    expect(result.current.promoCodeValidation?.isValid).toBe(false);
    expect(result.current.promoCodeValidation?.message).toBe('checkout.promoCodeRequired');
  });

  it('sets invalid validation when server returns isValid false', async () => {
    mockValidateUnwrap.mockResolvedValue({
      isValid: false,
      discountAmount: 0,
      message: 'Code expired',
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
    let resolve!: (value: { isValid: boolean; discountAmount: number; message: string }) => void;
    mockValidateUnwrap.mockReturnValue(
      new Promise((res) => {
        resolve = res;
      })
    );

    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('SLOW');
    });

    let applyPromise!: Promise<void>;
    act(() => {
      applyPromise = result.current.promoState.onApply();
    });

    expect(result.current.promoState.isValidating).toBe(true);

    await act(async () => {
      resolve({ isValid: true, discountAmount: 5, message: 'OK' });
      await applyPromise;
    });

    expect(result.current.promoState.isValidating).toBe(false);
  });
});
