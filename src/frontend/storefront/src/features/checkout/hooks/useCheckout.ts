/**
 * useCheckout Hook
 * Manages checkout page state:
 * - Shipping form data
 * - Order creation and submission
 * - Promo code validation
 * - Order completion state
 * - Error handling
 */

import { useState, useCallback, useMemo, useEffect } from 'react';
import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import { useLocalStorage } from '@/shared/hooks/useLocalStorage';
import { selectCartItems, selectCartSubtotal, clearCart } from '../../cart/slices/cartSlice';
import type { CartItem } from '../../cart/slices/cartSlice';
import { authReducer } from '../../auth/slices/authSlice';
import { useCreateOrderMutation } from '@/features/orders/api';
import { useGetCartQuery, useClearCartMutation } from '../../cart/api';
import { useValidatePromoCodeMutation } from '../api';
import { useCheckAvailabilityMutation } from '../api';
import type { StockIssue } from '../api/inventoryApi';
import { useCartSync } from '../../cart/hooks/useCartSync';
import { calculateOrderTotals } from '@/shared/lib/utils/orderCalculations';
import useForm from '@/shared/hooks/useForm';
import { zodValidate } from '@/shared/lib/utils/zodValidate';
import { checkoutSchema } from '../schemas/checkoutSchemas';
import { telemetry } from '@/shared/lib/utils/telemetry';
import type { CreateOrderRequest } from '@/shared/types';

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
  errors: Partial<Record<keyof ShippingFormData, string>>;

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
  isGuestOrder: boolean;

  // Cart info
  cartItems: CartItem[];
  subtotal: number;

  // Totals
  discount: number;
  shipping: number;
  tax: number;
  total: number;

  // Auth state
  isAuthenticated: boolean;

  // Payment method
  paymentMethod: string;
  setPaymentMethod: (method: string) => void;

  // Submit handler
  handleSubmit: (e: React.FormEvent) => Promise<void>;
}

const CHECKOUT_DRAFT_KEY = 'checkout:shippingDraft';

// Selector for authentication state
const selectIsAuthenticated = (state: { auth: ReturnType<typeof authReducer> }) => 
  state.auth.isAuthenticated;

const selectUser = (state: { auth: ReturnType<typeof authReducer> }) => 
  state.auth.user;

// eslint-disable-next-line max-lines-per-function -- Checkout orchestration hook: form state, cart sync, promo codes, order submission, and auth-aware prefill
export function useCheckout(): UseCheckoutReturn {
  const dispatch = useAppDispatch();
  const localCartItems = useAppSelector(selectCartItems);
  const localSubtotal = useAppSelector(selectCartSubtotal);
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const user = useAppSelector(selectUser);

  // Backend cart query (only used when authenticated)
  const { data: backendCart } = useGetCartQuery(undefined, {
    skip: !isAuthenticated
  });

  // Sync local cart with backend on login
  useCartSync({
    enabled: isAuthenticated
  });

  const [createOrder] = useCreateOrderMutation();
  const [clearCartApi] = useClearCartMutation();
  const [validatePromoCodeMutation] = useValidatePromoCodeMutation();
  const [checkAvailabilityMutation] = useCheckAvailabilityMutation();

  // Order state
  const [orderComplete, setOrderComplete] = useState(false);
  const [orderNumber, setOrderNumber] = useState('');
  const [error, setError] = useState<string | null>(null);

  // Payment method state
  const [paymentMethod, setPaymentMethod] = useState('credit_card');

  // Promo code state
  const [promoCode, setPromoCode] = useState('');
  const [promoCodeValidation, setPromoCodeValidation] = useState<PromoCodeValidation | null>(null);
  const [validatingPromoCode, setValidatingPromoCode] = useState(false);

  // Shipping form draft persisted in localStorage — auto-restored on mount
  const [shippingDraft, setShippingDraft] = useLocalStorage<Partial<ShippingFormData>>(
    CHECKOUT_DRAFT_KEY,
    {}
  );

  // Determine which cart items to use (backend for authenticated, local for guest)
  const cartItems: CartItem[] = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.map(item => ({
        id: item.productId,
        name: item.productName,
        slug: '',
        price: item.price,
        quantity: item.quantity,
        maxStock: 99,
        image: item.productImage || item.imageUrl || ''
      }));
    }
    return localCartItems;
  }, [isAuthenticated, backendCart, localCartItems]);

  // Calculate subtotal
  const subtotal = useMemo(() => {
    if (isAuthenticated && backendCart?.items) {
      return backendCart.items.reduce((sum, item) => sum + item.price * item.quantity, 0);
    }
    return localSubtotal;
  }, [isAuthenticated, backendCart, localSubtotal]);

  // Handle order submission (called by useForm after validation)
  const handleFormSubmit = async (values: ShippingFormData) => {
    setError(null);
    telemetry.track('checkout.submit', { itemCount: cartItems.length, subtotal });

    try {
      // Check stock availability before placing order
      const stockCheckResult = await checkAvailabilityMutation({
        items: cartItems.map((item: CartItem) => ({
          productId: item.id,
          quantity: item.quantity,
        })),
      }).unwrap();

      if (!stockCheckResult.isAvailable) {
        const issueMessages = stockCheckResult.issues
          .map((issue: StockIssue) => `${issue.productName}: ${issue.message}`)
          .join(', ');
        setError(`Some items are no longer available: ${issueMessages}`);
        return;
      }

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
      // Clear saved draft once the order is placed
      setShippingDraft({});
      telemetry.track('checkout.complete', {
        orderNumber: result.orderNumber,
        paymentMethod,
        isGuest: !isAuthenticated,
      });
      setOrderComplete(true);
    } catch (err: unknown) {
      const errorObj = err as { data?: { message?: string }; message?: string };
      const message = errorObj.data?.message || errorObj.message || 'Failed to create order. Please try again.';
      telemetry.track('checkout.error', { message });
      setError(message);
    }
  };

  // Initialize useForm hook — restore any previously saved draft
  const form = useForm<ShippingFormData>({
    initialValues: {
      firstName: shippingDraft.firstName ?? '',
      lastName: shippingDraft.lastName ?? '',
      email: shippingDraft.email ?? '',
      phone: shippingDraft.phone ?? '',
      streetLine1: shippingDraft.streetLine1 ?? '',
      city: shippingDraft.city ?? '',
      state: shippingDraft.state ?? '',
      postalCode: shippingDraft.postalCode ?? '',
      country: shippingDraft.country ?? '',
    },
    validate: zodValidate(checkoutSchema),
    onSubmit: handleFormSubmit,
  });

  // Persist form values to localStorage as a draft on every change
  useEffect(() => {
    setShippingDraft(form.values);
  }, [form.values]); // eslint-disable-line react-hooks/exhaustive-deps

  // Fire checkout.view once on mount
  useEffect(() => {
    telemetry.track('checkout.view', { isAuthenticated });
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Pre-fill form with user data when authenticated
  useEffect(() => {
    if (isAuthenticated && user && !form.values.firstName) {
      form.setValues({
        ...form.values,
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email || '',
        phone: user.phone || '',
      });
    }
  }, [isAuthenticated, user]); // eslint-disable-line react-hooks/exhaustive-deps

  // Track if this is a guest order (for showing account creation prompt)
  const [isGuestOrder, setIsGuestOrder] = useState(false);

  // Calculate totals with discount
  const discount = promoCodeValidation?.isValid ? promoCodeValidation.discountAmount : 0;
  const { shipping, tax, total } = calculateOrderTotals(subtotal, discount);

  // Adapter for backward compatibility
  const setFormData = useCallback((data: Partial<ShippingFormData>) => {
    form.setValues({ ...form.values, ...data });
  }, [form]);

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
        message: 'Failed to validate promo code',
      });
    } finally {
      setValidatingPromoCode(false);
    }
  }, [promoCode, subtotal, validatePromoCodeMutation]);

  // Remove promo code
  const handleRemovePromoCode = useCallback(() => {
    setPromoCode('');
    setPromoCodeValidation(null);
  }, []);

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
    isAuthenticated,
    paymentMethod,
    setPaymentMethod,
    handleSubmit: form.handleSubmit,
  };
}
