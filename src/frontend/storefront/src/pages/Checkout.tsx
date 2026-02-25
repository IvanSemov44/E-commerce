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
    return <OrderSuccess orderNumber={orderNumber} email={formData.email} />;
  }

  // Checkout form
  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <PageHeader title="Checkout" />

        {/* Trust Signals Bar */}
        <TrustSignals />

        <div className={styles.grid}>
          {/* Shipping Form */}
          <div>
            <Card variant="elevated" padding="lg">
              <h2 className={styles.formTitle}>Shipping Information</h2>
              {error && <ErrorAlert message={error} />}
              <CheckoutForm
                formData={formData}
                errors={errors}
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
