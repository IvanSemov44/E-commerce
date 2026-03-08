import { useState, useRef, useEffect, useCallback } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { SearchIcon, SpinnerIcon, CloseIcon, PackageIcon } from '@/shared/components/icons';
import type { Product } from '@/shared/types';
import { useSearch, useKeyboardNavigation, useClickOutside } from './hooks';
import { formatPriceLocale } from '@/shared/lib/utils/priceFormatter';
import type { SearchBarProps } from './SearchBar.types';
import styles from './SearchBar.module.css';

interface SearchResultsDropdownProps {
  isFetching: boolean;
  query: string;
  results: Product[];
  selectedIndex: number;
  formatPrice: (amount: number) => string;
  onSelectResult?: (product: Product) => void;
  onClose: () => void;
  resetSelection: () => void;
  t: (key: string, params?: { query: string }) => string;
}

function shouldShowDropdown(
  isFocused: boolean,
  query: string,
  resultsCount: number,
  isFetching: boolean
) {
  return isFocused && query.trim().length >= 2 && (resultsCount > 0 || isFetching);
}

interface SearchShortcutsOptions {
  inputRef: React.RefObject<HTMLInputElement | null>;
  isFocused: boolean;
  onOpen: () => void;
  onClose: () => void;
}

function useSearchShortcuts({ inputRef, isFocused, onOpen, onClose }: SearchShortcutsOptions) {
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
        event.preventDefault();
        inputRef.current?.focus();
        onOpen();
      }

      if (event.key === 'Escape' && isFocused) {
        inputRef.current?.blur();
        onClose();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [inputRef, isFocused, onOpen, onClose]);
}

function SearchResultsDropdown({
  isFetching,
  query,
  results,
  selectedIndex,
  formatPrice,
  onSelectResult,
  onClose,
  resetSelection,
  t,
}: SearchResultsDropdownProps) {
  if (isFetching) {
    return (
      <div className={styles.dropdown} role="listbox">
        <div className={styles.loadingState}>
          <span>{t('common.searching') || 'Searching...'}</span>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.dropdown} role="listbox">
      <>
        {results.length > 0 &&
          results.map((product, index) => (
            <Link
              key={product.id}
              to={`/products/${product.slug}`}
              className={`${styles.resultItem} ${index === selectedIndex ? styles.selected : ''}`}
              role="option"
              aria-selected={index === selectedIndex}
              onClick={() => {
                onSelectResult?.(product);
                onClose();
                resetSelection();
              }}
            >
              <div className={styles.resultImage}>
                {product.images[0]?.url ? (
                  <img src={product.images[0].url} alt={product.name} />
                ) : (
                  <div className={styles.resultPlaceholder}>
                    <PackageIcon />
                  </div>
                )}
              </div>
              <div className={styles.resultInfo}>
                <span className={styles.resultName}>{product.name}</span>
                <span className={styles.resultPrice}>
                  {product.compareAtPrice && (
                    <span className={styles.originalPrice}>
                      {formatPrice(product.compareAtPrice)}
                    </span>
                  )}
                  <span className={styles.currentPrice}>{formatPrice(product.price)}</span>
                </span>
              </div>
            </Link>
          ))}
        <Link
          to={`/products?search=${encodeURIComponent(query.trim())}`}
          className={styles.viewAll}
          onClick={onClose}
        >
          {t('common.viewAllResults', { query: query.trim() }) ||
            `View all results for "${query.trim()}"`}
        </Link>
      </>
    </div>
  );
}

/**
 * SearchBar Component
 *
 * A modern, accessible search input with:
 * - Live search with debouncing via useSearch
 * - Keyboard navigation via useKeyboardNavigation
 * - Click-outside handling via useClickOutside
 * - Product results dropdown
 */
export const SearchBar = function SearchBar({
  placeholder,
  size = 'md',
  className = '',
  showOnMobile = false,
  onSearch,
  onSelectResult,
  onError,
}: SearchBarProps) {
  const { t } = useTranslation();
  const defaultPlaceholder = t('products.searchProducts');
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  // State
  const [isExpanded, setIsExpanded] = useState(false);
  const [isFocused, setIsFocused] = useState(false);
  const openDropdown = useCallback(() => {
    setIsFocused(true);
    setIsExpanded(true);
  }, []);
  const closeDropdown = useCallback(() => {
    setIsFocused(false);
    setIsExpanded(false);
  }, []);

  // Custom hooks
  const { query, setQuery, results, isFetching, error, handleClear } = useSearch();
  const {
    selectedIndex,
    handleKeyDown: handleKeyboardNav,
    resetSelection,
  } = useKeyboardNavigation({
    itemCount: results.length,
    onEnter: (index) => {
      const product = results[index];
      onSelectResult?.(product);
      navigate(`/products/${product.slug}`);
      closeDropdown();
      resetSelection();
    },
  });

  // Click outside handler
  useClickOutside(containerRef, closeDropdown);
  useSearchShortcuts({ inputRef, isFocused, onOpen: openDropdown, onClose: closeDropdown });

  // Notify on search query change
  useEffect(() => {
    onSearch?.(query);
  }, [query, onSearch]);

  // Handle errors
  useEffect(() => {
    if (error) {
      onError?.(error);
    }
  }, [error, onError]);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      if (query.trim()) {
        navigate(`/products?search=${encodeURIComponent(query.trim())}`);
        inputRef.current?.blur();
        closeDropdown();
      }
    },
    [query, navigate, closeDropdown]
  );

  const handleFocus = openDropdown;

  const handleKeyDownWrapper = useCallback(
    (e: React.KeyboardEvent) => {
      handleKeyboardNav(e);
    },
    [handleKeyboardNav]
  );

  const showDropdown = shouldShowDropdown(isFocused, query, results.length, isFetching);

  const formatPrice = formatPriceLocale;
  const containerClassName = `${styles.container} ${styles[size]} ${isExpanded ? styles.expanded : ''} ${className} ${!showOnMobile ? styles.hideOnMobile : ''}`;

  return (
    <div ref={containerRef} className={containerClassName}>
      <form onSubmit={handleSubmit} className={styles.form}>
        <div className={`${styles.searchWrapper} ${isFocused ? styles.focused : ''}`}>
          {/* Search Icon */}
          <span className={styles.searchIcon}>
            <SearchIcon />
          </span>

          {/* Input */}
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onFocus={handleFocus}
            onKeyDown={handleKeyDownWrapper}
            placeholder={placeholder || defaultPlaceholder}
            className={styles.input}
            aria-label="Search products"
            aria-expanded={showDropdown}
            aria-autocomplete="list"
            role="combobox"
          />

          {/* Loading Spinner */}
          {isFetching && (
            <span className={styles.loadingSpinner}>
              <SpinnerIcon className={styles.spinner} />
            </span>
          )}

          {/* Keyboard Shortcut Hint */}
          {!isFocused && !query && (
            <span className={styles.shortcut}>
              <kbd>⌘</kbd>
              <kbd>K</kbd>
            </span>
          )}

          {/* Clear Button */}
          {query && !isFetching && (
            <button
              type="button"
              onClick={handleClear}
              className={styles.clearButton}
              aria-label="Clear search"
            >
              <CloseIcon />
            </button>
          )}
        </div>
      </form>

      {/* Search Results Dropdown */}
      {showDropdown && (
        <SearchResultsDropdown
          isFetching={isFetching}
          query={query}
          results={results}
          selectedIndex={selectedIndex}
          formatPrice={formatPrice}
          onSelectResult={onSelectResult}
          onClose={closeDropdown}
          resetSelection={resetSelection}
          t={t}
        />
      )}
    </div>
  );
};

export default SearchBar;
