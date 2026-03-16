import { useTranslation } from 'react-i18next';
import { formatPrice } from '@/shared/lib/utils/priceFormatter';
import styles from './OrderTotalsDisplay.module.css';

interface OrderTotalsDisplayProps {
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
  discount?: number;
  className?: string;
}

export default function OrderTotalsDisplay({
  subtotal,
  shipping,
  tax,
  total,
  discount = 0,
  className = '',
}: OrderTotalsDisplayProps) {
  const { t } = useTranslation();

  return (
    <div className={`${styles.container} ${className}`}>
      <div className={styles.row}>
        <span className={styles.label}>{t('cart.subtotal')}</span>
        <span className={styles.value}>{formatPrice(subtotal)}</span>
      </div>

      {discount > 0 && (
        <div className={styles.discountRow}>
          <span>{t('cart.discount')}</span>
          <span className={styles.value}>-{formatPrice(discount)}</span>
        </div>
      )}

      <div className={styles.row}>
        <span className={styles.label}>{t('cart.shipping')}</span>
        <span className={styles.value}>
          {shipping === 0 ? t('cart.free') : formatPrice(shipping)}
        </span>
      </div>

      <div className={styles.row}>
        <span className={styles.label}>{t('cart.tax')}</span>
        <span className={styles.value}>{formatPrice(tax)}</span>
      </div>

      <div className={styles.totalRow}>
        <span className={styles.totalLabel}>{t('cart.total')}</span>
        <span className={styles.totalValue}>{formatPrice(total)}</span>
      </div>
    </div>
  );
}
