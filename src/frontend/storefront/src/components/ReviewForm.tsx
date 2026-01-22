import { useState } from 'react';
import { useCreateReviewMutation } from '../store/api/reviewsApi';
import Button from './ui/Button';
import Card from './ui/Card';
import ErrorAlert from './ErrorAlert';

interface ReviewFormProps {
  productId: string;
  onSuccess?: () => void;
}

export default function ReviewForm({ productId, onSuccess }: ReviewFormProps) {
  const [rating, setRating] = useState(5);
  const [title, setTitle] = useState('');
  const [comment, setComment] = useState('');
  const [createReview, { isLoading, error }] = useCreateReviewMutation();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!comment.trim()) {
      return;
    }

    try {
      await createReview({
        productId,
        title: title.trim() || undefined,
        comment: comment.trim(),
        rating,
      }).unwrap();

      setRating(5);
      setTitle('');
      setComment('');

      onSuccess?.();
    } catch {
      // Error handled by error state
    }
  };

  return (
    <Card variant="bordered" padding="lg">
      <h3 style={{ marginTop: 0 }}>Write a Review</h3>

      {error && <ErrorAlert message="Failed to submit review. Please try again." />}

      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}>
            Rating
          </label>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            {[1, 2, 3, 4, 5].map((star) => (
              <button
                key={star}
                type="button"
                onClick={() => setRating(star)}
                style={{
                  background: 'none',
                  border: 'none',
                  fontSize: '1.5rem',
                  cursor: 'pointer',
                  color: star <= rating ? '#ffc107' : '#ddd',
                  padding: 0,
                }}
                title={`${star} stars`}
              >
                ★
              </button>
            ))}
          </div>
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <label
            htmlFor="review-title"
            style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}
          >
            Title (Optional)
          </label>
          <input
            id="review-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g., Great product!"
            style={{
              width: '100%',
              padding: '0.75rem',
              fontSize: '1rem',
              border: '1px solid #e0e0e0',
              borderRadius: '0.5rem',
              boxSizing: 'border-box',
            }}
          />
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <label
            htmlFor="review-comment"
            style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}
          >
            Comment *
          </label>
          <textarea
            id="review-comment"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Share your experience with this product..."
            rows={4}
            style={{
              width: '100%',
              padding: '0.75rem',
              fontSize: '1rem',
              border: '1px solid #e0e0e0',
              borderRadius: '0.5rem',
              boxSizing: 'border-box',
              fontFamily: 'inherit',
              resize: 'vertical',
            }}
            required
          />
        </div>

        <Button type="submit" disabled={!comment.trim() || isLoading}>
          {isLoading ? 'Submitting...' : 'Submit Review'}
        </Button>
      </form>
    </Card>
  );
}
