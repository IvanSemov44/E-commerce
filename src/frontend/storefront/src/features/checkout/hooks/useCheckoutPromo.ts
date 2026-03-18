/**
 * useCheckoutPromo Hook
 * Manages promo code state and validation
 */

import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useValidatePromoCodeMutation } from '@/features/checkout/api';
import type { PromoCodeValidation, PromoCodeState } from '@/features/checkout/types';

interface UseCheckoutPromoReturn {
  // Raw values — used by useCheckoutOrder
  promoCode: string;
  promoCodeValidation: PromoCodeValidation | null;
  // Shaped for OrderSummary — avoids mapping in CheckoutPage
  promoState: PromoCodeState;
}

interface UseCheckoutPromoOptions {
  subtotal: number;
}

export function useCheckoutPromo(options: UseCheckoutPromoOptions): UseCheckoutPromoReturn {
  const { subtotal } = options;
  const { t } = useTranslation();
  const [validatePromoCodeMutation] = useValidatePromoCodeMutation();

  // Promo code state
  const [promoCode, setPromoCode] = useState('');
  const [promoCodeValidation, setPromoCodeValidation] = useState<PromoCodeValidation | null>(null);
  const [validatingPromoCode, setValidatingPromoCode] = useState(false);

  // Validate promo code
  const handleApplyPromoCode = useCallback(async () => {
    if (!promoCode.trim()) {
      setPromoCodeValidation({
        isValid: false,
        discountAmount: 0,
        message: t('checkout.promoCodeRequired'),
      });
      return;
    }

    setValidatingPromoCode(true);
    setPromoCodeValidation(null);

    try {
      const result = await validatePromoCodeMutation({
        code: promoCode,
        orderAmount: subtotal,
      }).unwrap();

      setPromoCodeValidation({
        isValid: result.isValid,
        discountAmount: result.discountAmount,
        message: result.message,
      });
    } catch {
      setPromoCodeValidation({
        isValid: false,
        discountAmount: 0,
        message: t('checkout.promoCodeValidationFailed'),
      });
    } finally {
      setValidatingPromoCode(false);
    }
  }, [promoCode, subtotal, t, validatePromoCodeMutation]);

  // Clear validation when the user edits the code
  const handlePromoCodeChange = useCallback((code: string) => {
    setPromoCode(code);
    setPromoCodeValidation(null);
  }, []);

  // Remove promo code
  const handleRemovePromoCode = useCallback(() => {
    setPromoCode('');
    setPromoCodeValidation(null);
  }, []);

  return {
    promoCode,
    promoCodeValidation,
    promoState: {
      code: promoCode,
      validation: promoCodeValidation,
      isValidating: validatingPromoCode,
      onChange: handlePromoCodeChange,
      onApply: handleApplyPromoCode,
      onRemove: handleRemovePromoCode,
    },
  };
}
