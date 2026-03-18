import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useAppSelector } from '@/shared/lib/store';
import type { RootState } from '@/shared/lib/store';
import { usePerformanceMonitor } from '@/shared/hooks';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { calculateOrderTotals } from '@/shared/lib/utils/orderCalculations';
import { telemetry } from '@/shared/lib/utils/telemetry';
import { LocationIcon } from '@/shared/components/icons';
import { Card } from '@/shared/components/ui/Card';
import { Button } from '@/shared/components/ui/Button';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import ErrorAlert from '@/shared/components/ErrorAlert';
import TrustSignals from '@/shared/components/TrustSignals';
import CheckoutForm from '@/features/checkout/components/CheckoutForm';
import { OrderSummary } from '@/features/checkout/components/OrderSummary';
import OrderSuccess from '@/features/checkout/components/OrderSuccess';
import { useCheckoutCart } from '../../hooks/useCheckoutCart';
import { useCheckoutPromo } from '../../hooks/useCheckoutPromo';
import { useCheckoutOrder } from '../../hooks/useCheckoutOrder';
import { useGetPaymentMethodsQuery } from '../../api';
import styles from './CheckoutPage.module.css';

const selectIsAuthenticated = (state: RootState) => state.auth.isAuthenticated;

export default function CheckoutPage() {
  usePerformanceMonitor();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  // Shared state
  const [paymentMethod, setPaymentMethod] = useState('credit_card');
  const { data: paymentMethodsData } = useGetPaymentMethodsQuery();

  // Sync paymentMethod with the first available method returned by the API
  useEffect(() => {
    const methods = paymentMethodsData?.methods ?? [];
    if (methods.length > 0 && !methods.includes(paymentMethod)) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setPaymentMethod(methods[0]);
    }
  }, [paymentMethodsData]); // eslint-disable-line react-hooks/exhaustive-deps

  // Feature hooks
  const { cartItems, subtotal, isLoading } = useCheckoutCart();
  const promo = useCheckoutPromo({ subtotal });

  const discount = promo.promoCodeValidation?.isValid
    ? promo.promoCodeValidation.discountAmount
    : 0;

  const order = useCheckoutOrder({
    cartItems,
    subtotal,
    promoCode: promo.promoCode,
    promoCodeValidation: promo.promoCodeValidation,
    paymentMethod,
  });

  const totals = calculateOrderTotals(subtotal, discount);

  // Telemetry — fire once on mount
  const isAuthenticatedRef = useRef(isAuthenticated);
  useEffect(() => {
    isAuthenticatedRef.current = isAuthenticated;
  });
  useEffect(() => {
    telemetry.track('checkout.view', { isAuthenticated: isAuthenticatedRef.current });
  }, []);

  // Guard clauses (loading → empty → success → form)
  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.successContent} role="status" aria-label={t('common.loading')} />
      </div>
    );
  }

  if (cartItems.length === 0 && !order.orderComplete) {
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

  if (order.orderComplete) {
    return (
      <OrderSuccess
        orderNumber={order.orderNumber}
        email={order.orderEmail}
        isGuestOrder={order.isGuestOrder}
      />
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
              {order.error && <ErrorAlert message={order.error} />}
              <CheckoutForm
                onSubmit={order.handleFormSubmit}
                payment={{ method: paymentMethod, onChange: setPaymentMethod }}
              />
            </Card>
          </div>

          <div className={styles.summary}>
            <Card variant="elevated" padding="lg">
              <OrderSummary
                cart={{ items: cartItems }}
                totals={totals}
                promo={{
                  code: promo.promoCode,
                  validation: promo.promoCodeValidation,
                  isValidating: promo.validatingPromoCode,
                  onChange: promo.setPromoCode,
                  onApply: promo.handleApplyPromoCode,
                  onRemove: promo.handleRemovePromoCode,
                }}
              />
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
