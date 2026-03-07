import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { formatPrice } from '@/shared/lib/utils/priceFormatter';
import type { OrderCardProps } from './OrderCard.types';
import { formatOrderDate, getStatusClassName } from './OrderCard.utils';
import styles from './OrderCard.module.css';

/**
 * OrderCard Component
 *
 * Displays a summary card for a single order in the order history.
 * Shows order number, status, total amount, and a preview of items.
 */
export default function OrderCard({ order }: OrderCardProps) {
  const { t } = useTranslation();

  const formattedDate = formatOrderDate(order.createdAt);
  const statusClassName = getStatusClassName(order.status, styles);

  const itemCount = order.items.length;
  const itemsLabel = itemCount === 1
    ? t('orders.oneItem') || '1 item'
    : t('orders.multipleItems', { count: itemCount }) || `${itemCount} items`;

  return (
    <Link to={`/orders/${order.id}`} className={styles.card}>
      <div className={styles.header}>
        <div className={styles.orderInfo}>
          <h3 className={styles.orderNumber}>
            {t('orders.orderNumber')}: {order.orderNumber}
          </h3>
          <p className={styles.date}>{formattedDate}</p>
        </div>
        <div className={`${styles.status} ${statusClassName}`}>
          {t(`orders.status.${order.status.toLowerCase()}`) || order.status}
        </div>
      </div>

      <div className={styles.content}>
        <div className={styles.items}>
          <p className={styles.itemsLabel}>{itemsLabel}</p>
          {order.items.length > 0 && (
            <p className={styles.itemNames}>
              {order.items.slice(0, 2).map((item) => item.productName).join(', ')}
              {order.items.length > 2 && ` +${order.items.length - 2} ${t('orders.more') || 'more'}`}
            </p>
          )}
        </div>

        <div className={styles.total}>
          <p className={styles.totalLabel}>{t('orders.total')}:</p>
          <p className={styles.totalAmount}>
            {formatPrice(order.totalAmount)}
          </p>
        </div>
      </div>

      <div className={styles.footer}>
        <span className={styles.viewButton}>
          {t('orders.viewDetails')} →
        </span>
      </div>
    </Link>
  );
}
