import Button from '@/shared/components/ui/Button';
import ErrorAlert from '@/shared/components/ErrorAlert';
import styles from './ProductActions.module.css';

interface CartItem {
  quantity: number;
}

interface ProductActionsProps {
  stockQuantity: number;
  lowStockThreshold: number;
  quantity: number;
  cartItem: CartItem | undefined;
  addedToCart: boolean;
  addingToCartBackend: boolean;
  cartError: string | null;
  isAuthenticated: boolean;
  isInWishlist: boolean | undefined;
  addingToWishlist: boolean;
  removingFromWishlist: boolean;
  onQuantityChange: (quantity: number) => void;
  onAddToCart: () => void;
  onToggleWishlist: () => void;
  onDismissError: () => void;
}

function getAddToCartLabel(stockQuantity: number, addedToCart: boolean): string {
  if (stockQuantity === 0) return 'Out of Stock';
  if (addedToCart) return '✓ Added to Cart!';
  return 'Add to Cart';
}

export default function ProductActions({
  stockQuantity,
  lowStockThreshold,
  quantity,
  cartItem,
  addedToCart,
  addingToCartBackend,
  cartError,
  isAuthenticated,
  isInWishlist,
  addingToWishlist,
  removingFromWishlist,
  onQuantityChange,
  onAddToCart,
  onToggleWishlist,
  onDismissError,
}: ProductActionsProps) {
  return (
    <div className={styles.actions}>
      <div className={styles.stockSection}>
        <p
          className={`${styles.stockLabel} ${stockQuantity > 0 ? styles.inStock : styles.outOfStock}`}
        >
          {stockQuantity > 0 ? `${stockQuantity} in stock` : 'Out of stock'}
        </p>
        {stockQuantity > 0 && stockQuantity <= lowStockThreshold && (
          <p className={styles.lowStockWarning}>⚠ Only {stockQuantity} left!</p>
        )}
      </div>

      <div className={styles.quantitySection}>
        <label className={styles.quantityLabel}>Quantity:</label>
        <div className={styles.quantityControls}>
          <div className={styles.quantityButtonGroup}>
            <button
              onClick={() => onQuantityChange(Math.max(1, quantity - 1))}
              className={styles.quantityButton}
            >
              −
            </button>
            <input
              type="number"
              value={quantity}
              onChange={(e) => {
                const val = parseInt(e.target.value) || 1;
                onQuantityChange(Math.min(stockQuantity, Math.max(1, val)));
              }}
              className={styles.quantityInput}
              min="1"
              max={stockQuantity}
            />
            <button
              onClick={() => onQuantityChange(Math.min(stockQuantity, quantity + 1))}
              disabled={quantity >= stockQuantity}
              className={styles.quantityButton}
            >
              +
            </button>
          </div>
          {cartItem && <span className={styles.cartHint}>({cartItem.quantity} in cart)</span>}
        </div>
      </div>

      {cartError && (
        <div className={styles.errorContainer}>
          <ErrorAlert message={cartError} onDismiss={onDismissError} />
        </div>
      )}

      <div className={styles.buttonGroup}>
        <Button
          onClick={onAddToCart}
          disabled={stockQuantity === 0 || addedToCart || addingToCartBackend}
          size="lg"
        >
          {getAddToCartLabel(stockQuantity, addedToCart)}
        </Button>

        {isAuthenticated && (
          <Button
            variant={isInWishlist ? 'primary' : 'secondary'}
            size="lg"
            onClick={onToggleWishlist}
            disabled={addingToWishlist || removingFromWishlist}
          >
            {isInWishlist ? '♥ In Wishlist' : '♡ Add to Wishlist'}
          </Button>
        )}
      </div>
    </div>
  );
}
