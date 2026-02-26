import { Link } from 'react-router-dom';
import Button from '../../../components/ui/Button';
import Card from '../../../components/ui/Card';
import { CheckIcon } from '../../../components/icons';
import styles from './OrderSuccess.module.css';

interface OrderSuccessProps {
  orderNumber: string;
  email: string;
  isGuestOrder?: boolean;
}

export default function OrderSuccess({ orderNumber, email, isGuestOrder }: OrderSuccessProps) {
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
          
          {isGuestOrder && (
            <div className={styles.guestPrompt}>
              <h3 className={styles.guestPromptTitle}>Create an Account</h3>
              <p className={styles.guestPromptText}>
                Create an account to track your orders, save your information for faster checkout, 
                and receive exclusive offers.
              </p>
              <Link to="/register" className={styles.guestPromptLink}>
                <Button variant="primary" size="md">
                  Create Account
                </Button>
              </Link>
            </div>
          )}
          
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
