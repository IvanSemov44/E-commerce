/**
 * useCheckout Hook
 * Orchestrates checkout page state by composing sub-hooks:
 * - useCheckoutForm: shipping form state, validation, localStorage draft, user pre-fill
 * - useCheckoutCart: dual cart logic (local vs backend), subtotal calculation
 * - useCheckoutPromo: promo code state and validation
 * - useCheckoutOrder: stock check, order submission, cart clearing, success state
 */

import { useState, useCallback, useEffect, useRef } from 'react';
import { useAppSelector } from '@/shared/lib/store';
import type { RootState } from '@/shared/lib/store';
import { useLocalStorage } from '@/shared/hooks/useLocalStorage';
import { calculateOrderTotals } from '@/shared/lib/utils/orderCalculations';
import { telemetry } from '@/shared/lib/utils/telemetry';

import { useCheckoutForm } from './useCheckoutForm';
import { useCheckoutCart } from './useCheckoutCart';
import { useCheckoutPromo } from './useCheckoutPromo';
import { useCheckoutOrder } from './useCheckoutOrder';

import type { UseCheckoutReturn } from '../checkout.types';
import { CHECKOUT_DRAFT_KEY } from '../constants';
import type { ShippingFormData } from '../checkout.types';

const selectIsAuthenticated = (state: RootState) => state.auth.isAuthenticated;

export function useCheckout(): UseCheckoutReturn {
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  // Payment method state
  const [paymentMethod, setPaymentMethod] = useState('credit_card');

  // Get cart and subtotal
  const { cartItems, subtotal } = useCheckoutCart();

  // Get promo code state
  const {
    promoCode,
    setPromoCode,
    promoCodeValidation,
    validatingPromoCode,
    handleApplyPromoCode,
    handleRemovePromoCode,
  } = useCheckoutPromo({ subtotal });

  // Get order submission state
  const { orderComplete, orderNumber, error, isGuestOrder, handleFormSubmit } = useCheckoutOrder({
    cartItems,
    subtotal,
    promoCode,
    promoCodeValidation,
    paymentMethod,
  });

  // Initialize form with onSubmit handler
  const { form } = useCheckoutForm({
    onSubmit: handleFormSubmit,
  });

  // Backward-compatible setFormData adapter
  const setFormData = useCallback(
    (data: Partial<ShippingFormData>) => {
      form.setValues({ ...form.values, ...data });
    },
    [form]
  );

  // Clear shipping draft after order is complete
  const [, setShippingDraft] = useLocalStorage<Partial<ShippingFormData>>(CHECKOUT_DRAFT_KEY, {});

  useEffect(() => {
    if (orderComplete) {
      setShippingDraft({});
    }
  }, [orderComplete, setShippingDraft]);

  // Fire checkout.view once on mount
  const isAuthenticatedRef = useRef(isAuthenticated);
  useEffect(() => {
    isAuthenticatedRef.current = isAuthenticated;
  });

  useEffect(() => {
    telemetry.track('checkout.view', { isAuthenticated: isAuthenticatedRef.current });
  }, []);

  // Calculate totals with discount
  const discount = promoCodeValidation?.isValid ? promoCodeValidation.discountAmount : 0;
  const { shipping, tax, total } = calculateOrderTotals(subtotal, discount);

  return {
    formData: form.values,
    setFormData,
    errors: form.errors,
    promoCode,
    setPromoCode,
    promoCodeValidation,
    validatingPromoCode,
    handleApplyPromoCode,
    handleRemovePromoCode,
    orderComplete,
    orderNumber,
    error,
    isGuestOrder,
    cartItems,
    subtotal,
    discount,
    shipping,
    tax,
    total,
    paymentMethod,
    setPaymentMethod,
    handleSubmit: form.handleSubmit,
  };
}
