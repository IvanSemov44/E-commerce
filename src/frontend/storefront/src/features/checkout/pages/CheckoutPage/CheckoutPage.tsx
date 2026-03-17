import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { usePerformanceMonitor } from '@/shared/hooks';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { LocationIcon } from '@/shared/components/icons';
import { Card } from '@/shared/components/ui/Card';
import { Button } from '@/shared/components/ui/Button';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import ErrorAlert from '@/shared/components/ErrorAlert';
import TrustSignals from '@/shared/components/TrustSignals';
import CheckoutForm from '@/features/checkout/components/CheckoutForm';
import { OrderSummary } from '@/features/checkout/components/OrderSummary';
import OrderSuccess from '@/features/checkout/components/OrderSuccess';
import { CheckoutProvider, useCheckoutContext } from '../../context/CheckoutContext';
import styles from './CheckoutPage.module.css';

export default function CheckoutPage() {
  usePerformanceMonitor();
  return (
    <CheckoutProvider>
      <CheckoutPageContent />
    </CheckoutProvider>
  );
}

function CheckoutPageContent() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { isLoading, cartItems, orderComplete, orderNumber, error, formData, isGuestOrder } =
    useCheckoutContext();

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.successContent} role="status" aria-label={t('common.loading')} />
      </div>
    );
  }

  if (cartItems.length === 0 && !orderComplete) {
    return (
      <div className={styles.container}>
        <div className={styles.successContent}>
          <EmptyState
            icon="cart"
            title={t('cart.emptyCart')}
            description={t('checkout.addItemsBeforeCheckout')}
            action={
              <Button onClick={() => navigate(ROUTE_PATHS.products)}>
                {t('products.browseProducts')}
              </Button>
            }
          />
        </div>
      </div>
    );
  }

  if (orderComplete) {
    return (
      <OrderSuccess orderNumber={orderNumber} email={formData.email} isGuestOrder={isGuestOrder} />
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <div className={styles.checkoutHeader}>
          <h1 className={styles.checkoutTitle}>{t('checkout.secureCheckout')}</h1>
          <p className={styles.checkoutSubtitle}>{t('checkout.completeOrderSubtitle')}</p>
        </div>

        <div className={styles.trustSignalsWrapper}>
          <TrustSignals />
        </div>

        <div className={styles.grid}>
          <div>
            <Card variant="elevated" padding="lg">
              <h2 className={styles.formTitle}>
                <LocationIcon />
                {t('checkout.deliveryAddress')}
              </h2>
              {error && <ErrorAlert message={error} />}
              <CheckoutForm />
            </Card>
          </div>

          <div className={styles.summary}>
            <Card variant="elevated" padding="lg">
              <OrderSummary />
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
