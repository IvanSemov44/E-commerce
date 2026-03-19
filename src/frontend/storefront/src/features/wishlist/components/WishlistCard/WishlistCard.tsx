import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import { ImageIcon } from '@/shared/components/icons';
import { formatPrice } from '@/shared/lib/utils/priceFormatter';
import { useRemoveFromWishlistMutation } from '@/features/wishlist/api';
import { useAddToCartMutation } from '@/features/cart/api';
import { useApiErrorHandler } from '@/shared/hooks';
import styles from './WishlistCard.module.css';

interface WishlistCardProps {
  productId: string;
  productName: string;
  price: number;
  compareAtPrice?: number;
  isAvailable?: boolean;
  image?: string;
}

export function WishlistCard({
  productId,
  productName,
  price,
  compareAtPrice,
  isAvailable = true,
  image,
}: WishlistCardProps) {
  const { t } = useTranslation();
  const [removeFromWishlist] = useRemoveFromWishlistMutation();
  const [addToCart] = useAddToCartMutation();
  const { handleError } = useApiErrorHandler();

  async function handleRemove() {
    try {
      await removeFromWishlist(productId).unwrap();
    } catch (err) {
      handleError(err, t('common.errorOccurred'));
    }
  }

  async function handleAddToCart() {
    try {
      await addToCart({ productId, quantity: 1 }).unwrap();
    } catch (err) {
      handleError(err, t('common.errorOccurred'));
    }
  }

  return (
    <div className={styles.card}>
      <div className={styles.imagePlaceholder}>
        {image ? <img src={image} alt={productName} className={styles.image} /> : <ImageIcon />}
      </div>

      <div className={styles.content}>
        <p className={styles.productName}>{productName}</p>

        <div className={styles.priceRow}>
          <span className={styles.price}>{formatPrice(price)}</span>
          {compareAtPrice && (
            <span className={styles.comparePrice}>{formatPrice(compareAtPrice)}</span>
          )}
        </div>

        {!isAvailable && <p className={styles.outOfStock}>{t('wishlist.outOfStock')}</p>}

        <div className={styles.actions}>
          <Button size="sm" onClick={handleAddToCart} disabled={!isAvailable}>
            {t('wishlist.addToCart')}
          </Button>
          <Button variant="outline" size="sm" onClick={handleRemove}>
            {t('wishlist.remove')}
          </Button>
        </div>
      </div>
    </div>
  );
}
