import Button from './Button';
import { HeartIcon, ShoppingCartIcon, PackageIcon, SearchIcon, ErrorIcon } from '../icons';
import styles from './EmptyState.module.css';

interface EmptyStateProps {
  icon: 'cart' | 'wishlist' | 'orders' | 'search' | 'error';
  title: string;
  description: string;
  actionLabel?: string;
  onAction?: () => void;
  secondaryActionLabel?: string;
  onSecondaryAction?: () => void;
}

const iconMap = {
  cart: ShoppingCartIcon,
  wishlist: HeartIcon,
  orders: PackageIcon,
  search: SearchIcon,
  error: ErrorIcon,
};

export default function EmptyState({
  icon,
  title,
  description,
  actionLabel,
  onAction,
  secondaryActionLabel,
  onSecondaryAction,
}: EmptyStateProps) {
  const IconComponent = iconMap[icon];

  return (
    <div className={styles.container}>
      <div className={styles.iconWrapper}>
        <IconComponent className={styles.icon} />
      </div>
      <h3 className={styles.title}>{title}</h3>
      <p className={styles.description}>{description}</p>
      <div className={styles.actions}>
        {actionLabel && onAction && (
          <Button variant="primary" onClick={onAction}>
            {actionLabel}
          </Button>
        )}
        {secondaryActionLabel && onSecondaryAction && (
          <Button variant="outline" onClick={onSecondaryAction}>
            {secondaryActionLabel}
          </Button>
        )}
      </div>
    </div>
  );
}

// Preset Empty States
export function EmptyCart({ onBrowse }: { onBrowse?: () => void }) {
  return (
    <EmptyState
      icon="cart"
      title="Your cart is empty"
      description="Looks like you haven't added anything to your cart yet. Start shopping to fill it up!"
      actionLabel="Browse Products"
      onAction={onBrowse}
    />
  );
}

export function EmptyWishlist({ onBrowse }: { onBrowse?: () => void }) {
  return (
    <EmptyState
      icon="wishlist"
      title="Your wishlist is empty"
      description="Save items you love by clicking the heart icon on any product."
      actionLabel="Discover Products"
      onAction={onBrowse}
    />
  );
}

export function EmptyOrders({ onBrowse }: { onBrowse?: () => void }) {
  return (
    <EmptyState
      icon="orders"
      title="No orders yet"
      description="You haven't placed any orders yet. Start shopping to see your orders here."
      actionLabel="Start Shopping"
      onAction={onBrowse}
    />
  );
}

export function NoSearchResults({ 
  query, 
  onClear,
  onBrowse 
}: { 
  query?: string; 
  onClear?: () => void;
  onBrowse?: () => void;
}) {
  return (
    <EmptyState
      icon="search"
      title="No results found"
      description={query 
        ? `We couldn't find any products matching "${query}". Try different keywords or browse our categories.`
        : "We couldn't find any products matching your search. Try different keywords."
      }
      actionLabel="Clear Filters"
      onAction={onClear}
      secondaryActionLabel="Browse All"
      onSecondaryAction={onBrowse}
    />
  );
}

export function ErrorState({ 
  message, 
  onRetry,
  onContact 
}: { 
  message?: string; 
  onRetry?: () => void;
  onContact?: () => void;
}) {
  return (
    <EmptyState
      icon="error"
      title="Something went wrong"
      description={message || "We encountered an unexpected error. Please try again or contact support if the problem persists."}
      actionLabel="Try Again"
      onAction={onRetry}
      secondaryActionLabel="Contact Support"
      onSecondaryAction={onContact}
    />
  );
}
