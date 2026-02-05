import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { selectCartItems, selectCartSubtotal, clearCart } from '../store/slices/cartSlice';
import { useCreateOrderMutation } from '../store/api/ordersApi';
import { useClearCartMutation } from '../store/api/cartApi';
import type { CreateOrderRequest } from '../store/api/ordersApi';
import { FREE_SHIPPING_THRESHOLD, STANDARD_SHIPPING_COST, DEFAULT_TAX_RATE } from '../utils/constants';

import Button from '../components/ui/Button';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import ErrorAlert from '../components/ErrorAlert';
import { CheckoutForm, OrderSummary, OrderSuccess } from './components/Checkout';
import styles from './Checkout.module.css';

export default function Checkout() {
  const dispatch = useAppDispatch();
  const cartItems = useAppSelector(selectCartItems);
  const subtotal = useAppSelector(selectCartSubtotal);

  const [createOrder] = useCreateOrderMutation();
  const [clearCartApi] = useClearCartMutation();

  const [orderComplete, setOrderComplete] = useState(false);
  const [orderNumber, setOrderNumber] = useState('');
  const [error, setError] = useState<string | null>(null);

  // Promo code state
  const [promoCode, setPromoCode] = useState('');
  const [promoCodeValidation, setPromoCodeValidation] = useState<{
    isValid: boolean;
    discountAmount: number;
    message?: string;
  } | null>(null);
  const [validatingPromoCode, setValidatingPromoCode] = useState(false);

  // Calculate totals with discount
  const discount = promoCodeValidation?.isValid ? promoCodeValidation.discountAmount : 0;
  const shipping = subtotal > FREE_SHIPPING_THRESHOLD ? 0 : STANDARD_SHIPPING_COST;
  const tax = subtotal * DEFAULT_TAX_RATE;
  const total = subtotal - discount + shipping + tax;

  // Form state
  const [formData, setFormData] = useState({
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

  const handleApplyPromoCode = async () => {
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
  };

  const handleRemovePromoCode = () => {
    setPromoCode('');
    setPromoCodeValidation(null);
  };

  // Redirect if cart is empty
  if (cartItems.length === 0 && !orderComplete) {
    return (
      <div className={styles.container}>
        <div className={styles.successContent}>
          <EmptyState
            title="Your cart is empty"
            description="Add items to your cart before checking out."
            action={
              <Link to="/products">
                <Button size="lg">Browse Products</Button>
              </Link>
            }
          />
        </div>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
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
  };

  // Success screen
  if (orderComplete) {
    return <OrderSuccess orderNumber={orderNumber} email={formData.email} />;
  }

  // Checkout form
  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <PageHeader title="Checkout" />

        <div className={styles.grid}>
          {/* Shipping Form */}
          <div>
            <Card variant="elevated" padding="lg">
              <h2 className={styles.formTitle}>Shipping Information</h2>
              {error && <ErrorAlert message={error} />}
              <CheckoutForm
                formData={formData}
                onFormDataChange={setFormData}
                onSubmit={handleSubmit}
              />
            </Card>
          </div>

          {/* Order Summary */}
          <div className={styles.summary}>
            <Card variant="elevated" padding="lg">
              <OrderSummary
                cartItems={cartItems}
                subtotal={subtotal}
                discount={discount}
                shipping={shipping}
                tax={tax}
                total={total}
                promoCode={promoCode}
                onPromoCodeChange={setPromoCode}
                promoCodeValidation={promoCodeValidation}
                validatingPromoCode={validatingPromoCode}
                onApplyPromoCode={handleApplyPromoCode}
                onRemovePromoCode={handleRemovePromoCode}
              />
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
