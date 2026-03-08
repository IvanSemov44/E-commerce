import { useTranslation } from 'react-i18next';
import { ShieldIcon, TruckIcon, RefreshIcon, LockIcon, CheckIcon } from '@/shared/components/icons';
import styles from './TrustSignals.module.css';

interface TrustSignal {
  icon: React.ReactNode;
  title: string;
  description: string;
  badge?: string;
}

interface TrustSignalsProps {
  variant?: 'full' | 'compact';
}

export default function TrustSignals({ variant = 'full' }: TrustSignalsProps) {
  const { t } = useTranslation();

  const trustSignals: TrustSignal[] = [
    {
      icon: <LockIcon />,
      title: t('trustSignals.secureCheckout'),
      description: t('trustSignals.secureCheckoutDesc'),
      badge: t('trustSignals.sslSecured'),
    },
    {
      icon: <TruckIcon />,
      title: t('trustSignals.freeShipping'),
      description: t('trustSignals.freeShippingDesc'),
      badge: t('trustSignals.free'),
    },
    {
      icon: <RefreshIcon />,
      title: t('trustSignals.returns'),
      description: t('trustSignals.returnsDesc'),
      badge: t('trustSignals.easy'),
    },
    {
      icon: <ShieldIcon />,
      title: t('trustSignals.buyerProtection'),
      description: t('trustSignals.buyerProtectionDesc'),
      badge: t('trustSignals.guaranteed'),
    },
  ];

  if (variant === 'compact') {
    return (
      <div className={styles.compact}>
        {trustSignals.map((signal, index) => (
          <div key={index} className={styles.compactItem}>
            <span className={styles.compactIcon}>
              <CheckIcon />
            </span>
            <span className={styles.compactText}>{signal.title}</span>
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.shieldBadge}>
          <ShieldIcon />
        </div>
        <div className={styles.headerText}>
          <h3 className={styles.headerTitle}>{t('trustSignals.title')}</h3>
          <p className={styles.headerSubtitle}>{t('trustSignals.subtitle')}</p>
        </div>
      </div>

      <div className={styles.signals}>
        {trustSignals.map((signal, index) => (
          <div key={index} className={styles.signal}>
            <div className={styles.iconWrapper}>{signal.icon}</div>
            <div className={styles.content}>
              <div className={styles.titleRow}>
                <h4 className={styles.title}>{signal.title}</h4>
                {signal.badge && <span className={styles.badge}>{signal.badge}</span>}
              </div>
              <p className={styles.description}>{signal.description}</p>
            </div>
          </div>
        ))}
      </div>

      <div className={styles.footer}>
        <div className={styles.certificationBadges}>
          <div className={styles.certBadge}>
            <LockIcon />
            <span>{t('trustSignals.ssl')}</span>
          </div>
          <div className={styles.certBadge}>
            <ShieldIcon />
            <span>{t('trustSignals.pciDss')}</span>
          </div>
          <div className={styles.certBadge}>
            <ShieldIcon />
            <span>{t('trustSignals.verified')}</span>
          </div>
        </div>
      </div>
    </div>
  );
}
