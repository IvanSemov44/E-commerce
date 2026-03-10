import Skeleton from '@/shared/components/Skeletons';
import ProductSkeleton from '@/features/products/components/ProductSkeleton/ProductSkeleton';
import styles from './QueryRendererSkeleton.module.css';

interface QueryRendererSkeletonProps {
  count?: number;
  type?: 'card' | 'text' | 'image';
}

export default function QueryRendererSkeleton({
  count = 1,
  type = 'card',
}: QueryRendererSkeletonProps) {
  if (type === 'card') {
    return (
      <div className={styles.cards}>
        {Array.from({ length: count }).map((_, i) => (
          <ProductSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (type === 'text') {
    return (
      <div className={styles.textStack}>
        {Array.from({ length: count }).map((_, i) => (
          <Skeleton key={i} width="100%" height={16} variant="rounded" animation="wave" />
        ))}
      </div>
    );
  }

  return (
    <div className={styles.images}>
      {Array.from({ length: count }).map((_, i) => (
        <Skeleton key={i} width="100%" height={220} variant="rounded" animation="wave" />
      ))}
    </div>
  );
}
