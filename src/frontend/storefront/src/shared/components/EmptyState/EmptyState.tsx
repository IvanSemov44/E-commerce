import type { ReactNode } from 'react';
import Card from '../ui/Card';
import styles from './EmptyState.module.css';

interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}

export default function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <Card variant="bordered" padding="lg">
      {icon && (
        <div className={styles.iconContainer}>
          {icon}
        </div>
      )}
      <h3 className={styles.title}>{title}</h3>
      {description && (
        <p className={styles.description}>{description}</p>
      )}
      {action && <div className={styles.actionContainer}>{action}</div>}
    </Card>
  );
}
