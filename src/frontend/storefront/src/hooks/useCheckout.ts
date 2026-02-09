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
import type { CartItem } from '../store/slices/cartSlice';
import { useCreateOrderMutation } from '../store/api/ordersApi';
import { useClearCartMutation } from '../store/api/cartApi';
import { useValidatePromoCodeMutation } from '../store/api/promoCodeApi';
import { useCheckAvailabilityMutation } from '../store/api/inventoryApi';
import type { StockIssue } from '../store/api/inventoryApi';
import { FREE_SHIPPING_THRESHOLD, STANDARD_SHIPPING_COST, DEFAULT_TAX_RATE } from '../utils/constants';
import useForm from './useForm';
import { validators } from '../utils/validation';
import type { CreateOrderRequest } from '../types';

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

  // Cart info
  cartItems: CartItem[];
  subtotal: number;

  // Totals
  discount: number;
  shipping: number;
  tax: number;
  total: number;

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

export function useCheckout(): UseCheckoutReturn {
  const dispatch = useAppDispatch();
  const cartItems = useAppSelector(selectCartItems);
  const subtotal = useAppSelector(selectCartSubtotal);

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

      setOrderNumber(result.orderNumber);
      setOrderComplete(true);
    } catch (err: any) {
      setError(err.data?.message || err.message || 'Failed to create order. Please try again.');
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
    } catch (err) {
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
    cartItems,
    subtotal,
    discount,
    shipping,
    tax,
    total,
    handleSubmit: form.handleSubmit,
  };
}
