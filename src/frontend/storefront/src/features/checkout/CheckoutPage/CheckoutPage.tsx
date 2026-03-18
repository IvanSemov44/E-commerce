import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useAppSelector } from '@/shared/lib/store';
import { selectIsAuthenticated } from '@/features/auth/slices/authSlice';
import { usePerformanceMonitor } from '@/shared/hooks';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { calculateOrderTotals } from '@/shared/lib/utils/orderCalculations';
import { telemetry } from '@/shared/lib/utils/telemetry';
import { LocationIcon } from '@/shared/components/icons';
import { Button, Card, EmptyState, ErrorAlert, TrustSignals } from '@/shared/components';
import { CheckoutForm, OrderSummary, OrderSuccess } from '@/features/checkout/components';
import { useCheckoutCart, useCheckoutPromo, useCheckoutOrder } from '@/features/checkout/hooks';
import { useGetPaymentMethodsQuery } from '@/features/checkout/api';
import { CHECKOUT_DRAFT_KEY } from '@/features/checkout/constants';
import styles from './CheckoutPage.module.css';

export function CheckoutPage() {
  usePerformanceMonitor();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  // Derive selected payment method — falls back to first available if stored value not in list
  const [paymentMethod, setPaymentMethod] = useState('credit_card');
  const { data: paymentMethodsData } = useGetPaymentMethodsQuery();
  const availableMethods = paymentMethodsData?.methods ?? [];
  const selectedMethod = availableMethods.includes(paymentMethod)
    ? paymentMethod
    : (availableMethods[0] ?? 'credit_card');

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
    paymentMethod: selectedMethod,
  });

  const totals = calculateOrderTotals(subtotal, discount);

  // Clear shipping draft from localStorage once order is confirmed
  useEffect(() => {
    if (!order.orderComplete) return;
    localStorage.removeItem(CHECKOUT_DRAFT_KEY);
  }, [order.orderComplete]);

  // Telemetry — fire once on mount
  useEffect(() => {
    telemetry.track('checkout.view', { isAuthenticated });
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
      <div>
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
                payment={{ method: selectedMethod, onChange: setPaymentMethod }}
              />
            </Card>
          </div>

          <div className={styles.summary}>
            <Card variant="elevated" padding="lg">
              <OrderSummary items={cartItems} totals={totals} promo={promo.promoState} />
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
