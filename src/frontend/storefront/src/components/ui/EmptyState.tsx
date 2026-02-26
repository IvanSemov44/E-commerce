import Button from './Button';
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

// SVG Icons
const CartIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.icon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} 
      d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
  </svg>
);

const HeartIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.icon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} 
      d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
  </svg>
);

const PackageIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.icon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} 
      d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
  </svg>
);

const SearchIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.icon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} 
      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
  </svg>
);

const ErrorIcon = () => (
  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.icon}>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} 
      d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
  </svg>
);

const iconMap = {
  cart: CartIcon,
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
        <IconComponent />
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
