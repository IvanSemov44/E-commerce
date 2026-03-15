import { Button } from '@/shared/components/ui/Button';
import ErrorAlert from '@/shared/components/ErrorAlert';
import type { ProductActionsProps } from './ProductActions.types';
import {
  isInStock,
  isStockLow,
  getStockStatusMessage,
  getAddToCartButtonText,
  getWishlistButtonText,
} from './ProductActions.utils';
import styles from './ProductActions.module.css';

export function ProductActions({
  stockQuantity,
  lowStockThreshold,
  isAuthenticated,
  cart,
  wishlist,
  onQuantityChange,
  onAddToCart,
  onToggleWishlist,
  onDismissError,
}: ProductActionsProps) {
  const {
    quantity,
    cartItem,
    addedToCart,
    isLoading: addingToCartBackend,
    error: cartError,
  } = cart;
  const { isInWishlist, isAdding: addingToWishlist, isRemoving: removingFromWishlist } = wishlist;

  return (
    <div className={styles.actions}>
      <div className={styles.stockSection}>
        <p
          className={`${styles.stockLabel} ${isInStock(stockQuantity) ? styles.inStock : styles.outOfStock}`}
        >
          {getStockStatusMessage(stockQuantity)}
        </p>
        {isStockLow(stockQuantity, lowStockThreshold) && (
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
          disabled={!isInStock(stockQuantity) || addedToCart || addingToCartBackend}
          size="lg"
        >
          {getAddToCartButtonText(stockQuantity, addedToCart)}
        </Button>

        {isAuthenticated && (
          <Button
            variant={isInWishlist ? 'primary' : 'secondary'}
            size="lg"
            onClick={onToggleWishlist}
            disabled={addingToWishlist || removingFromWishlist}
          >
            {getWishlistButtonText(isInWishlist)}
          </Button>
        )}
      </div>
    </div>
  );
}
