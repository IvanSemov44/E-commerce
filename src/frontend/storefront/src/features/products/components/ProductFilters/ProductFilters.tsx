import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { parseOptionalFloat } from '@/features/products/utils/parsing';
import styles from './ProductFilters.module.css';

export function ProductFilters() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();

  const minPrice = parseOptionalFloat(searchParams.get('minPrice'));
  const maxPrice = parseOptionalFloat(searchParams.get('maxPrice'));
  const minRating = parseOptionalFloat(searchParams.get('minRating'));
  const isFeatured = searchParams.get('isFeatured') === 'true';

  const setParam = (key: string, value: string | undefined) => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (value !== undefined) next.set(key, value);
        else next.delete(key);
        next.delete('page');
        return next;
      },
      { replace: true }
    );
  };

  return (
    <div className={styles.filters}>
      {/* Price Filter */}
      <div className={styles.filterSection}>
        <h3 className={styles.filterTitle}>{t('products.priceRange')}</h3>
        <div className={styles.priceInputs}>
          <input
            type="number"
            placeholder={t('products.priceMin')}
            value={minPrice ?? ''}
            onChange={(e) => setParam('minPrice', e.target.value || undefined)}
            className={styles.priceInput}
          />
          <input
            type="number"
            placeholder={t('products.priceMax')}
            value={maxPrice ?? ''}
            onChange={(e) => setParam('maxPrice', e.target.value || undefined)}
            className={styles.priceInput}
          />
        </div>
      </div>

      {/* Rating Filter */}
      <div className={styles.filterSection}>
        <h3 className={styles.filterTitle}>{t('products.minimumRating')}</h3>
        <select
          value={minRating ?? ''}
          onChange={(e) => setParam('minRating', e.target.value || undefined)}
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
            checked={isFeatured}
            onChange={(e) => setParam('isFeatured', e.target.checked ? 'true' : undefined)}
            className={styles.checkbox}
          />
          <span className={styles.checkboxText}>{t('products.featuredOnly')}</span>
        </label>
      </div>
    </div>
  );
}
