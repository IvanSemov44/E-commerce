import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import { formatPrice } from '@/shared/lib/utils/priceFormatter';
import { OrderTotalsDisplay } from '@/shared/components/OrderTotalsDisplay/OrderTotalsDisplay';
import type { CartItem } from '@/features/cart/slices/cartSlice';
import type { OrderTotals, PromoCodeState } from '@/features/checkout/types';
import styles from './OrderSummary.module.css';

interface OrderSummaryProps {
  items: CartItem[];
  totals: OrderTotals;
  promo: PromoCodeState;
}

export function OrderSummary({ items, totals, promo }: OrderSummaryProps) {
  const { t } = useTranslation();
  const { code, validation, isValidating, onChange, onApply, onRemove } = promo;

  return (
    <div className={styles.container}>
      <h2 className={styles.title}>{t('checkout.orderSummary')}</h2>

      {/* Items */}
      <div className={styles.items}>
        {items.map((item) => (
          <div key={item.id} className={styles.item}>
            {item.image && <img src={item.image} alt={item.name} className={styles.itemImage} />}
            <div className={styles.itemContent}>
              <p className={styles.itemName}>{item.name}</p>
              <p className={styles.itemMeta}>
                {item.quantity} x {formatPrice(item.price)}
              </p>
            </div>
            <p className={styles.itemPrice}>{formatPrice(item.price * item.quantity)}</p>
          </div>
        ))}
      </div>

      {/* Promo Code */}
      <div className={styles.promoSection}>
        <label htmlFor="promoCode" className={styles.promoLabel}>
          {t('checkout.promoCode')}
        </label>
        <div className={styles.promoRow}>
          <input
            type="text"
            id="promoCode"
            value={code}
            onChange={(e) => onChange(e.target.value)}
            placeholder={t('checkout.enterPromoCode')}
            className={styles.promoInput}
          />
          {code ? (
            <Button variant="outline" size="sm" onClick={onRemove}>
              {t('checkout.remove')}
            </Button>
          ) : (
            <Button size="sm" onClick={onApply} disabled={isValidating}>
              {isValidating ? t('checkout.applying') : t('checkout.apply')}
            </Button>
          )}
        </div>
        {validation && (
          <p className={validation.isValid ? styles.validationSuccess : styles.validationError}>
            {validation.message}
          </p>
        )}
      </div>

      {/* Totals */}
      <OrderTotalsDisplay {...totals} />
    </div>
  );
}
