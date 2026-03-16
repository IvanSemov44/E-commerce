import { useState, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { SearchBar } from '@/app/SearchBar';
import { ActiveFilters } from '@/features/products/components/ActiveFilters';
import { RefreshIcon } from '@/shared/components/icons';
import { useDebounce } from '@/shared/hooks';
import { SEARCH_DEBOUNCE_MS, type SortBy } from '@/features/products/constants';
import { parseOptionalFloat, parseSortBy } from '@/features/products/utils/parsing';
import styles from './ProductsToolbar.module.css';

interface ProductsToolbarProps {
  isRefetching: boolean;
}

export function ProductsToolbar({ isRefetching }: ProductsToolbarProps) {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();

  // Read current filter values from URL (for ActiveFilters badge)
  const urlSearch = searchParams.get('search') ?? '';
  const selectedCategoryId = searchParams.get('categoryId') ?? undefined;
  const minPrice = parseOptionalFloat(searchParams.get('minPrice'));
  const maxPrice = parseOptionalFloat(searchParams.get('maxPrice'));
  const minRating = parseOptionalFloat(searchParams.get('minRating'));
  const isFeatured = searchParams.get('isFeatured') === 'true' ? true : undefined;
  const sortBy = parseSortBy(searchParams.get('sortBy'));

  // Local search: debounce → write to URL
  const [searchInput, setSearchInput] = useState(urlSearch);
  const [clearKey, setClearKey] = useState(0);
  const debouncedSearch = useDebounce(searchInput, SEARCH_DEBOUNCE_MS);
  const isFirstRender = useRef(true);

  useEffect(() => {
    if (isFirstRender.current) {
      isFirstRender.current = false;
      return;
    }
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (debouncedSearch) next.set('search', debouncedSearch);
        else next.delete('search');
        next.delete('page');
        return next;
      },
      { replace: true }
    );
  }, [debouncedSearch, setSearchParams]);

  const handleSortChange = (sort: SortBy) => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (sort !== 'newest') next.set('sortBy', sort);
        else next.delete('sortBy');
        next.delete('page');
        return next;
      },
      { replace: true }
    );
  };

  const handleClearFilters = () => {
    setSearchInput('');
    setClearKey((k) => k + 1);
    setSearchParams({}, { replace: true });
  };

  return (
    <div className={styles.searchSection}>
      <div className={styles.searchBar}>
        <div className={styles.searchInput}>
          <SearchBar
            key={clearKey}
            size="md"
            placeholder={t('products.searchProducts')}
            onQueryChange={setSearchInput}
          />
        </div>
        <select
          value={sortBy}
          onChange={(e) => handleSortChange(e.target.value as SortBy)}
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
        search={urlSearch}
        categorySelected={!!selectedCategoryId}
        minPrice={minPrice}
        maxPrice={maxPrice}
        minRating={minRating}
        isFeatured={isFeatured}
        onClearAll={handleClearFilters}
      />

      {isRefetching && (
        <div className={styles.refetchBadge} aria-live="polite" aria-atomic="true">
          <RefreshIcon className={styles.refetchIcon} aria-hidden="true" />
          <span>{t('common.updating')}</span>
        </div>
      )}
    </div>
  );
}
