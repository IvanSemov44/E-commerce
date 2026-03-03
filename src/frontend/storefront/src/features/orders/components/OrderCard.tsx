import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import styles from './OrderCard.module.css';

interface OrderItemSummary {
  productName: string;
}

interface OrderCardProps {
  order: {
    id: string;
    orderNumber: string;
    status: string;
    totalAmount: number;
    createdAt: string;
    items: OrderItemSummary[];
  };
}

/**
 * OrderCard Component
 * 
 * Displays a summary card for a single order in the order history.
 * Shows order number, status, total amount, and a preview of items.
 */
export default function OrderCard({ order }: OrderCardProps) {
  const { t } = useTranslation();
  
  const orderDate = new Date(order.createdAt);
  const formattedDate = orderDate.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });

  // Status styling
  const getStatusClass = (status: string) => {
    const statusLower = status.toLowerCase();
    return styles[`status${status[0].toUpperCase() + statusLower.slice(1)}`] || styles.statusPending;
  };

  // Item count formatting
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
        <div className={`${styles.status} ${getStatusClass(order.status)}`}>
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
            ${order.totalAmount.toFixed(2)}
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
