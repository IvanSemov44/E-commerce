import { useState } from 'react';
import { useCreateReviewMutation } from '../store/api/reviewsApi';
import Button from './ui/Button';
import Card from './ui/Card';
import ErrorAlert from './ErrorAlert';
import StarRating from './StarRating';

import styles from './ReviewForm.module.css';

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
      <h3 className={styles.title}>Write a Review</h3>

      {error && <ErrorAlert message="Failed to submit review. Please try again." />}

      <form onSubmit={handleSubmit}>
        <div className={styles.formGroup}>
          <label className={styles.label}>Rating</label>
          <StarRating rating={rating} onRatingChange={setRating} size="lg" />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="review-title" className={styles.label}>
            Title (Optional)
          </label>
          <input
            id="review-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g., Great product!"
            className={styles.input}
          />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="review-comment" className={styles.label}>
            Comment *
          </label>
          <textarea
            id="review-comment"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Share your experience with this product..."
            rows={4}
            className={styles.textarea}
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
