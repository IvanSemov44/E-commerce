import { useTranslation } from 'react-i18next';
import Button from '@/shared/components/ui/Button';
import type { WishlistCardProps } from './WishlistCard.types';
import { useWishlistRemove, useWishlistAddToCart } from './WishlistCard.hooks';
import styles from './WishlistCard.module.css';

export default function WishlistCard({ productId, productName, image }: Omit<WishlistCardProps, 'price'>) {
  const { t } = useTranslation();
  const handleRemove = useWishlistRemove(productId);
  const handleAddToCart = useWishlistAddToCart(productId);

  return (
    <div className={styles.card}>
      <div className={styles.imagePlaceholder}>
        {image ? (
          <img src={image} alt={productName} className={styles.image} />
        ) : (
          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        )}
      </div>
      
      <div className={styles.content}>
        <p className={styles.productName}>
          {productName}
        </p>
        
        <div className={styles.actions}>
          <Button 
            size="sm" 
            onClick={handleAddToCart}
          >
            {t('wishlist.addToCart')}
          </Button>
          <Button 
            variant="outline" 
            size="sm"
            onClick={handleRemove}
          >
            {t('wishlist.remove')}
          </Button>
        </div>
      </div>
    </div>
  );
}
