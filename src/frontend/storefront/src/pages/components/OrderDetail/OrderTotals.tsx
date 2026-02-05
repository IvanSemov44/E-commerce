import Card from '../../../components/ui/Card';
import styles from './OrderTotals.module.css';

interface OrderTotalsProps {
  subtotal?: number;
  discountAmount?: number;
  shippingAmount?: number;
  taxAmount?: number;
  totalAmount?: number;
}

export default function OrderTotals({
  subtotal,
  discountAmount,
  shippingAmount,
  taxAmount,
  totalAmount,
}: OrderTotalsProps) {
  return (
    <Card variant="elevated" padding="lg">
      <div className={styles.container}>
        <div />
        <div className={styles.totalsSection}>
          <div className={styles.totalLine}>
            <span>Subtotal:</span>
            <span>${subtotal?.toFixed(2) || '0.00'}</span>
          </div>

          {discountAmount && discountAmount > 0 && (
            <div className={`${styles.totalLine} ${styles.discount}`}>
              <span>Discount:</span>
              <span>-${discountAmount.toFixed(2)}</span>
            </div>
          )}

          <div className={styles.totalLine}>
            <span>Shipping:</span>
            <span>${shippingAmount?.toFixed(2) || '0.00'}</span>
          </div>

          <div className={styles.totalLine}>
            <span>Tax:</span>
            <span>${taxAmount?.toFixed(2) || '0.00'}</span>
          </div>

          <div className={styles.grandTotal}>
            <span>Total:</span>
            <span>${totalAmount?.toFixed(2) || '0.00'}</span>
          </div>
        </div>
      </div>
    </Card>
  );
}
