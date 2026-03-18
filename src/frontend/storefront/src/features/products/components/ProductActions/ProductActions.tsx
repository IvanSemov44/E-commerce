import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import { ErrorAlert } from '@/shared/components/ErrorAlert';
import { useAppSelector } from '@/shared/lib/store';
import { selectCartItemById } from '@/features/cart/slices/cartSlice';
import type { ProductActionsProps } from './ProductActions.types';
import { useCartActions, useWishlistActions } from './ProductActions.hooks';
import { isInStock, isStockLow } from './ProductActions.utils';
import styles from './ProductActions.module.css';

export function ProductActions({ product }: ProductActionsProps) {
  const { t } = useTranslation();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const cartItem = useAppSelector(selectCartItemById(product.id));

  const cart = useCartActions(product);
  const wishlist = useWishlistActions(product.id);

  return (
    <div className={styles.actions}>
      <div className={styles.stockSection}>
        <p
          className={`${styles.stockLabel} ${isInStock(product.stockQuantity) ? styles.inStock : styles.outOfStock}`}
        >
          {product.stockQuantity === 0
            ? t('common.outOfStock')
            : t('products.inStockCount', { count: product.stockQuantity })}
        </p>
        {isStockLow(product.stockQuantity, product.lowStockThreshold) && (
          <p className={styles.lowStockWarning}>
            {t('products.lowStockWarning', { count: product.stockQuantity })}
          </p>
        )}
      </div>

      <div className={styles.quantitySection}>
        <label className={styles.quantityLabel}>{t('productDetail.quantity')}:</label>
        <div className={styles.quantityControls}>
          <div className={styles.quantityButtonGroup}>
            <button
              onClick={() => cart.setQuantity(Math.max(1, cart.quantity - 1))}
              className={styles.quantityButton}
            >
              −
            </button>
            <input
              type="number"
              value={cart.quantity}
              onChange={(e) => {
                const val = parseInt(e.target.value) || 1;
                cart.setQuantity(Math.min(product.stockQuantity, Math.max(1, val)));
              }}
              className={styles.quantityInput}
              min="1"
              max={product.stockQuantity}
            />
            <button
              onClick={() => cart.setQuantity(Math.min(product.stockQuantity, cart.quantity + 1))}
              disabled={cart.quantity >= product.stockQuantity}
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

      {cart.cartError && (
        <div className={styles.errorContainer}>
          <ErrorAlert message={cart.cartError} onDismiss={cart.dismissCartError} />
        </div>
      )}

      <div className={styles.buttonGroup}>
        <Button
          onClick={cart.addToCart}
          disabled={!isInStock(product.stockQuantity) || cart.addedToCart || cart.isAdding}
          size="lg"
        >
          {product.stockQuantity === 0
            ? t('common.outOfStock')
            : cart.addedToCart
              ? t('products.addedToCartSuccess')
              : t('products.addToCart')}
        </Button>

        {isAuthenticated && (
          <Button
            variant={wishlist.isInWishlist ? 'primary' : 'secondary'}
            size="lg"
            onClick={wishlist.toggleWishlist}
            disabled={wishlist.isAdding || wishlist.isRemoving}
          >
            {wishlist.isInWishlist ? t('products.inWishlist') : t('products.addToWishlist')}
          </Button>
        )}
      </div>
    </div>
  );
}
