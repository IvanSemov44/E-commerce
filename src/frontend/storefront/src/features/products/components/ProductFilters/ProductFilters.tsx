import { useTranslation } from 'react-i18next';
import type { ProductFiltersProps } from './ProductFilters.types';
import styles from './ProductFilters.module.css';

export function ProductFilters({
  minPrice,
  maxPrice,
  minRating,
  isFeatured,
  onMinPriceChange,
  onMaxPriceChange,
  onMinRatingChange,
  onIsFeaturedChange,
}: ProductFiltersProps) {
  const { t } = useTranslation();

  return (
    <div className={styles.filters}>
      {/* Price Filter */}
      <div className={styles.filterSection}>
        <h3 className={styles.filterTitle}>{t('products.priceRange')}</h3>
        <div className={styles.priceInputs}>
          <input
            type="number"
            placeholder={t('products.priceMin')}
            value={minPrice || ''}
            onChange={(e) =>
              onMinPriceChange(e.target.value ? parseFloat(e.target.value) : undefined)
            }
            className={styles.priceInput}
          />
          <input
            type="number"
            placeholder={t('products.priceMax')}
            value={maxPrice || ''}
            onChange={(e) =>
              onMaxPriceChange(e.target.value ? parseFloat(e.target.value) : undefined)
            }
            className={styles.priceInput}
          />
        </div>
      </div>

      {/* Rating Filter */}
      <div className={styles.filterSection}>
        <h3 className={styles.filterTitle}>{t('products.minimumRating')}</h3>
        <select
          value={minRating || ''}
          onChange={(e) =>
            onMinRatingChange(e.target.value ? parseFloat(e.target.value) : undefined)
          }
          className={styles.filterSelect}
        >
          <option value="">{t('products.allRatings')}</option>
          <option value="4">{t('products.rating4Plus')}</option>
          <option value="4.5">{t('products.rating45Plus')}</option>
          <option value="5">{t('products.rating5')}</option>
        </select>
      </div>

      {/* Featured Filter */}
      <div className={styles.filterSection}>
        <label className={styles.checkboxLabel}>
          <input
            type="checkbox"
            checked={isFeatured || false}
            onChange={(e) => onIsFeaturedChange(e.target.checked ? true : undefined)}
            className={styles.checkbox}
          />
          <span className={styles.checkboxText}>{t('products.featuredOnly')}</span>
        </label>
      </div>
    </div>
  );
}
