import { useTranslation } from 'react-i18next';
import { SearchBar } from '@/app/SearchBar';
import { ActiveFilters } from '@/features/products/components/ActiveFilters';
import { RefreshIcon } from '@/shared/components/icons';
import type { SortBy } from '@/features/products/constants';
import styles from './ProductsToolbar.module.css';

interface ProductsToolbarProps {
  searchInput: string;
  sortBy: SortBy;
  debouncedSearch: string;
  selectedCategoryId: string | undefined;
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  isFeatured: boolean | undefined;
  onSearchChange: (search: string) => void;
  onSortChange: (sort: SortBy) => void;
  onClearFilters: () => void;
  isFetching: boolean;
  isLoading: boolean;
}

export function ProductsToolbar({
  searchInput,
  sortBy,
  debouncedSearch,
  selectedCategoryId,
  minPrice,
  maxPrice,
  minRating,
  isFeatured,
  onSearchChange,
  onSortChange,
  onClearFilters,
  isFetching,
  isLoading,
}: ProductsToolbarProps) {
  const { t } = useTranslation();

  return (
    <div className={styles.searchSection}>
      <div className={styles.searchBar}>
        <div className={styles.searchInput}>
          <SearchBar
            key={searchInput ? 1 : 0}
            size="md"
            placeholder={t('products.searchProducts')}
            onQueryChange={onSearchChange}
          />
        </div>
        <select
          value={sortBy}
          onChange={(e) => onSortChange(e.target.value as SortBy)}
          className={styles.sortSelect}
        >
          <option value="newest">{t('products.sortNewest')}</option>
          <option value="name">{t('products.sortNameAZ')}</option>
          <option value="price-asc">{t('products.sortPriceLowHigh')}</option>
          <option value="price-desc">{t('products.sortPriceHighLow')}</option>
          <option value="rating">{t('products.sortRating')}</option>
        </select>
      </div>

      <ActiveFilters
        search={debouncedSearch}
        categorySelected={!!selectedCategoryId}
        minPrice={minPrice}
        maxPrice={maxPrice}
        minRating={minRating}
        isFeatured={isFeatured}
        onClearAll={onClearFilters}
      />

      {isFetching && !isLoading && (
        <div className={styles.refetchBadge} aria-live="polite" aria-atomic="true">
          <RefreshIcon className={styles.refetchIcon} aria-hidden="true" />
          <span>{t('common.updating')}</span>
        </div>
      )}
    </div>
  );
}
