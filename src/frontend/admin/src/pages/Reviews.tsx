import toast from 'react-hot-toast';
import { useGetPendingReviewsQuery, useApproveReviewMutation, useRejectReviewMutation } from '../store/api/reviewsApi';
import Button from '../components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/Card';
import Badge from '../components/ui/Badge';
import styles from './Reviews.module.css';

export default function Reviews() {
  const { data: reviews, isLoading, error } = useGetPendingReviewsQuery();
  const [approveReview, { isLoading: approving }] = useApproveReviewMutation();
  const [rejectReview, { isLoading: rejecting }] = useRejectReviewMutation();

  const handleApprove = async (reviewId: string) => {
    try {
      await approveReview(reviewId).unwrap();
      toast.success('Review approved successfully');
    } catch (err) {
      toast.error('Failed to approve review');
    }
  };

  const handleReject = async (reviewId: string) => {
    if (!confirm('Are you sure you want to reject and delete this review?')) return;

    try {
      await rejectReview(reviewId).unwrap();
      toast.success('Review rejected successfully');
    } catch (err) {
      toast.error('Failed to reject review');
    }
  };

  const renderStars = (rating: number) => {
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  };

  if (isLoading) {
    return (
      <div className={styles.container}>
        <h1 className={styles.title}>Review Moderation</h1>
        <Card variant="elevated">
          <CardContent>
            <p className={styles.loadingState}>Loading reviews...</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <h1 className={styles.title}>Review Moderation</h1>
        <Card variant="elevated">
          <CardContent>
            <p className={styles.errorState}>
              Failed to load reviews
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Review Moderation</h1>
        <Badge variant="warning">{reviews?.length || 0} Pending</Badge>
      </div>

      {!reviews || reviews.length === 0 ? (
        <Card variant="elevated">
          <CardContent>
            <p className={styles.emptyState}>
              No pending reviews to moderate
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className={styles.reviewsGrid}>
          {reviews.map((review) => (
            <Card key={review.id} variant="elevated">
              <CardHeader>
                <div className={styles.reviewHeader}>
                  <div>
                    <CardTitle>{review.title || 'No Title'}</CardTitle>
                    <div className={styles.reviewMeta}>
                      <span className={styles.rating}>{renderStars(review.rating)}</span>
                      <span className={styles.separator}>•</span>
                      <span>{review.userName || 'Anonymous'}</span>
                      {review.isVerified && (
                        <>
                          <span className={styles.separator}>•</span>
                          <Badge variant="success">Verified</Badge>
                        </>
                      )}
                    </div>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <p className={styles.productName}>
                  Product: {review.productName || `ID: ${review.productId}`}
                </p>
                <p className={styles.comment}>{review.comment}</p>
                <p className={styles.date}>
                  Submitted: {new Date(review.createdAt).toLocaleString()}
                </p>
                <div className={styles.actions}>
                  <Button
                    onClick={() => handleApprove(review.id)}
                    disabled={approving || rejecting}
                    size="sm"
                  >
                    Approve
                  </Button>
                  <Button
                    variant="destructive"
                    onClick={() => handleReject(review.id)}
                    disabled={approving || rejecting}
                    size="sm"
                  >
                    Reject
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
