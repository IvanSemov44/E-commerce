// WishlistPage - User's wishlist page
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useGetWishlistQuery, useRemoveFromWishlistMutation } from '../../api/wishlistApi';
import { useAddToCartMutation } from '../../../cart/api/cartApi';
import Button from '../../../../shared/components/ui/Button';
import EmptyState from '../../../../shared/components/EmptyState';
import QueryRenderer from '../../../../shared/components/QueryRenderer';
import styles from './WishlistPage.module.css';

export default function WishlistPage() {
  const { t } = useTranslation();
  const { data: wishlist, isLoading, error } = useGetWishlistQuery();
  const [removeFromWishlist] = useRemoveFromWishlistMutation();
  const [addToCart] = useAddToCartMutation();

  const handleRemove = async (productId: string) => {
    try {
      await removeFromWishlist(productId).unwrap();
    } catch (err) {
      console.error('Failed to remove from wishlist:', err);
    }
  };

  const handleAddToCart = async (productId: string, quantity: number = 1) => {
    try {
      await addToCart({ productId, quantity }).unwrap();
    } catch (err) {
      console.error('Failed to add to cart:', err);
    }
  };

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>{t('wishlist.title')}</h1>
      
      <QueryRenderer
        isLoading={isLoading}
        error={error}
        data={wishlist}
        emptyState={{
          icon: (
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.emptyIcon}>
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
            </svg>
          ),
          title: t('wishlist.empty'),
          action: (
            <Link to="/products">
              <Button>{t('wishlist.continueShopping')}</Button>
            </Link>
          )
        }}
      >
        {(wishlist) => (
          <>
            {wishlist.items.length === 0 ? (
              <EmptyState
                icon={
                  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" className={styles.emptyIcon}>
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                  </svg>
                }
                title={t('wishlist.empty')}
                action={
                  <Link to="/products">
                    <Button>{t('wishlist.continueShopping')}</Button>
                  </Link>
                }
              />
            ) : (
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
                        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                      </div>
                    )}
                    
                    <div className={styles.content}>
                      <h3 className={styles.productName}>
                        {item.productName}
                      </h3>
                      
                      <div className={styles.priceRow}>
                        <span className={styles.price}>${item.price.toFixed(2)}</span>
                        {item.compareAtPrice && (
                          <span className={styles.comparePrice}>${item.compareAtPrice.toFixed(2)}</span>
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
                          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" width="20" height="20">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </QueryRenderer>
    </div>
  );
}
