import { useTranslation } from 'react-i18next';
import Button from '../../../../shared/components/ui/Button';
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
  const { t } = useTranslation();
  const hasFilters = search || categorySelected || minPrice !== undefined || maxPrice !== undefined || minRating !== undefined || isFeatured;

  if (!hasFilters) return null;

  return (
    <div className={styles.container}>
      {search && (
        <div className={`${styles.badge} ${styles.badgeSearch}`}>
          {t('products.search')}: <strong>{search}</strong>
        </div>
      )}
      
      {categorySelected && (
        <div className={`${styles.badge} ${styles.badgeCategory}`}>
          {t('products.categorySelected')}
        </div>
      )}
      
      {(minPrice !== undefined || maxPrice !== undefined) && (
        <div className={`${styles.badge} ${styles.badgePrice}`}>
          {t('products.price')}: ${minPrice || 0} - ${maxPrice || '∞'}
        </div>
      )}
      
      {minRating !== undefined && (
        <div className={`${styles.badge} ${styles.badgeRating}`}>
          {minRating}+ {t('products.stars')}
        </div>
      )}
      
      {isFeatured && (
        <div className={`${styles.badge} ${styles.badgeFeatured}`}>
          {t('products.featuredOnly')}
        </div>
      )}
      
      <Button
        variant="secondary"
        size="sm"
        onClick={onClearAll}
      >
        {t('common.clear')}
      </Button>
    </div>
  );
}
