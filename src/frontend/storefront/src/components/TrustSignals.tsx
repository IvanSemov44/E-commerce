import styles from './TrustSignals.module.css';

interface TrustSignal {
  icon: string;
  title: string;
  description: string;
}

interface TrustSignalsProps {
  variant?: 'full' | 'compact';
}

const trustSignals: TrustSignal[] = [
  {
    icon: '-lock',
    title: 'Secure Checkout',
    description: 'SSL encrypted payment',
  },
  {
    icon: 'truck',
    title: 'Free Shipping',
    description: 'On orders over $50',
  },
  {
    icon: 'refresh',
    title: '30-Day Returns',
    description: 'Hassle-free returns',
  },
  {
    icon: 'shield',
    title: 'Buyer Protection',
    description: 'Full money-back guarantee',
  },
];

export default function TrustSignals({ variant = 'full' }: TrustSignalsProps) {
  if (variant === 'compact') {
    return (
      <div className={styles.compact}>
        {trustSignals.map((signal, index) => (
          <div key={index} className={styles.compactItem}>
            <span className={styles.compactIcon}>{"\u2713"}</span>
            <span className={styles.compactText}>{signal.title}</span>
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {trustSignals.map((signal, index) => (
        <div key={index} className={styles.signal}>
          <div className={styles.iconWrapper}>
            <span className={styles.icon}>
              {signal.icon === '-lock' && 'lock'}
              {signal.icon === 'truck' && 'truck'}
              {signal.icon === 'refresh' && 'refresh'}
              {signal.icon === 'shield' && 'shield'}
            </span>
          </div>
          <div className={styles.content}>
            <h4 className={styles.title}>{signal.title}</h4>
            <p className={styles.description}>{signal.description}</p>
          </div>
        </div>
      ))}
    </div>
  );
}