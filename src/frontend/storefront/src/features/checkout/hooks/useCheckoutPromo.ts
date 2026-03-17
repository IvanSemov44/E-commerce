/**
 * useCheckoutPromo Hook
 * Manages promo code state and validation
 */

import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useValidatePromoCodeMutation } from '../api';
import type { PromoCodeValidation } from '../checkout.types';

interface UseCheckoutPromoReturn {
  promoCode: string;
  setPromoCode: (code: string) => void;
  promoCodeValidation: PromoCodeValidation | null;
  validatingPromoCode: boolean;
  handleApplyPromoCode: () => Promise<void>;
  handleRemovePromoCode: () => void;
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

  // Remove promo code
  const handleRemovePromoCode = useCallback(() => {
    setPromoCode('');
    setPromoCodeValidation(null);
  }, []);

  return {
    promoCode,
    setPromoCode,
    promoCodeValidation,
    validatingPromoCode,
    handleApplyPromoCode,
    handleRemovePromoCode,
  };
}
