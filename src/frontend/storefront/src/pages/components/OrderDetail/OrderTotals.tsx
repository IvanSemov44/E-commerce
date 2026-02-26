import Card from '../../../components/ui/Card';
import styles from './OrderTotals.module.css';

interface OrderTotalsProps {
  subtotal?: number;
  discountAmount?: number;
  shippingAmount?: number;
  taxAmount?: number;
  totalAmount?: number;
}

// Icons
const ReceiptIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.titleIcon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2zM10 8.5a.5.5 0 11-1 0 .5.5 0 011 0zm5 5a.5.5 0 11-1 0 .5.5 0 011 0z" />
  </svg>
);

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
        <h2 className={styles.title}>
          <ReceiptIcon />
          Order Summary
        </h2>
        <div className={styles.totalsSection}>
          <div className={styles.totalLine}>
            <span>Subtotal</span>
            <span>${subtotal?.toFixed(2) || '0.00'}</span>
          </div>

          {discountAmount && discountAmount > 0 && (
            <div className={`${styles.totalLine} ${styles.discount}`}>
              <span>
                Discount
                <span className={styles.discountBadge}>Saved</span>
              </span>
              <span>-${discountAmount.toFixed(2)}</span>
            </div>
          )}

          <div className={styles.totalLine}>
            <span>Shipping</span>
            <span>${shippingAmount?.toFixed(2) || '0.00'}</span>
          </div>

          <div className={styles.totalLine}>
            <span>Tax</span>
            <span>${taxAmount?.toFixed(2) || '0.00'}</span>
          </div>

          <div className={styles.grandTotal}>
            <span>Total</span>
            <span>${totalAmount?.toFixed(2) || '0.00'}</span>
          </div>
        </div>
      </div>
    </Card>
  );
}
