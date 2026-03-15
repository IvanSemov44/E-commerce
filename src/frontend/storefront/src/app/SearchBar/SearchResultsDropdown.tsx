import { SpinnerIcon } from '@/shared/components/icons';
import type { SearchResult } from './SearchBar.types';
import styles from './SearchBar.module.css';

interface SearchResultsDropdownProps {
  results: SearchResult[];
  isLoading: boolean;
  isStale: boolean;
  selectedIndex: number;
  query: string;
  listboxId: string;
  onResultClick: (result: SearchResult) => void;
  onViewAll: () => void;
}

function formatPrice(price: number) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(price);
}

export function SearchResultsDropdown({
  results,
  isLoading,
  isStale,
  selectedIndex,
  query,
  listboxId,
  onResultClick,
  onViewAll,
}: SearchResultsDropdownProps) {
  const isPending = isLoading || isStale;

  return (
    <div id={listboxId} role="listbox" aria-label="Search results" className={styles.dropdown}>
      {isPending && results.length === 0 && (
        <div className={styles.dropdownState}>
          <SpinnerIcon width={16} height={16} className={styles.spinner} />
          <span>Searching…</span>
        </div>
      )}

      {!isPending && results.length === 0 && (
        <div className={styles.dropdownState}>No results for &ldquo;{query}&rdquo;</div>
      )}

      {results.length > 0 && (
        <>
          <div className={styles.dropdownHeader}>Products</div>
          <ul className={styles.resultList}>
            {results.map((result, index) => {
              const image = result.images.find((img) => img.isPrimary) ?? result.images[0];
              const isActive = index === selectedIndex;

              return (
                <li
                  key={result.id}
                  id={`${listboxId}-option-${index}`}
                  role="option"
                  aria-selected={isActive}
                >
                  <button
                    type="button"
                    className={`${styles.resultItem} ${isActive ? styles.resultItemActive : ''}`}
                    onClick={() => onResultClick(result)}
                    tabIndex={-1}
                  >
                    <div className={styles.resultImage}>
                      {image ? (
                        <img src={image.url} alt={image.altText ?? result.name} loading="lazy" />
                      ) : (
                        <div className={styles.resultImagePlaceholder} />
                      )}
                    </div>

                    <div className={styles.resultMeta}>
                      <span className={styles.resultName}>{result.name}</span>
                      {result.category && (
                        <span className={styles.resultCategory}>{result.category.name}</span>
                      )}
                    </div>

                    <span className={styles.resultPrice}>{formatPrice(result.price)}</span>
                  </button>
                </li>
              );
            })}
          </ul>

          <button type="button" className={styles.viewAll} onClick={onViewAll}>
            View all results for &ldquo;{query}&rdquo;
          </button>
        </>
      )}
    </div>
  );
}
