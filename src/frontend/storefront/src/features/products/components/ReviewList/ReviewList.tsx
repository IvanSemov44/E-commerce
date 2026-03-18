import { Card } from '@/shared/components/ui/Card';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { ErrorAlert } from '@/shared/components/ErrorAlert';
import { StarRating } from '../StarRating';
import { ReviewSkeleton } from '@/features/products/components';
import { useTranslation } from 'react-i18next';

import type { ProductReview } from '@/shared/types';
import styles from './ReviewList.module.css';

interface ReviewListProps {
  reviews: ProductReview[];
  isLoading?: boolean;
  error?: unknown;
}

export function ReviewList({ reviews, isLoading, error }: ReviewListProps) {
  const { t } = useTranslation();

  if (isLoading) {
    return <ReviewSkeleton />;
  }

  if (error) {
    return <ErrorAlert message={t('products.failedToLoadReviews')} />;
  }

  if (reviews.length === 0) {
    return <EmptyState icon="orders" title={t('products.noReviewsYet')} />;
  }

  return (
    <div className={styles.grid}>
      {reviews.map((review) => (
        <Card key={review.id} variant="bordered" padding="lg">
          <div className={styles.reviewCard}>
            <div className={styles.header}>
              <div className={styles.headerContent}>
                {review.title && <h4 className={styles.title}>{review.title}</h4>}
                <div className={styles.ratingRow}>
                  <StarRating rating={review.rating} readonly size="md" />
                  <span className={styles.ratingText}>{review.rating}/5</span>
                </div>
              </div>
            </div>
          </div>

          <p className={styles.comment}>{review.comment}</p>

          <div className={styles.footer}>
            <span>
              {t('products.by')} {review.userName || t('products.anonymous')}
            </span>
            <span>{new Date(review.createdAt).toLocaleDateString()}</span>
          </div>
        </Card>
      ))}
    </div>
  );
}
