import { useState } from 'react';
import { useCreateReviewMutation } from '@/features/products/api/reviewsApi';
import { useTranslation } from 'react-i18next';
import Button from '../ui/Button';
import Card from '../ui/Card';
import ErrorAlert from '../ErrorAlert';
import StarRating from '../StarRating';

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
  const { t } = useTranslation();

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
      <h3 className={styles.title}>{t('products.writeReview')}</h3>

      {error && <ErrorAlert message={t('products.failedToSubmitReview')} />}

      <form onSubmit={handleSubmit}>
        <div className={styles.formGroup}>
          <label className={styles.label}>{t('products.reviewTitle').replace(' (Optional)', '')}</label>
          <StarRating rating={rating} onRatingChange={setRating} size="lg" />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="review-title" className={styles.label}>
            {t('products.reviewTitle')}
          </label>
          <input
            id="review-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t('products.reviewTitlePlaceholder')}
            className={styles.input}
          />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="review-comment" className={styles.label}>
            {t('products.reviewComment')}
          </label>
          <textarea
            id="review-comment"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder={t('products.reviewCommentPlaceholder')}
            rows={4}
            className={styles.textarea}
            required
          />
        </div>

        <Button type="submit" disabled={!comment.trim() || isLoading}>
          {isLoading ? t('products.submitting') : t('products.submitReview')}
        </Button>
      </form>
    </Card>
  );
}
