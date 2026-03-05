import { useTranslation } from 'react-i18next';
import { useCheckout } from '@/features/checkout/hooks/useCheckout';
import Card from '@/shared/components/ui/Card';
import EmptyState from '@/shared/components/ui/EmptyState';
import ErrorAlert from '@/shared/components/ErrorAlert';
import TrustSignals from '@/shared/components/TrustSignals';
import CheckoutForm from '@/features/checkout/components/CheckoutForm';
import OrderSummary from '@/features/checkout/components/OrderSummary';
import OrderSuccess from '@/features/checkout/components/OrderSuccess';
import styles from './CheckoutPage.module.css';

export default function CheckoutPage() {
  const { t } = useTranslation();
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
            icon="cart"
            title={t('cart.emptyCart')}
            description={t('checkout.addItemsBeforeCheckout')}
            actionLabel={t('products.browseProducts')}
            onAction={() => window.location.href = '/products'}
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
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/>
                  <circle cx="12" cy="10" r="3"/>
                </svg>
                {t('checkout.deliveryAddress')}
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
