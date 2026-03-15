import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/components/ui/Button';
import { ImageIcon } from '@/shared/components/icons';
import type { WishlistCardProps } from './WishlistCard.types';
import { useWishlistRemove, useWishlistAddToCart } from './WishlistCard.hooks';
import styles from './WishlistCard.module.css';

export default function WishlistCard({
  productId,
  productName,
  image,
}: Omit<WishlistCardProps, 'price'>) {
  const { t } = useTranslation();
  const handleRemove = useWishlistRemove(productId);
  const handleAddToCart = useWishlistAddToCart(productId);

  return (
    <div className={styles.card}>
      <div className={styles.imagePlaceholder}>
        {image ? <img src={image} alt={productName} className={styles.image} /> : <ImageIcon />}
      </div>

      <div className={styles.content}>
        <p className={styles.productName}>{productName}</p>

        <div className={styles.actions}>
          <Button size="sm" onClick={handleAddToCart}>
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
