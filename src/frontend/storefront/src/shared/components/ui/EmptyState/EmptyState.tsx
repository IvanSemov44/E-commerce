import type { ReactNode } from 'react';
import {
  ShoppingCartIcon,
  HeartIcon,
  PackageIcon,
  SearchIcon,
  ErrorIcon,
} from '@/shared/components/icons';
import styles from './EmptyState.module.css';

type EmptyStateIcon = 'cart' | 'wishlist' | 'orders' | 'search' | 'error';

export interface EmptyStateProps {
  icon?: EmptyStateIcon | ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}

function getPresetIcon(icon: EmptyStateIcon): ReactNode {
  switch (icon) {
    case 'cart':
      return <ShoppingCartIcon className={styles.icon} />;
    case 'wishlist':
      return <HeartIcon className={styles.icon} />;
    case 'orders':
      return <PackageIcon className={styles.icon} />;
    case 'search':
      return <SearchIcon className={styles.icon} />;
    case 'error':
      return <ErrorIcon className={styles.icon} />;
    default:
      return null;
  }
}

export default function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  const renderedIcon =
    typeof icon === 'string' ? getPresetIcon(icon as EmptyStateIcon) : (icon ?? null);

  return (
    <div className={styles.container} data-testid="empty-state">
      {renderedIcon && <div className={styles.iconWrapper}>{renderedIcon}</div>}
      <h2 className={styles.title}>{title}</h2>
      {description && <p className={styles.description}>{description}</p>}
      {action && <div className={styles.action}>{action}</div>}
    </div>
  );
}
