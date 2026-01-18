import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { selectCartItems, selectCartSubtotal, clearCart } from '../store/slices/cartSlice';

import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import styles from './Checkout.module.css';
import CartItem from '@/components/CartItem';

export default function Checkout() {
  const dispatch = useAppDispatch();
  const cartItems = useAppSelector(selectCartItems);
  const subtotal = useAppSelector(selectCartSubtotal);

  const [isProcessing, setIsProcessing] = useState(false);
  const [orderComplete, setOrderComplete] = useState(false);
  const [orderNumber, setOrderNumber] = useState('');

  // Calculate totals
  const shipping = subtotal > 100 ? 0 : 10;
  const tax = subtotal * 0.08;
  const total = subtotal + shipping + tax;

  // Form state
  const [formData, setFormData] = useState({
    email: '',
    cardNumber: '',
    cardName: '',
    expiry: '',
    cvv: '',
  });

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

    // Basic validation
    if (
      !formData.email ||
      !formData.cardNumber ||
      !formData.cardName ||
      !formData.expiry ||
      !formData.cvv
    ) {
      alert('Please fill in all fields');
      return;
    }

    // Mock payment processing
    setIsProcessing(true);

    // Simulate API call
    setTimeout(() => {
      // Generate order number
      const orderNum =
        'ORD-' +
        Date.now().toString(36).toUpperCase() +
        Math.random().toString(36).slice(2, 7).toUpperCase();
      setOrderNumber(orderNum);
      setOrderComplete(true);
      setIsProcessing(false);

      // Clear cart
      dispatch(clearCart());
    }, 2000);
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
            <p className={styles.successEmail}>A confirmation email has been sent to {formData.email}</p>
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
          {/* Payment Form */}
          <div>
            <Card variant="elevated" padding="lg">
              <h2 className={styles.formTitle}>Payment Information</h2>

              <form onSubmit={handleSubmit} className={styles.form}>
                <Input
                  label="Email Address"
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  placeholder="your@email.com"
                  required
                />

                <div>
                  <Input
                    label="Card Number"
                    type="text"
                    value={formData.cardNumber}
                    onChange={(e) => setFormData({ ...formData, cardNumber: e.target.value })}
                    placeholder="1234 5678 9012 3456"
                    maxLength={19}
                    required
                  />
                  <p className={styles.inputHint}>
                    Use any test card number (e.g., 4242 4242 4242 4242)
                  </p>
                </div>

                <Input
                  label="Cardholder Name"
                  type="text"
                  value={formData.cardName}
                  onChange={(e) => setFormData({ ...formData, cardName: e.target.value })}
                  placeholder="John Doe"
                  required
                />

                <div className={styles.formGroup}>
                  <Input
                    label="Expiry Date"
                    type="text"
                    value={formData.expiry}
                    onChange={(e) => setFormData({ ...formData, expiry: e.target.value })}
                    placeholder="MM/YY"
                    maxLength={5}
                    required
                  />
                  <Input
                    label="CVV"
                    type="text"
                    value={formData.cvv}
                    onChange={(e) => setFormData({ ...formData, cvv: e.target.value })}
                    placeholder="123"
                    maxLength={4}
                    required
                  />
                </div>

                <Button
                  type="submit"
                  disabled={isProcessing}
                  size="lg"
                  className={styles.actionButton}
                  isLoading={isProcessing}
                >
                  {isProcessing ? 'Processing...' : `Pay $${total.toFixed(2)}`}
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

              {/* Totals */}
              <div className={styles.totalsSection}>
                <div className={styles.totalLine}>
                  <span>Subtotal:</span>
                  <span className={styles.totalValue}>${subtotal.toFixed(2)}</span>
                </div>
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
