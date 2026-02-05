import Card from '../../../components/ui/Card';
import Button from '../../../components/ui/Button';
import styles from './OrderHeader.module.css';

interface OrderHeaderProps {
  orderNumber: string;
  createdAt: string;
  status: string;
  canCancel: boolean;
  isCancelling: boolean;
  onCancel: () => void;
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

export default function OrderHeader({
  orderNumber,
  createdAt,
  status,
  canCancel,
  isCancelling,
  onCancel,
}: OrderHeaderProps) {
  return (
    <Card variant="elevated" padding="lg">
      <div className={styles.grid}>
        <div>
          <p className={styles.label}>Order Number</p>
          <p className={styles.orderNumber}>{orderNumber}</p>
        </div>

        <div>
          <p className={styles.label}>Date</p>
          <p className={styles.value}>
            {new Date(createdAt).toLocaleDateString()}{' '}
            {new Date(createdAt).toLocaleTimeString()}
          </p>
        </div>

        <div>
          <p className={styles.label}>Status</p>
          <p className={styles.status} style={{ color: getStatusColor(status) }}>
            {status}
          </p>
        </div>

        {canCancel && (
          <div>
            <Button variant="secondary" onClick={onCancel} disabled={isCancelling}>
              Cancel Order
            </Button>
          </div>
        )}
      </div>
    </Card>
  );
}
