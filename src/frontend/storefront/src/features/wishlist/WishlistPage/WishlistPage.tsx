import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { HeartIcon } from '@/shared/components/icons';
import { useGetWishlistQuery } from '@/features/wishlist/api';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { Button } from '@/shared/components/ui/Button';
import { QueryRenderer } from '@/shared/components';
import { WishlistCard, WishlistSkeleton } from '@/features/wishlist/components';
import styles from './WishlistPage.module.css';

export function WishlistPage() {
  const { t } = useTranslation();
  const { data: wishlist, isLoading, error } = useGetWishlistQuery();

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
        {(data) => (
          <div className={styles.grid}>
            {data.items.map((item) => (
              <WishlistCard
                key={item.id}
                productId={item.productId}
                productName={item.productName}
                price={item.price}
                compareAtPrice={item.compareAtPrice}
                isAvailable={item.isAvailable}
                image={item.productImage}
              />
            ))}
          </div>
        )}
      </QueryRenderer>
    </div>
  );
}
