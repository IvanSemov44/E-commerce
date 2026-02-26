import styles from './OrderStatusTimeline.module.css';

interface OrderStatusTimelineProps {
  status: string;
}

type OrderStatus = 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';

const STATUS_STEPS: { key: OrderStatus; label: string; icon: string }[] = [
  { key: 'Pending', label: 'Pending', icon: '🕐' },
  { key: 'Processing', label: 'Processing', icon: '📦' },
  { key: 'Shipped', label: 'Shipped', icon: '🚚' },
  { key: 'Delivered', label: 'Delivered', icon: '✓' },
];

export default function OrderStatusTimeline({ status }: OrderStatusTimelineProps) {
  // Handle cancelled orders separately
  if (status === 'Cancelled') {
    return (
      <div className={styles.container}>
        <div className={styles.cancelledStatus}>
          <span className={styles.cancelledIcon}>✕</span>
          <span className={styles.cancelledText}>Order Cancelled</span>
        </div>
      </div>
    );
  }

  const currentStepIndex = STATUS_STEPS.findIndex((step) => step.key === status);

  return (
    <div className={styles.container}>
      <div className={styles.timeline}>
        {STATUS_STEPS.map((step, index) => {
          const isCompleted = index <= currentStepIndex;
          const isCurrent = index === currentStepIndex;
          
          return (
            <div key={step.key} className={styles.step}>
              <div
                className={`${styles.stepCircle} ${
                  isCompleted ? styles.completed : ''
                } ${isCurrent ? styles.current : ''}`}
              >
                <span className={styles.stepIcon}>{step.icon}</span>
              </div>
              <span
                className={`${styles.stepLabel} ${
                  isCompleted ? styles.labelCompleted : ''
                } ${isCurrent ? styles.labelCurrent : ''}`}
              >
                {step.label}
              </span>
              {index < STATUS_STEPS.length - 1 && (
                <div
                  className={`${styles.connector} ${
                    index < currentStepIndex ? styles.connectorCompleted : ''
                  }`}
                />
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}