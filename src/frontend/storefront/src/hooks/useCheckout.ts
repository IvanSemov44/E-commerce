/**
 * useCheckout Hook
 * Manages checkout page state:
 * - Shipping form data
 * - Order creation and submission
 * - Promo code validation
 * - Order completion state
 * - Error handling
 */

import { useState, useCallback } from 'react';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { selectCartItems, selectCartSubtotal, clearCart } from '../store/slices/cartSlice';
import { useCreateOrderMutation } from '../store/api/ordersApi';
import { useClearCartMutation } from '../store/api/cartApi';
import { FREE_SHIPPING_THRESHOLD, STANDARD_SHIPPING_COST, DEFAULT_TAX_RATE } from '../utils/constants';
import type { CreateOrderRequest } from '../store/api/ordersApi';

interface ShippingFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  streetLine1: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

interface PromoCodeValidation {
  isValid: boolean;
  discountAmount: number;
  message?: string;
}

interface UseCheckoutReturn {
  // Form state
  formData: ShippingFormData;
  setFormData: (data: Partial<ShippingFormData>) => void;

  // Promo code state
  promoCode: string;
  setPromoCode: (code: string) => void;
  promoCodeValidation: PromoCodeValidation | null;
  validatingPromoCode: boolean;
  handleApplyPromoCode: () => Promise<void>;
  handleRemovePromoCode: () => void;

  // Order state
  orderComplete: boolean;
  orderNumber: string;
  error: string | null;

  // Cart info
  cartItems: typeof selectCartItems;
  subtotal: number;

  // Totals
  discount: number;
  shipping: number;
  tax: number;
  total: number;

  // Submit handler
  handleSubmit: (e: React.FormEvent) => Promise<void>;
}

export function useCheckout(): UseCheckoutReturn {
  const dispatch = useAppDispatch();
  const cartItems = useAppSelector(selectCartItems);
  const subtotal = useAppSelector(selectCartSubtotal);

  const [createOrder] = useCreateOrderMutation();
  const [clearCartApi] = useClearCartMutation();

  // Order state
  const [orderComplete, setOrderComplete] = useState(false);
  const [orderNumber, setOrderNumber] = useState('');
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [formData, setFormDataState] = useState<ShippingFormData>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    streetLine1: '',
    city: '',
    state: '',
    postalCode: '',
    country: '',
  });

  // Promo code state
  const [promoCode, setPromoCode] = useState('');
  const [promoCodeValidation, setPromoCodeValidation] = useState<PromoCodeValidation | null>(null);
  const [validatingPromoCode, setValidatingPromoCode] = useState(false);

  // Calculate totals with discount
  const discount = promoCodeValidation?.isValid ? promoCodeValidation.discountAmount : 0;
  const shipping = subtotal > FREE_SHIPPING_THRESHOLD ? 0 : STANDARD_SHIPPING_COST;
  const tax = subtotal * DEFAULT_TAX_RATE;
  const total = subtotal - discount + shipping + tax;

  // Update form data
  const setFormData = useCallback((data: Partial<ShippingFormData>) => {
    setFormDataState((prev) => ({ ...prev, ...data }));
  }, []);

  // Validate promo code
  const handleApplyPromoCode = useCallback(async () => {
    if (!promoCode.trim()) {
      setPromoCodeValidation({
        isValid: false,
        discountAmount: 0,
        message: 'Please enter a promo code',
      });
      return;
    }

    setValidatingPromoCode(true);
    setPromoCodeValidation(null);

    try {
      const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';
      const response = await fetch(`${API_URL}/promo-codes/validate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          code: promoCode,
          orderAmount: subtotal,
        }),
      });

      const result = await response.json();

      if (result.success && result.data) {
        setPromoCodeValidation({
          isValid: result.data.isValid,
          discountAmount: result.data.discountAmount,
          message: result.data.message,
        });
      } else {
        setPromoCodeValidation({
          isValid: false,
          discountAmount: 0,
          message: 'Invalid promo code',
        });
      }
    } catch (err) {
      setPromoCodeValidation({
        isValid: false,
        discountAmount: 0,
        message: 'Failed to validate promo code',
      });
    } finally {
      setValidatingPromoCode(false);
    }
  }, [promoCode, subtotal]);

  // Remove promo code
  const handleRemovePromoCode = useCallback(() => {
    setPromoCode('');
    setPromoCodeValidation(null);
  }, []);

  // Submit order
  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);

      // Validation
      if (
        !formData.firstName ||
        !formData.lastName ||
        !formData.email ||
        !formData.phone ||
        !formData.streetLine1 ||
        !formData.city ||
        !formData.state ||
        !formData.postalCode ||
        !formData.country
      ) {
        setError('Please fill in all fields');
        return;
      }

      try {
        // Check stock availability before placing order
        const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';
        const stockCheckResponse = await fetch(`${API_URL}/inventory/check-availability`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            items: cartItems.map((item) => ({
              productId: item.id,
              quantity: item.quantity,
            })),
          }),
        });

        const stockCheckResult = await stockCheckResponse.json();

        if (!stockCheckResult.success) {
          setError('Failed to verify stock availability. Please try again.');
          return;
        }

        if (!stockCheckResult.data.isAvailable) {
          const issueMessages = stockCheckResult.data.issues
            .map((issue: any) => `${issue.productName}: ${issue.message}`)
            .join(', ');
          setError(`Some items are no longer available: ${issueMessages}`);
          return;
        }

        const orderData: CreateOrderRequest = {
          items: cartItems.map((item) => ({
            productId: item.id,
            productName: item.name,
            price: item.price,
            quantity: item.quantity,
          })),
          shippingAddress: {
            firstName: formData.firstName,
            lastName: formData.lastName,
            phone: formData.phone,
            streetLine1: formData.streetLine1,
            city: formData.city,
            state: formData.state,
            postalCode: formData.postalCode,
            country: formData.country,
          },
          paymentMethod: 'card',
          promoCode: promoCodeValidation?.isValid ? promoCode : undefined,
          guestEmail: formData.email,
        };

        const result = await createOrder(orderData).unwrap();

        // Clear cart from backend
        await clearCartApi().unwrap();

        // Clear local cart
        dispatch(clearCart());

        setOrderNumber(result.orderNumber);
        setOrderComplete(true);
      } catch (err: any) {
        setError(err.data?.message || err.message || 'Failed to create order. Please try again.');
      }
    },
    [cartItems, formData, promoCode, promoCodeValidation, createOrder, clearCartApi, dispatch]
  );

  return {
    formData,
    setFormData,
    promoCode,
    setPromoCode,
    promoCodeValidation,
    validatingPromoCode,
    handleApplyPromoCode,
    handleRemovePromoCode,
    orderComplete,
    orderNumber,
    error,
    cartItems,
    subtotal,
    discount,
    shipping,
    tax,
    total,
    handleSubmit,
  };
}
