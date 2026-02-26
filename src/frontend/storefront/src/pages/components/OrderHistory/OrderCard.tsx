import { Link } from 'react-router-dom';
import Card from '../../../components/ui/Card';
import styles from './OrderCard.module.css';

interface Order {
  id: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  items: Array<{ productName: string }>;
}

interface OrderCardProps {
  order: Order;
}

// Icons
const ArrowRightIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
  </svg>
);

const getStatusClass = (status: string) => {
  switch (status.toLowerCase()) {
    case 'pending':
      return styles.pending;
    case 'processing':
      return styles.processing;
    case 'shipped':
      return styles.shipped;
    case 'delivered':
      return styles.delivered;
    case 'cancelled':
      return styles.cancelled;
    default:
      return '';
  }
};

export default function OrderCard({ order }: OrderCardProps) {
  const orderDate = new Date(order.createdAt);
  
  return (
    <Link to={`/orders/${order.id}`} className={styles.cardLink}>
      <Card variant="elevated" padding="none" className={styles.card}>
        <div className={styles.grid}>
          <div className={styles.infoItem}>
            <p className={styles.label}>Order Number</p>
            <p className={styles.orderNumber}>#{order.orderNumber}</p>
          </div>

          <div className={styles.infoItem}>
            <p className={styles.label}>Date</p>
            <div className={styles.dateSection}>
              <p className={styles.dateValue}>{orderDate.toLocaleDateString()}</p>
              <p className={styles.timeValue}>{orderDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</p>
            </div>
          </div>

          <div className={styles.infoItem}>
            <p className={styles.label}>Status</p>
            <p className={`${styles.status} ${getStatusClass(order.status)}`}>
              {order.status}
            </p>
          </div>

          <div className={styles.infoItem}>
            <p className={styles.label}>Total</p>
            <p className={styles.total}>${order.totalAmount.toFixed(2)}</p>
          </div>

          <div className={styles.infoItem}>
            <p className={styles.label}>Items</p>
            <div className={styles.itemsPreview}>
              <span className={styles.itemsCount}>
                {order.items.length} item{order.items.length !== 1 ? 's' : ''}
              </span>
            </div>
          </div>

          <div className={styles.actions}>
            <button className={styles.viewButton}>
              View Details
              <ArrowRightIcon />
            </button>
          </div>
        </div>
      </Card>
    </Link>
  );
}
