// WishlistPage - User's wishlist page
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { HeartIcon, ImageIcon, CloseIcon } from '@/shared/components/icons';
import {
  useGetWishlistQuery,
  useRemoveFromWishlistMutation,
} from '@/features/wishlist/api/wishlistApi';
import { useAddToCartMutation } from '@/features/cart/api/cartApi';
import { useApiErrorHandler } from '@/shared/hooks';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button } from '@/shared/components/ui/Button';
import QueryRenderer from '@/shared/components/QueryRenderer';
import WishlistSkeleton from '@/features/wishlist/components/WishlistSkeleton';
import { formatPrice } from '@/shared/lib/utils/priceFormatter';
import styles from './WishlistPage.module.css';

export default function WishlistPage() {
  const { t } = useTranslation();
  const { data: wishlist, isLoading, error } = useGetWishlistQuery();
  const [removeFromWishlist] = useRemoveFromWishlistMutation();
  const [addToCart] = useAddToCartMutation();
  const { handleError } = useApiErrorHandler();

  const handleRemove = async (productId: string) => {
    try {
      await removeFromWishlist(productId).unwrap();
    } catch (err) {
      handleError(err, t('common.errorOccurred'));
    }
  };

  const handleAddToCart = async (productId: string, quantity: number = 1) => {
    try {
      await addToCart({ productId, quantity }).unwrap();
    } catch (err) {
      handleError(err, t('common.errorOccurred'));
    }
  };

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>{t('wishlist.title')}</h1>

      <QueryRenderer
        isLoading={isLoading}
        error={error}
        data={wishlist}
        loadingSkeleton={{ custom: <WishlistSkeleton count={8} /> }}
        emptyState={{
          icon: <HeartIcon className={styles.emptyIcon} />,
          title: t('wishlist.empty'),
          action: (
            <Link to={ROUTE_PATHS.products}>
              <Button>{t('wishlist.continueShopping')}</Button>
            </Link>
          ),
        }}
      >
        {(wishlist) => (
          <div className={styles.grid}>
            {wishlist.items.map((item) => (
              <div key={item.id} className={styles.card}>
                {item.productImage ? (
                  <img
                    src={item.productImage}
                    alt={item.productName}
                    className={styles.productImage}
                  />
                ) : (
                  <div className={styles.imagePlaceholder}>
                    <ImageIcon />
                  </div>
                )}

                <div className={styles.content}>
                  <h3 className={styles.productName}>{item.productName}</h3>

                  <div className={styles.priceRow}>
                    <span className={styles.price}>{formatPrice(item.price)}</span>
                    {item.compareAtPrice && (
                      <span className={styles.comparePrice}>
                        {formatPrice(item.compareAtPrice)}
                      </span>
                    )}
                  </div>

                  {!item.isAvailable && (
                    <p className={styles.outOfStock}>{t('wishlist.outOfStock')}</p>
                  )}

                  <div className={styles.actions}>
                    <button
                      className={styles.actionButton}
                      onClick={() => handleAddToCart(item.productId)}
                      disabled={!item.isAvailable}
                    >
                      {t('wishlist.addToCart')}
                    </button>
                    <button
                      className={styles.removeButton}
                      onClick={() => handleRemove(item.productId)}
                      aria-label={t('wishlist.remove')}
                    >
                      <CloseIcon />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </QueryRenderer>
    </div>
  );
}
