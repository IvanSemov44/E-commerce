import Card from '../../../components/ui/Card';
import Button from '../../../components/ui/Button';
import OrderStatusTimeline from './OrderStatusTimeline';
import styles from './OrderHeader.module.css';

interface OrderHeaderProps {
  orderNumber: string;
  createdAt: string;
  status: string;
  canCancel: boolean;
  isCancelling: boolean;
  onCancel: () => void;
}

export default function OrderHeader({
  orderNumber,
  createdAt,
  status,
  canCancel,
  isCancelling,
  onCancel,
}: OrderHeaderProps) {
  const orderDate = new Date(createdAt);
  
  return (
    <Card variant="elevated" padding="lg">
      <div className={styles.grid}>
        <div className={styles.infoItem}>
          <p className={styles.label}>Order Number</p>
          <p className={styles.orderNumber}>#{orderNumber}</p>
        </div>

        <div className={styles.infoItem}>
          <p className={styles.label}>Order Date</p>
          <div className={styles.dateSection}>
            <p className={styles.dateValue}>{orderDate.toLocaleDateString()}</p>
            <p className={styles.timeValue}>{orderDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</p>
          </div>
        </div>

        {canCancel && (
          <div className={styles.cancelButton}>
            <Button variant="secondary" onClick={onCancel} disabled={isCancelling}>
              Cancel Order
            </Button>
          </div>
        )}
      </div>

      <OrderStatusTimeline status={status} />
    </Card>
  );
}
