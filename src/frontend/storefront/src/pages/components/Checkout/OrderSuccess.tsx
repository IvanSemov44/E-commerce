import { Link } from 'react-router-dom';
import Button from '../../../components/ui/Button';
import Card from '../../../components/ui/Card';
import { CheckIcon } from '../../../components/icons';
import styles from './OrderSuccess.module.css';

interface OrderSuccessProps {
  orderNumber: string;
  email: string;
}

export default function OrderSuccess({ orderNumber, email }: OrderSuccessProps) {
  return (
    <div className={styles.container}>
      <div className={styles.successContent}>
        <Card variant="elevated" padding="lg">
          <div className={styles.successIcon}>
            <CheckIcon className={styles.successIconSvg} />
          </div>
          <h1 className={styles.successTitle}>Order Placed Successfully!</h1>
          <p className={styles.successMessage}>Thank you for your purchase.</p>
          <p className={styles.successOrderNumber}>Order Number: {orderNumber}</p>
          <p className={styles.successEmail}>
            A confirmation email has been sent to {email || 'your email'}
          </p>
          <div className={styles.successActions}>
            <Link to="/products" className={styles.successActionLink}>
              <Button size="lg">Continue Shopping</Button>
            </Link>
            <Link to="/" className={styles.successActionLink}>
              <Button variant="secondary" size="lg">
                Return Home
              </Button>
            </Link>
          </div>
        </Card>
      </div>
    </div>
  );
}
