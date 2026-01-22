import Card from './ui/Card';
import ErrorAlert from './ErrorAlert';

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
    return <div style={{ color: '#666' }}>Loading reviews...</div>;
  }

  if (error) {
    return <ErrorAlert message="Failed to load reviews." />;
  }

  if (reviews.length === 0) {
    return <div style={{ color: '#666', textAlign: 'center', padding: '2rem' }}>No reviews yet</div>;
  }

  return (
    <div style={{ display: 'grid', gap: '1rem' }}>
      {reviews.map((review) => (
        <Card key={review.id} variant="bordered" padding="lg">
          <div style={{ marginBottom: '0.5rem' }}>
            <div style={{ display: 'flex', alignItems: 'start', gap: '1rem' }}>
              <div style={{ flex: 1 }}>
                {review.title && (
                  <h4 style={{ margin: '0 0 0.25rem 0', fontSize: '1.125rem' }}>
                    {review.title}
                  </h4>
                )}
                <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                  <div style={{ display: 'flex', color: '#ffc107' }}>
                    {Array.from({ length: 5 }).map((_, i) => (
                      <span key={i}>{i < review.rating ? '★' : '☆'}</span>
                    ))}
                  </div>
                  <span style={{ fontSize: '0.875rem', color: '#666' }}>
                    {review.rating}/5
                  </span>
                </div>
              </div>
            </div>
          </div>

          <p
            style={{
              margin: '1rem 0 0.5rem 0',
              color: '#333',
              lineHeight: 1.6,
              whiteSpace: 'pre-wrap',
            }}
          >
            {review.comment}
          </p>

          <div style={{ display: 'flex', gap: '1rem', fontSize: '0.875rem', color: '#666' }}>
            <span>By {review.userName || 'Anonymous'}</span>
            <span>{new Date(review.createdAt).toLocaleDateString()}</span>
          </div>
        </Card>
      ))}
    </div>
  );
}
