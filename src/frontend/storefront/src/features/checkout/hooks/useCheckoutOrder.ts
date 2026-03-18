/**
 * useCheckoutOrder Hook
 * Manages order submission, stock check, cart clearing, and success state
 */

import { useState, useCallback, useEffect, useMemo, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import type { RootState } from '@/shared/lib/store';
import { clearCart } from '@/features/cart/slices/cartSlice';
import type { CartItem } from '@/features/cart/slices/cartSlice';
import { useCreateOrderMutation } from '@/features/orders/api';
import { useClearCartMutation } from '@/features/cart/api';
import { useCheckAvailabilityMutation } from '../api';
import type { StockIssue } from '../api/inventoryApi';
import type { CreateOrderRequest } from '@/shared/types';
import type { ShippingFormData, PromoCodeValidation } from '../checkout.types';
import { telemetry } from '@/shared/lib/utils/telemetry';

const selectIsAuthenticated = (state: RootState) => state.auth.isAuthenticated;

interface UseCheckoutOrderReturn {
  orderComplete: boolean;
  orderNumber: string;
  orderEmail: string;
  error: string | null;
  isGuestOrder: boolean;
  handleFormSubmit: (values: ShippingFormData) => Promise<void>;
}

interface UseCheckoutOrderOptions {
  cartItems: CartItem[];
  subtotal: number;
  promoCode: string;
  promoCodeValidation: PromoCodeValidation | null;
  paymentMethod: string;
}

export function useCheckoutOrder(options: UseCheckoutOrderOptions): UseCheckoutOrderReturn {
  const { cartItems, subtotal, promoCode, promoCodeValidation, paymentMethod } = options;

  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  const [createOrder] = useCreateOrderMutation();
  const [clearCartApi] = useClearCartMutation();
  const [checkAvailabilityMutation] = useCheckAvailabilityMutation();

  // Order state
  const [orderComplete, setOrderComplete] = useState(false);
  const [orderNumber, setOrderNumber] = useState('');
  const [orderEmail, setOrderEmail] = useState('');
  const [isGuestOrder, setIsGuestOrder] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const cartItemsPayload = useMemo(
    () => cartItems.map((item: CartItem) => ({ productId: item.id, quantity: item.quantity })),
    [cartItems]
  );

  // Eager stock check on page load — surfaces issues before form submission
  const hasCheckedStockRef = useRef(false);
  useEffect(() => {
    if (cartItems.length === 0 || hasCheckedStockRef.current) return;
    hasCheckedStockRef.current = true;

    checkAvailabilityMutation({ items: cartItemsPayload })
      .unwrap()
      .then((result) => {
        if (!result.isAvailable) {
          const issueMessages = result.issues
            .map((issue: StockIssue) => `${issue.productName}: ${issue.message}`)
            .join(', ');
          setError(t('checkout.stockIssues', { issues: issueMessages }));
        }
      })
      .catch(() => {
        // Silent early check failure — will re-check on submit
      });
  }, [cartItems.length, cartItemsPayload, checkAvailabilityMutation, t]);

  // Handle order submission (called by useForm after validation)
  const handleFormSubmit = useCallback(
    async (values: ShippingFormData) => {
      setError(null);
      telemetry.track('checkout.submit_attempt', { itemCount: cartItems.length, subtotal });

      // Step 1: Stock availability check
      let stockCheckResult;
      try {
        stockCheckResult = await checkAvailabilityMutation({ items: cartItemsPayload }).unwrap();
      } catch {
        setError(t('checkout.stockCheckFailed'));
        return;
      }

      if (!stockCheckResult.isAvailable) {
        const issueMessages = stockCheckResult.issues
          .map((issue: StockIssue) => `${issue.productName}: ${issue.message}`)
          .join(', ');
        setError(t('checkout.stockIssues', { issues: issueMessages }));
        return;
      }

      // Step 2: Order creation
      const orderData: CreateOrderRequest = {
        items: cartItemsPayload,
        shippingAddress: {
          firstName: values.firstName,
          lastName: values.lastName,
          phone: values.phone,
          streetLine1: values.streetLine1,
          city: values.city,
          state: values.state,
          postalCode: values.postalCode,
          country: values.country,
        },
        paymentMethod: paymentMethod,
        promoCode: promoCodeValidation?.isValid ? promoCode : undefined,
        guestEmail: values.email,
      };

      let result;
      try {
        result = await createOrder(orderData).unwrap();
      } catch (err: unknown) {
        const errorObj = err as { data?: { message?: string }; message?: string };
        const message = errorObj.data?.message || errorObj.message || t('checkout.orderFailed');
        telemetry.track('checkout.error', { message });
        setError(message);
        return;
      }

      // Step 3: Clear carts — non-blocking, order is already confirmed
      try {
        await clearCartApi().unwrap();
      } catch {
        telemetry.track('checkout.cart_clear_failed', { orderNumber: result.orderNumber });
      }

      // Only clear local Redux cart for guests; authenticated users have no local cart items
      if (!isAuthenticated) {
        dispatch(clearCart());
      }

      setIsGuestOrder(!isAuthenticated);
      setOrderNumber(result.orderNumber);
      setOrderEmail(values.email);
      telemetry.track('checkout.complete', {
        orderNumber: result.orderNumber,
        paymentMethod,
        isGuest: !isAuthenticated,
      });
      setOrderComplete(true);
    },
    [
      cartItemsPayload,
      cartItems.length,
      subtotal,
      paymentMethod,
      promoCode,
      promoCodeValidation,
      isAuthenticated,
      t,
      dispatch,
      createOrder,
      clearCartApi,
      checkAvailabilityMutation,
    ]
  );

  return {
    orderComplete,
    orderNumber,
    orderEmail,
    error,
    isGuestOrder,
    handleFormSubmit,
  };
}
