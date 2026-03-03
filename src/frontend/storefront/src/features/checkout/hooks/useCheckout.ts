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
import { selectCartItems, selectCartSubtotal, clearCart } from '../../cart/slices/cartSlice';
import type { CartItem } from '../../cart/slices/cartSlice';
import { authReducer } from '../../auth/slices/authSlice';
import { useCreateOrderMutation } from '@/features/orders/api';
import { useGetCartQuery, useClearCartMutation } from '../../cart/api';
import { useValidatePromoCodeMutation } from '../api';
import { useCheckAvailabilityMutation } from '../api';
import type { StockIssue } from '../api/inventoryApi';
import { useCartSync } from '../../cart/hooks/useCartSync';
import { FREE_SHIPPING_THRESHOLD, STANDARD_SHIPPING_COST, DEFAULT_TAX_RATE } from '@/shared/lib/utils/constants';
import useForm from '@/shared/hooks/useForm';
import { validators } from '@/shared/lib/utils/validation';
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

  // Submit handler
  handleSubmit: (e: React.FormEvent) => Promise<void>;
}

// Validation function for checkout form
const validateCheckoutForm = (values: ShippingFormData): Partial<Record<keyof ShippingFormData, string>> => {
  const errors: Partial<Record<keyof ShippingFormData, string>> = {};

  const firstNameError = validators.required('First name')(values.firstName);
  if (firstNameError) errors.firstName = firstNameError;

  const lastNameError = validators.required('Last name')(values.lastName);
  if (lastNameError) errors.lastName = lastNameError;

  const emailRequiredError = validators.required('Email')(values.email);
  if (emailRequiredError) {
    errors.email = emailRequiredError;
  } else {
    const emailFormatError = validators.email(values.email);
    if (emailFormatError) errors.email = emailFormatError;
  }

  const phoneRequiredError = validators.required('Phone')(values.phone);
  if (phoneRequiredError) {
    errors.phone = phoneRequiredError;
  } else {
    const phoneFormatError = validators.phone(values.phone);
    if (phoneFormatError) errors.phone = phoneFormatError;
  }

  const streetError = validators.required('Street address')(values.streetLine1);
  if (streetError) errors.streetLine1 = streetError;

  const cityError = validators.required('City')(values.city);
  if (cityError) errors.city = cityError;

  const stateError = validators.required('State')(values.state);
  if (stateError) errors.state = stateError;

  const postalCodeError = validators.required('Postal code')(values.postalCode);
  if (postalCodeError) errors.postalCode = postalCodeError;

  const countryError = validators.required('Country')(values.country);
  if (countryError) errors.country = countryError;

  return errors;
};

// Selector for authentication state
const selectIsAuthenticated = (state: { auth: ReturnType<typeof authReducer> }) => 
  state.auth.isAuthenticated;

const selectUser = (state: { auth: ReturnType<typeof authReducer> }) => 
  state.auth.user;

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

  // Promo code state
  const [promoCode, setPromoCode] = useState('');
  const [promoCodeValidation, setPromoCodeValidation] = useState<PromoCodeValidation | null>(null);
  const [validatingPromoCode, setValidatingPromoCode] = useState(false);

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
        paymentMethod: 'card',
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
      setOrderComplete(true);
    } catch (err: unknown) {
      const errorObj = err as { data?: { message?: string }; message?: string };
      setError(errorObj.data?.message || errorObj.message || 'Failed to create order. Please try again.');
    }
  };

  // Initialize useForm hook
  const form = useForm<ShippingFormData>({
    initialValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      streetLine1: '',
      city: '',
      state: '',
      postalCode: '',
      country: '',
    },
    validate: validateCheckoutForm,
    onSubmit: handleFormSubmit,
  });

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
  const shipping = subtotal > FREE_SHIPPING_THRESHOLD ? 0 : STANDARD_SHIPPING_COST;
  const tax = subtotal * DEFAULT_TAX_RATE;
  const total = subtotal - discount + shipping + tax;

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
    handleSubmit: form.handleSubmit,
  };
}
