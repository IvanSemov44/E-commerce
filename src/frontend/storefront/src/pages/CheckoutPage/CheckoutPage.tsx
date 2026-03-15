import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { usePerformanceMonitor } from '@/shared/hooks';
import { useCheckout } from '@/features/checkout/hooks/useCheckout';
import { LocationIcon } from '@/shared/components/icons';
import Card from '@/shared/components/ui/Card';
import Button from '@/shared/components/ui/Button';
import EmptyState from '@/shared/components/ui/EmptyState';
import ErrorAlert from '@/shared/components/ErrorAlert';
import TrustSignals from '@/shared/components/TrustSignals';
import CheckoutForm from '@/features/checkout/components/CheckoutForm';
import OrderSummary from '@/features/checkout/components/OrderSummary';
import OrderSuccess from '@/features/checkout/components/OrderSuccess';
import styles from './CheckoutPage.module.css';

export default function CheckoutPage() {
  usePerformanceMonitor();
  const { t } = useTranslation();
  const navigate = useNavigate();
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
    paymentMethod,
    setPaymentMethod,
  } = useCheckout();

  // Redirect if cart is empty
  if (cartItems.length === 0 && !orderComplete) {
    return (
      <div className={styles.container}>
        <div className={styles.successContent}>
          <EmptyState
            icon="cart"
            title={t('cart.emptyCart')}
            description={t('checkout.addItemsBeforeCheckout')}
            action={
              <Button onClick={() => navigate('/products')}>{t('products.browseProducts')}</Button>
            }
          />
        </div>
      </div>
    );
  }

  // Success screen
  if (orderComplete) {
    return (
      <OrderSuccess orderNumber={orderNumber} email={formData.email} isGuestOrder={isGuestOrder} />
    );
  }

  // Checkout form
  return (
    <div className={styles.container}>
      <div className={styles.content}>
        {/* Page Header with Progress */}
        <div className={styles.checkoutHeader}>
          <h1 className={styles.checkoutTitle}>{t('checkout.secureCheckout')}</h1>
          <p className={styles.checkoutSubtitle}>{t('checkout.completeOrderSubtitle')}</p>
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
                <LocationIcon />
                {t('checkout.deliveryAddress')}
              </h2>
              {error && <ErrorAlert message={error} />}
              <CheckoutForm
                formData={formData}
                errors={errors}
                onFormDataChange={setFormData}
                onSubmit={handleSubmit}
                isAuthenticated={isAuthenticated}
                selectedPaymentMethod={paymentMethod}
                onPaymentMethodChange={setPaymentMethod}
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
