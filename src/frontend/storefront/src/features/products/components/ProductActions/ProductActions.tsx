import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import ErrorAlert from '@/shared/components/ErrorAlert';
import type { ProductActionsProps } from './ProductActions.types';
import { isInStock, isStockLow } from './ProductActions.utils';
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
  const { t } = useTranslation();

  return (
    <div className={styles.actions}>
      <div className={styles.stockSection}>
        <p
          className={`${styles.stockLabel} ${isInStock(stockQuantity) ? styles.inStock : styles.outOfStock}`}
        >
          {stockQuantity === 0
            ? t('common.outOfStock')
            : t('products.inStockCount', { count: stockQuantity })}
        </p>
        {isStockLow(stockQuantity, lowStockThreshold) && (
          <p className={styles.lowStockWarning}>
            {t('products.lowStockWarning', { count: stockQuantity })}
          </p>
        )}
      </div>

      <div className={styles.quantitySection}>
        <label className={styles.quantityLabel}>{t('productDetail.quantity')}:</label>
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
          {cartItem && (
            <span className={styles.cartHint}>
              ({cartItem.quantity} {t('productDetail.inCart')})
            </span>
          )}
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
          {stockQuantity === 0
            ? t('common.outOfStock')
            : addedToCart
              ? t('products.addedToCartSuccess')
              : t('products.addToCart')}
        </Button>

        {isAuthenticated && (
          <Button
            variant={isInWishlist ? 'primary' : 'secondary'}
            size="lg"
            onClick={onToggleWishlist}
            disabled={addingToWishlist || removingFromWishlist}
          >
            {isInWishlist ? t('products.inWishlist') : t('products.addToWishlist')}
          </Button>
        )}
      </div>
    </div>
  );
}
