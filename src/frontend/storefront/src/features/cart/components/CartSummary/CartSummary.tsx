import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Card } from '@/shared/components/ui/Card';
import { Button } from '@/shared/components/ui/Button';
import { OrderTotalsDisplay } from '@/shared/components/OrderTotalsDisplay/OrderTotalsDisplay';
import styles from './CartSummary.module.css';

interface CartSummaryProps {
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
  freeShippingThreshold: number;
}

export default function CartSummary({
  subtotal,
  shipping,
  tax,
  total,
  freeShippingThreshold,
}: CartSummaryProps) {
  const { t } = useTranslation();
  const freeShippingRemaining = freeShippingThreshold - subtotal;
  const showFreeShippingMessage = subtotal > 0 && freeShippingRemaining > 0;

  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>{t('checkout.orderSummary')}</h2>

      {showFreeShippingMessage && (
        <div className={styles.shippingMessage}>
          Add ${freeShippingRemaining.toFixed(2)} more for free shipping!
        </div>
      )}

      <OrderTotalsDisplay
        subtotal={subtotal}
        shipping={shipping}
        tax={tax}
        total={total}
        className={styles.totalsSection}
      />

      <div className={styles.actions}>
        <Link to={ROUTE_PATHS.checkout} className={styles.actionLink}>
          <Button size="lg">{t('cart.proceedToCheckout')}</Button>
        </Link>
        <Link to={ROUTE_PATHS.products} className={styles.actionLink}>
          <Button variant="secondary" size="lg">
            {t('cart.continueShopping')}
          </Button>
        </Link>
      </div>
    </Card>
  );
}
