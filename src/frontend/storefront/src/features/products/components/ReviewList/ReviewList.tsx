import Card from '../ui/Card';
import ErrorAlert from '../ErrorAlert';
import StarRating from '../StarRating';
import { Skeleton, SkeletonLabelRow } from '@/shared/components/Skeletons';
import { useTranslation } from 'react-i18next';

import styles from './ReviewList.module.css';

export interface Review {
  id: string;
  title?: string;
  comment?: string;
  rating: number;
  userName?: string;
  createdAt: string;
}

interface ReviewListProps {
  reviews: Review[];
  isLoading?: boolean;
  error?: unknown;
  onReviewDeleted?: () => void;
}

export default function ReviewList({ reviews, isLoading, error }: ReviewListProps) {
  const { t } = useTranslation();

  if (isLoading) {
    return (
      <div className={styles.grid}>
        {Array.from({ length: 3 }).map((_, i) => (
          <Card key={i} variant="bordered" padding="lg">
            <SkeletonLabelRow
              items={[
                { width: '55%', height: 16 },
                { width: 90, height: 14 },
              ]}
            />
            <Skeleton width="100%" height={12} variant="rounded" animation="wave" />
            <Skeleton width="90%" height={12} variant="rounded" animation="wave" />
            <Skeleton width="70%" height={12} variant="rounded" animation="wave" />
            <SkeletonLabelRow
              items={[
                { width: '28%', height: 12 },
                { width: '18%', height: 12 },
              ]}
              between={false}
            />
          </Card>
        ))}
      </div>
    );
  }

  if (error) {
    return <ErrorAlert message={t('products.failedToLoadReviews')} />;
  }

  if (reviews.length === 0) {
    return <div className={styles.emptyState}>{t('products.noReviewsYet')}</div>;
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
