import { Link } from 'react-router-dom';
import { useCheckout } from '../hooks';
import Button from '../components/ui/Button';
import Card from '../components/ui/Card';
import PageHeader from '../components/PageHeader';
import EmptyState from '../components/EmptyState';
import ErrorAlert from '../components/ErrorAlert';
import TrustSignals from '../components/TrustSignals';
import { CheckoutForm, OrderSummary, OrderSuccess } from './components/Checkout';
import styles from './Checkout.module.css';

export default function Checkout() {
  const {
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
    errors,
    handleSubmit,
    isAuthenticated,
    isGuestOrder,
  } = useCheckout();

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

  // Success screen
  if (orderComplete) {
    return (
      <OrderSuccess 
        orderNumber={orderNumber} 
        email={formData.email} 
        isGuestOrder={isGuestOrder}
      />
    );
  }

  // Checkout form
  return (
    <div className={styles.container}>
      <div className={styles.content}>
        {/* Page Header with Progress */}
        <div className={styles.checkoutHeader}>
          <h1 className={styles.checkoutTitle}>Secure Checkout</h1>
          <p className={styles.checkoutSubtitle}>Complete your order safely and quickly</p>
        </div>

        {/* Trust Signals Bar */}
        <div className={styles.trustSignalsWrapper}>
          <TrustSignals />
        </div>

        <div className={styles.grid}>
          {/* Shipping Form */}
          <div>
            <Card variant="elevated" padding="lg">
              <h2 className={styles.formTitle}>
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/>
                  <circle cx="12" cy="10" r="3"/>
                </svg>
                Delivery Address
              </h2>
              {error && <ErrorAlert message={error} />}
              <CheckoutForm
                formData={formData}
                errors={errors}
                onFormDataChange={setFormData}
                onSubmit={handleSubmit}
                isAuthenticated={isAuthenticated}
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
