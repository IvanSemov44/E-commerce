import { useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePerformanceMonitor } from '@/shared/hooks';
import { useProductData } from '@/features/products/hooks/useProductData';
import { Card } from '@/shared/components/ui/Card';
import QueryRenderer from '@/shared/components/QueryRenderer';
import { ReviewForm } from '@/features/products/components/ReviewForm';
import { ReviewList } from '@/features/products/components/ReviewList';
import {
  ProductImageGallery,
  ProductInfo,
  ProductActions,
  ProductSkeleton,
} from '@/features/products/components';
import { useAppSelector } from '@/shared/lib/store';
import styles from './ProductDetailPage.module.css';

export function ProductDetailPage() {
  usePerformanceMonitor();
  const { slug = '' } = useParams();
  const { t } = useTranslation();
  const { isAuthenticated } = useAppSelector((state) => state.auth);

  const { product, isLoading, error, reviews, reviewsLoading, reviewsError } = useProductData(slug);

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <QueryRenderer
          isLoading={isLoading}
          error={error}
          data={product}
          loadingSkeleton={{
            custom: (
              <Card variant="default" padding="lg">
                <ProductSkeleton />
              </Card>
            ),
          }}
          emptyState={{
            title: t('products.notFound'),
            description: t('products.notFoundDescription'),
          }}
          errorMessage={t('products.failedToLoad')}
        >
          {(product) => (
            <Card variant="default" padding="lg">
              <div className={styles.grid}>
                <ProductImageGallery images={product.images} productName={product.name} />

                <div className={styles.details}>
                  <ProductInfo
                    name={product.name}
                    description={product.description}
                    averageRating={product.averageRating}
                    reviewCount={product.reviewCount}
                    price={product.price}
                    compareAtPrice={product.compareAtPrice}
                  />

                  <ProductActions product={product} />
                </div>
              </div>

              <div className={styles.reviewsSection}>
                <h2 className={styles.reviewsTitle}>{t('products.customerReviews')}</h2>

                {isAuthenticated && (
                  <div className={styles.reviewFormSection}>
                    <ReviewForm productId={product.id} />
                  </div>
                )}

                <ReviewList
                  reviews={reviews || []}
                  isLoading={reviewsLoading}
                  error={reviewsError}
                />
              </div>
            </Card>
          )}
        </QueryRenderer>
      </div>
    </div>
  );
}
