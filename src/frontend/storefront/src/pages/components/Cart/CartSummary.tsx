import { Link } from 'react-router-dom';
import Card from '../../../components/ui/Card';
import Button from '../../../components/ui/Button';
import styles from './CartSummary.module.css';

interface CartSummaryProps {
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
  freeShippingThreshold: number;
}

export default function CartSummary({
  subtotal,
  shipping,
  tax,
  total,
  freeShippingThreshold,
}: CartSummaryProps) {
  const freeShippingRemaining = freeShippingThreshold - subtotal;
  const showFreeShippingMessage = subtotal > 50 && freeShippingRemaining > 0;

  return (
    <Card variant="elevated" padding="lg">
      <h2 className={styles.title}>Order Summary</h2>
      
      <div className={styles.totalsSection}>
        <div className={styles.totalLine}>
          <span>Subtotal:</span>
          <span className={styles.totalValue}>${subtotal.toFixed(2)}</span>
        </div>
        
        <div className={styles.totalLine}>
          <span>Shipping:</span>
          <span className={styles.totalValue}>
            {shipping === 0 ? 'FREE' : `$${shipping.toFixed(2)}`}
          </span>
        </div>
        
        {showFreeShippingMessage && (
          <div className={styles.shippingMessage}>
            Add ${freeShippingRemaining.toFixed(2)} more for free shipping!
          </div>
        )}
        
        <div className={styles.totalLine}>
          <span>Tax (8%):</span>
          <span className={styles.totalValue}>${tax.toFixed(2)}</span>
        </div>
      </div>
      
      <div className={styles.grandTotal}>
        <span>Total:</span>
        <span className={styles.grandTotalAmount}>${total.toFixed(2)}</span>
      </div>
      
      <div className={styles.actions}>
        <Link to="/checkout" className={styles.actionLink}>
          <Button size="lg">Proceed to Checkout</Button>
        </Link>
        <Link to="/products" className={styles.actionLink}>
          <Button variant="secondary" size="lg">Continue Shopping</Button>
        </Link>
      </div>
    </Card>
  );
}
