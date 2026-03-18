import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button } from '@/shared/components/ui/Button';
import { CheckIcon } from '@/shared/components/icons';
import styles from './OrderSuccess.module.css';

interface OrderSuccessProps {
  orderNumber: string;
  email: string;
  isGuestOrder: boolean;
}

export function OrderSuccess({ orderNumber, email, isGuestOrder }: OrderSuccessProps) {
  const { t } = useTranslation();

  return (
    <div className={styles.container}>
      <div className={styles.iconWrapper}>
        <CheckIcon className={styles.icon} />
      </div>

      <h2 className={styles.title}>{t('checkout.orderSuccess')}</h2>
      <p className={styles.subtitle}>{t('checkout.confirmationEmailSent', { email })}</p>

      <p className={styles.orderNumber}>
        {t('checkout.orderNumber')}: <span className={styles.orderNumberValue}>{orderNumber}</span>
      </p>

      {isGuestOrder && (
        <div className={styles.guestCta}>
          <h3 className={styles.guestCtaTitle}>{t('checkout.createAccount')}</h3>
          <p className={styles.guestCtaText}>{t('checkout.registerToTrack')}</p>
          <Link to={ROUTE_PATHS.register}>
            <Button variant="outline">{t('auth.signUp')}</Button>
          </Link>
        </div>
      )}

      <div className={styles.actions}>
        <Link to={ROUTE_PATHS.orders}>
          <Button variant="outline">{t('orders.viewOrders')}</Button>
        </Link>
        <Link to={ROUTE_PATHS.products}>
          <Button>{t('checkout.continueShopping')}</Button>
        </Link>
      </div>
    </div>
  );
}
