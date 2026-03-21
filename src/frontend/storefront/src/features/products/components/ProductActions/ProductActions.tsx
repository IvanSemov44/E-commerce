import { useTranslation } from 'react-i18next';
import { Button, QuantityControl } from '@/shared/components/ui';
import { ErrorAlert } from '@/shared/components/ErrorAlert';
import { useAppSelector } from '@/shared/lib/store';
import { useCartActions, useWishlistToggle } from '@/features/products/hooks';
import type { ProductDetail } from '@/shared/types';
import styles from './ProductActions.module.css';

export function ProductActions({ product }: { product: ProductDetail }) {
  const { t } = useTranslation();
  const isAuthenticated = useAppSelector((s) => s.auth.isAuthenticated);
  const cart = useCartActions(product);
  const wishlist = useWishlistToggle(product.id);

  return (
    <div className={styles.actions}>
      <div className={styles.stockSection}>
        <p
          className={`${styles.stockLabel} ${product.stockQuantity > 0 ? styles.inStock : styles.outOfStock}`}
        >
          {product.stockQuantity === 0
            ? t('common.outOfStock')
            : t('products.inStockCount', { count: product.stockQuantity })}
        </p>
        {product.stockQuantity > 0 && product.stockQuantity <= product.lowStockThreshold && (
          <p className={styles.lowStockWarning}>
            {t('products.lowStockWarning', { count: product.stockQuantity })}
          </p>
        )}
      </div>

      <div className={styles.quantitySection}>
        <label className={styles.quantityLabel}>{t('productDetail.quantity')}:</label>
        <div className={styles.quantityControls}>
          <QuantityControl
            value={cart.quantity}
            max={product.stockQuantity}
            onChange={cart.setQuantity}
            editable
          />
          {cart.inCartQuantity && (
            <span className={styles.cartHint}>
              ({cart.inCartQuantity} {t('productDetail.inCart')})
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
          disabled={product.stockQuantity === 0 || cart.addedToCart || cart.isAdding}
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
