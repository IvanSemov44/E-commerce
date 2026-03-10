import type { ReactNode } from 'react';
import Button from '../Button';
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
  actionLabel?: string;
  onAction?: () => void;
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

export default function EmptyState({
  icon,
  title,
  description,
  actionLabel,
  onAction,
}: EmptyStateProps) {
  const renderedIcon =
    typeof icon === 'string' ? getPresetIcon(icon as EmptyStateIcon) : (icon ?? null);

  return (
    <div className={styles.container}>
      {renderedIcon}
      <h2 className={styles.title}>{title}</h2>
      {description && <p className={styles.description}>{description}</p>}
      {actionLabel && onAction && (
        <div className={styles.action}>
          <Button onClick={onAction}>{actionLabel}</Button>
        </div>
      )}
    </div>
  );
}

export function EmptyCart({
  actionLabel,
  onAction,
}: Pick<EmptyStateProps, 'actionLabel' | 'onAction'>) {
  return (
    <EmptyState
      icon="cart"
      title="Your cart is empty"
      description="Looks like you have not added anything to your cart yet."
      actionLabel={actionLabel}
      onAction={onAction}
    />
  );
}

export function EmptyWishlist({
  actionLabel,
  onAction,
}: Pick<EmptyStateProps, 'actionLabel' | 'onAction'>) {
  return (
    <EmptyState
      icon="wishlist"
      title="Your wishlist is empty"
      description="Save items you love so you can find them later."
      actionLabel={actionLabel}
      onAction={onAction}
    />
  );
}

export function EmptyOrders({
  actionLabel,
  onAction,
}: Pick<EmptyStateProps, 'actionLabel' | 'onAction'>) {
  return (
    <EmptyState
      icon="orders"
      title="No orders yet"
      description="When you place your first order, it will appear here."
      actionLabel={actionLabel}
      onAction={onAction}
    />
  );
}

export function NoSearchResults({
  actionLabel,
  onAction,
}: Pick<EmptyStateProps, 'actionLabel' | 'onAction'>) {
  return (
    <EmptyState
      icon="search"
      title="No results found"
      description="Try a different query or remove some filters."
      actionLabel={actionLabel}
      onAction={onAction}
    />
  );
}

export function ErrorState({
  title = 'Something went wrong',
  description = 'Please try again.',
  actionLabel,
  onAction,
}: Omit<EmptyStateProps, 'icon'>) {
  return (
    <EmptyState
      icon="error"
      title={title}
      description={description}
      actionLabel={actionLabel}
      onAction={onAction}
    />
  );
}
