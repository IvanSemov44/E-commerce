import { formatPrice } from '@/shared/lib/utils/priceFormatter';
import styles from './ProductInfo.module.css';

interface ProductInfoProps {
  name: string;
  description: string | undefined;
  averageRating: number;
  reviewCount: number;
  price: number;
  compareAtPrice?: number;
}

export default function ProductInfo({
  name,
  description,
  averageRating,
  reviewCount,
  price,
  compareAtPrice,
}: ProductInfoProps) {
  return (
    <div className={styles.info}>
      <h1 className={styles.name}>{name}</h1>

      <div className={styles.rating}>
        <div className={styles.ratingContainer}>
          <span className={styles.ratingIcon}>★</span>
          <span className={styles.ratingValue}>{averageRating}</span>
        </div>
        <span className={styles.ratingCount}>({reviewCount} reviews)</span>
      </div>

      <div className={styles.priceSection}>
        <div className={styles.priceContainer}>
          <span className={styles.pricePrimary}>{formatPrice(price)}</span>
          {compareAtPrice && (
            <span className={styles.priceCompare}>{formatPrice(compareAtPrice)}</span>
          )}
        </div>
      </div>

      {description && <p className={styles.description}>{description}</p>}
    </div>
  );
}
