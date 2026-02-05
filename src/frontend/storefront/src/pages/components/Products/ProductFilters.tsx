import styles from './ProductFilters.module.css';

interface ProductFiltersProps {
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  isFeatured: boolean | undefined;
  onMinPriceChange: (value: number | undefined) => void;
  onMaxPriceChange: (value: number | undefined) => void;
  onMinRatingChange: (value: number | undefined) => void;
  onIsFeaturedChange: (value: boolean | undefined) => void;
}

export default function ProductFilters({
  minPrice,
  maxPrice,
  minRating,
  isFeatured,
  onMinPriceChange,
  onMaxPriceChange,
  onMinRatingChange,
  onIsFeaturedChange,
}: ProductFiltersProps) {
  return (
    <div className={styles.filters}>
      {/* Price Filter */}
      <div className={styles.filterSection}>
        <h3 className={styles.filterTitle}>Price Range</h3>
        <div className={styles.priceInputs}>
          <input
            type="number"
            placeholder="Min"
            value={minPrice || ''}
            onChange={(e) => onMinPriceChange(e.target.value ? parseFloat(e.target.value) : undefined)}
            className={styles.priceInput}
          />
          <input
            type="number"
            placeholder="Max"
            value={maxPrice || ''}
            onChange={(e) => onMaxPriceChange(e.target.value ? parseFloat(e.target.value) : undefined)}
            className={styles.priceInput}
          />
        </div>
      </div>

      {/* Rating Filter */}
      <div className={styles.filterSection}>
        <h3 className={styles.filterTitle}>Minimum Rating</h3>
        <select
          value={minRating || ''}
          onChange={(e) => onMinRatingChange(e.target.value ? parseFloat(e.target.value) : undefined)}
          className={styles.filterSelect}
        >
          <option value="">All Ratings</option>
          <option value="4">4+ Stars</option>
          <option value="4.5">4.5+ Stars</option>
          <option value="5">5 Stars</option>
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
          <span className={styles.checkboxText}>Featured Products Only</span>
        </label>
      </div>
    </div>
  );
}
