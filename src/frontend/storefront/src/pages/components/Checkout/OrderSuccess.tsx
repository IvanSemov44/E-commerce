import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
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
  const { t } = useTranslation();
  
  return (
    <div className={styles.container}>
      <div className={styles.successContent}>
        <Card variant="elevated" padding="lg">
          <div className={styles.successIcon}>
            <CheckIcon className={styles.successIconSvg} />
          </div>
          <h1 className={styles.successTitle}>{t('checkout.orderSuccess')}</h1>
          <p className={styles.successMessage}>{t('checkout.thankYou')}</p>
          <p className={styles.successOrderNumber}>{t('checkout.orderNumber')}: {orderNumber}</p>
          <p className={styles.successEmail}>
            {t('checkout.confirmationEmailSent', { email: email || 'your email' })}
          </p>
          
          {isGuestOrder && (
            <div className={styles.guestPrompt}>
              <h3 className={styles.guestPromptTitle}>{t('checkout.createAccount')}</h3>
              <p className={styles.guestPromptText}>
                {t('checkout.registerToTrack')}
              </p>
              <Link to="/register" className={styles.guestPromptLink}>
                <Button variant="primary" size="md">
                  {t('auth.register')}
                </Button>
              </Link>
            </div>
          )}
          
          <div className={styles.successActions}>
            <Link to="/products" className={styles.successActionLink}>
              <Button size="lg">{t('checkout.continueShopping')}</Button>
            </Link>
            <Link to="/" className={styles.successActionLink}>
              <Button variant="secondary" size="lg">
                {t('nav.home')}
              </Button>
            </Link>
          </div>
        </Card>
      </div>
    </div>
  );
}
