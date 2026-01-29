import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { selectCartItems, selectCartSubtotal, clearCart } from '../store/slices/cartSlice';
import { useCreateOrderMutation } from '../store/api/ordersApi';
import { useClearCartMutation } from '../store/api/cartApi';
import type { CreateOrderRequest } from '../store/api/ordersApi';

import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import ErrorAlert from '../components/ErrorAlert';
import styles from './Checkout.module.css';
import CartItem from '../components/CartItem';

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
  const shipping = subtotal > 100 ? 0 : 10;
  const tax = subtotal * 0.08;
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
    return (
      <div className={styles.container}>
        <div className={styles.successContent}>
          <Card variant="elevated" padding="lg">
            <div className={styles.successIcon}>
              <svg
                className={styles.successIconSvg}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={3}
                  d="M5 13l4 4L19 7"
                />
              </svg>
            </div>
            <h1 className={styles.successTitle}>Order Placed Successfully!</h1>
            <p className={styles.successMessage}>Thank you for your purchase.</p>
            <p className={styles.successOrderNumber}>Order Number: {orderNumber}</p>
            <p className={styles.successEmail}>A confirmation email has been sent to {formData.email || 'your email'}</p>
            <div className={styles.successActions}>
              <Link to="/products" className={styles.successActionLink}>
                <Button size="lg">Continue Shopping</Button>
              </Link>
              <Link to="/" className={styles.successActionLink}>
                <Button variant="secondary" size="lg">
                  Return Home
                </Button>
              </Link>
            </div>
          </Card>
        </div>
      </div>
    );
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

              <form onSubmit={handleSubmit} className={styles.form}>
                <div className={styles.formGroup}>
                  <Input
                    label="First Name"
                    type="text"
                    value={formData.firstName}
                    onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                    placeholder="John"
                    required
                  />
                  <Input
                    label="Last Name"
                    type="text"
                    value={formData.lastName}
                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                    placeholder="Doe"
                    required
                  />
                </div>

                <Input
                  label="Email Address"
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  placeholder="your@email.com"
                  required
                />

                <Input
                  label="Phone"
                  type="tel"
                  value={formData.phone}
                  onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                  placeholder="+1 (555) 123-4567"
                  required
                />

                <Input
                  label="Street Address"
                  type="text"
                  value={formData.streetLine1}
                  onChange={(e) => setFormData({ ...formData, streetLine1: e.target.value })}
                  placeholder="123 Main St"
                  required
                />

                <div className={styles.formGroup}>
                  <Input
                    label="City"
                    type="text"
                    value={formData.city}
                    onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                    placeholder="New York"
                    required
                  />
                  <Input
                    label="State"
                    type="text"
                    value={formData.state}
                    onChange={(e) => setFormData({ ...formData, state: e.target.value })}
                    placeholder="NY"
                    required
                  />
                </div>

                <div className={styles.formGroup}>
                  <Input
                    label="Zip Code"
                    type="text"
                    value={formData.postalCode}
                    onChange={(e) => setFormData({ ...formData, postalCode: e.target.value })}
                    placeholder="10001"
                    required
                  />
                  <Input
                    label="Country"
                    type="text"
                    value={formData.country}
                    onChange={(e) => setFormData({ ...formData, country: e.target.value })}
                    placeholder="United States"
                    required
                  />
                </div>

                <Button
                  type="submit"
                  size="lg"
                  className={styles.actionButton}
                >
                  Place Order
                </Button>
              </form>
            </Card>
          </div>

          {/* Order Summary */}
          <div className={styles.summary}>
            <Card variant="elevated" padding="lg">
              <h2 className={styles.summaryTitle}>Order Summary</h2>

              {/* Items */}
              <div className={styles.itemsList}>
                {cartItems.map((item) => (
                  <CartItem
                    key={item.id}
                    item={item}
                    onUpdateQuantity={() => {}}
                    onRemove={() => {}}
                    readOnly={true}
                  />
                ))}
              </div>

              {/* Promo Code */}
              <div className={styles.promoSection}>
                {!promoCodeValidation?.isValid ? (
                  <div className={styles.promoInput}>
                    <Input
                      placeholder="Enter promo code"
                      value={promoCode}
                      onChange={(e) => setPromoCode(e.target.value.toUpperCase())}
                      style={{ flex: 1 }}
                    />
                    <Button
                      onClick={handleApplyPromoCode}
                      disabled={validatingPromoCode || !promoCode.trim()}
                      variant="secondary"
                      size="sm"
                    >
                      {validatingPromoCode ? 'Validating...' : 'Apply'}
                    </Button>
                  </div>
                ) : null}

                {promoCodeValidation && (
                  <div
                    className={`${styles.promoMessage} ${
                      promoCodeValidation.isValid ? styles.promoSuccess : styles.promoError
                    }`}
                  >
                    {promoCodeValidation.message}
                    {promoCodeValidation.isValid && (
                      <button
                        onClick={handleRemovePromoCode}
                        className={styles.promoRemove}
                        type="button"
                      >
                        Remove
                      </button>
                    )}
                  </div>
                )}
              </div>

              {/* Totals */}
              <div className={styles.totalsSection}>
                <div className={styles.totalLine}>
                  <span>Subtotal:</span>
                  <span className={styles.totalValue}>${subtotal.toFixed(2)}</span>
                </div>
                {discount > 0 && (
                  <div className={styles.totalLine} style={{ color: '#16a34a' }}>
                    <span>Discount ({promoCode}):</span>
                    <span className={styles.totalValue}>-${discount.toFixed(2)}</span>
                  </div>
                )}
                <div className={styles.totalLine}>
                  <span>Shipping:</span>
                  <span className={styles.totalValue}>
                    {shipping === 0 ? 'FREE' : `$${shipping.toFixed(2)}`}
                  </span>
                </div>
                <div className={styles.totalLine}>
                  <span>Tax:</span>
                  <span className={styles.totalValue}>${tax.toFixed(2)}</span>
                </div>
              </div>
              <div className={styles.grandTotal}>
                <span>Total:</span>
                <span className={styles.grandTotalAmount}>${total.toFixed(2)}</span>
              </div>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
