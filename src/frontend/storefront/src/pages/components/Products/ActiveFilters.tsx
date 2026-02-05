import Button from '../../../components/ui/Button';
import styles from './ActiveFilters.module.css';

interface ActiveFiltersProps {
  search: string;
  categorySelected: boolean;
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  isFeatured: boolean | undefined;
  onClearAll: () => void;
}

export default function ActiveFilters({
  search,
  categorySelected,
  minPrice,
  maxPrice,
  minRating,
  isFeatured,
  onClearAll,
}: ActiveFiltersProps) {
  const hasFilters = search || categorySelected || minPrice !== undefined || maxPrice !== undefined || minRating !== undefined || isFeatured;

  if (!hasFilters) return null;

  return (
    <div className={styles.container}>
      {search && (
        <div className={`${styles.badge} ${styles.badgeSearch}`}>
          Search: <strong>{search}</strong>
        </div>
      )}
      
      {categorySelected && (
        <div className={`${styles.badge} ${styles.badgeCategory}`}>
          Category Selected
        </div>
      )}
      
      {(minPrice !== undefined || maxPrice !== undefined) && (
        <div className={`${styles.badge} ${styles.badgePrice}`}>
          Price: ${minPrice || 0} - ${maxPrice || '∞'}
        </div>
      )}
      
      {minRating !== undefined && (
        <div className={`${styles.badge} ${styles.badgeRating}`}>
          {minRating}+ Stars
        </div>
      )}
      
      {isFeatured && (
        <div className={`${styles.badge} ${styles.badgeFeatured}`}>
          Featured Only
        </div>
      )}
      
      <Button
        variant="secondary"
        size="sm"
        onClick={onClearAll}
        style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem' }}
      >
        Clear Filters
      </Button>
    </div>
  );
}
