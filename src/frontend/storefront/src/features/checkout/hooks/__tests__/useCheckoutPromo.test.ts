import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act } from '@testing-library/react';
import { renderHookWithProviders } from '@/shared/lib/test/test-utils';
import { useCheckoutPromo } from '../useCheckoutPromo';
import { server } from '@/shared/lib/test/msw-server';
import { http, HttpResponse } from 'msw';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

const setupHandlers = (
  result: { isValid: boolean; discountAmount: number; message: string } = {
    isValid: true,
    discountAmount: 10,
    message: 'Applied!',
  }
) => {
  server.use(
    http.post('/api/promo-codes/validate', async () => {
      return HttpResponse.json({
        success: true,
        data: result,
      });
    })
  );
};

describe('useCheckoutPromo', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupHandlers();
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
    server.use(
      http.post('/api/checkout/validate-promo', async () => {
        return HttpResponse.json(
          { success: false, errorDetails: { message: 'Network error', code: 'INTERNAL_ERROR' } },
          { status: 500 }
        );
      })
    );

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
    setupHandlers({ isValid: false, discountAmount: 0, message: 'Code expired' });

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
    server.use(
      http.post('/api/checkout/validate-promo', async () => {
        await new Promise((resolve) => setTimeout(resolve, 50));
        return HttpResponse.json({
          success: true,
          data: { isValid: true, discountAmount: 5, message: 'OK' },
        });
      })
    );

    const { result } = renderHookWithProviders(() => useCheckoutPromo({ subtotal: 100 }));

    act(() => {
      result.current.promoState.onChange('SLOW');
    });

    let applyPromise: Promise<void>;
    act(() => {
      applyPromise = result.current.promoState.onApply();
    });

    expect(result.current.promoState.isValidating).toBe(true);

    await act(async () => {
      await applyPromise;
    });

    expect(result.current.promoState.isValidating).toBe(false);
  });
});
