import Card from './ui/Card';
import ErrorAlert from './ErrorAlert';
import StarRating from './StarRating';

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
  if (isLoading) {
    return <div className={styles.loading}>Loading reviews...</div>;
  }

  if (error) {
    return <ErrorAlert message="Failed to load reviews." />;
  }

  if (reviews.length === 0) {
    return <div className={styles.emptyState}>No reviews yet</div>;
  }

  return (
    <div className={styles.grid}>
      {reviews.map((review) => (
        <Card key={review.id} variant="bordered" padding="lg">
          <div className={styles.reviewCard}>
            <div className={styles.header}>
              <div className={styles.headerContent}>
                {review.title && (
                  <h4 className={styles.title}>
                    {review.title}
                  </h4>
                )}
                <div className={styles.ratingRow}>
                  <StarRating rating={review.rating} readonly size="md" />
                  <span className={styles.ratingText}>
                    {review.rating}/5
                  </span>
                </div>
              </div>
            </div>
          </div>

          <p className={styles.comment}>
            {review.comment}
          </p>

          <div className={styles.footer}>
            <span>By {review.userName || 'Anonymous'}</span>
            <span>{new Date(review.createdAt).toLocaleDateString()}</span>
          </div>
        </Card>
      ))}
    </div>
  );
}
