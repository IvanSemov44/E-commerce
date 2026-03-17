/**
 * useCheckoutOrder Hook
 * Manages order submission, stock check, cart clearing, and success state
 */

import { useState, useCallback } from 'react';
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
  const [isGuestOrder, setIsGuestOrder] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Handle order submission (called by useForm after validation)
  const handleFormSubmit = useCallback(
    async (values: ShippingFormData) => {
      setError(null);
      telemetry.track('checkout.submit_attempt', { itemCount: cartItems.length, subtotal });

      // Step 1: Stock availability check
      let stockCheckResult;
      try {
        stockCheckResult = await checkAvailabilityMutation({
          items: cartItems.map((item: CartItem) => ({
            productId: item.id,
            quantity: item.quantity,
          })),
        }).unwrap();
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
      try {
        const orderData: CreateOrderRequest = {
          items: cartItems.map((item: CartItem) => ({
            productId: item.id,
            quantity: item.quantity,
          })),
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

        const result = await createOrder(orderData).unwrap();

        // Clear cart from backend
        await clearCartApi().unwrap();

        // Clear local cart
        dispatch(clearCart());

        // Track if this was a guest order for account creation prompt
        setIsGuestOrder(!isAuthenticated);
        setOrderNumber(result.orderNumber);
        telemetry.track('checkout.complete', {
          orderNumber: result.orderNumber,
          paymentMethod,
          isGuest: !isAuthenticated,
        });
        setOrderComplete(true);
      } catch (err: unknown) {
        const errorObj = err as { data?: { message?: string }; message?: string };
        const message = errorObj.data?.message || errorObj.message || t('checkout.orderFailed');
        telemetry.track('checkout.error', { message });
        setError(message);
      }
    },
    [
      cartItems,
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
    error,
    isGuestOrder,
    handleFormSubmit,
  };
}
