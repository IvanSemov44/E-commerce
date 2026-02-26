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

// Modern SVG icons for trust signals
const ShieldIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
    <path d="M9 12l2 2 4-4"/>
  </svg>
);

const TruckIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="1" y="3" width="15" height="13"/>
    <polygon points="16 8 20 8 23 11 23 16 16 16 16 8"/>
    <circle cx="5.5" cy="18.5" r="2.5"/>
    <circle cx="18.5" cy="18.5" r="2.5"/>
  </svg>
);

const RefreshIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="23 4 23 10 17 10"/>
    <polyline points="1 20 1 14 7 14"/>
    <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
  </svg>
);

const LockIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
    <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
  </svg>
);

const CheckIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12"/>
  </svg>
);

const trustSignals: TrustSignal[] = [
  {
    icon: <LockIcon />,
    title: 'Secure Checkout',
    description: '256-bit SSL encryption protects your data',
    badge: 'SSL Secured',
  },
  {
    icon: <TruckIcon />,
    title: 'Free Shipping',
    description: 'On all orders over $50',
    badge: 'Free',
  },
  {
    icon: <RefreshIcon />,
    title: '30-Day Returns',
    description: 'No questions asked return policy',
    badge: 'Easy',
  },
  {
    icon: <ShieldIcon />,
    title: 'Buyer Protection',
    description: 'Full money-back guarantee on every order',
    badge: 'Guaranteed',
  },
];

export default function TrustSignals({ variant = 'full' }: TrustSignalsProps) {
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
          <h3 className={styles.headerTitle}>Shop with Confidence</h3>
          <p className={styles.headerSubtitle}>Your security and satisfaction are our priority</p>
        </div>
      </div>
      
      <div className={styles.signals}>
        {trustSignals.map((signal, index) => (
          <div key={index} className={styles.signal}>
            <div className={styles.iconWrapper}>
              {signal.icon}
            </div>
            <div className={styles.content}>
              <div className={styles.titleRow}>
                <h4 className={styles.title}>{signal.title}</h4>
                {signal.badge && (
                  <span className={styles.badge}>{signal.badge}</span>
                )}
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
            <span>SSL</span>
          </div>
          <div className={styles.certBadge}>
            <ShieldIcon />
            <span>PCI DSS</span>
          </div>
          <div className={styles.certBadge}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            </svg>
            <span>Verified</span>
          </div>
        </div>
      </div>
    </div>
  );
}
