import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import Card from '../../../../shared/components/ui/Card';
import Button from '../../../../shared/components/ui/Button';
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
      
      <div className={styles.totalsSection}>
        <div className={styles.totalLine}>
          <span>{t('cart.subtotal')}:</span>
          <span className={styles.totalValue}>${subtotal.toFixed(2)}</span>
        </div>
        
        <div className={styles.totalLine}>
          <span>{t('cart.shipping')}:</span>
          <span className={styles.totalValue}>
            {shipping === 0 ? t('common.free') : `$${shipping.toFixed(2)}`}
          </span>
        </div>
        
        {showFreeShippingMessage && (
          <div className={styles.shippingMessage}>
            Add ${freeShippingRemaining.toFixed(2)} more for free shipping!
          </div>
        )}
        
        <div className={styles.totalLine}>
          <span>{t('cart.tax')} (8%):</span>
          <span className={styles.totalValue}>${tax.toFixed(2)}</span>
        </div>
      </div>
      
      <div className={styles.grandTotal}>
        <span>{t('cart.total')}:</span>
        <span className={styles.grandTotalAmount}>${total.toFixed(2)}</span>
      </div>
      
      <div className={styles.actions}>
        <Link to="/checkout" className={styles.actionLink}>
          <Button size="lg">{t('cart.proceedToCheckout')}</Button>
        </Link>
        <Link to="/products" className={styles.actionLink}>
          <Button variant="secondary" size="lg">{t('cart.continueShopping')}</Button>
        </Link>
      </div>
    </Card>
  );
}
