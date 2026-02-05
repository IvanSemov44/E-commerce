import { Link } from 'react-router-dom';
import Card from '../../../components/ui/Card';
import Button from '../../../components/ui/Button';
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

const getStatusColor = (status: string) => {
  switch (status) {
    case 'Pending':
      return '#ff9800';
    case 'Processing':
      return '#2196f3';
    case 'Shipped':
      return '#9c27b0';
    case 'Delivered':
      return '#4caf50';
    case 'Cancelled':
      return '#f44336';
    default:
      return '#666';
  }
};

export default function OrderCard({ order }: OrderCardProps) {
  return (
    <Link to={`/orders/${order.id}`} className={styles.cardLink}>
      <Card variant="elevated" padding="lg" className={styles.card}>
        <div className={styles.grid}>
          <div>
            <p className={styles.label}>Order Number</p>
            <p className={styles.orderNumber}>{order.orderNumber}</p>
          </div>

          <div>
            <p className={styles.label}>Date</p>
            <p className={styles.value}>
              {new Date(order.createdAt).toLocaleDateString()}
            </p>
          </div>

          <div>
            <p className={styles.label}>Status</p>
            <p className={styles.status} style={{ color: getStatusColor(order.status) }}>
              {order.status}
            </p>
          </div>

          <div>
            <p className={styles.label}>Total</p>
            <p className={styles.total}>${order.totalAmount.toFixed(2)}</p>
          </div>

          <div>
            <p className={styles.label}>Items</p>
            <p className={styles.value}>
              {order.items.length} item{order.items.length !== 1 ? 's' : ''}
            </p>
          </div>

          <div className={styles.actions}>
            <Button variant="secondary" size="sm">
              View Details
            </Button>
          </div>
        </div>
      </Card>
    </Link>
  );
}
