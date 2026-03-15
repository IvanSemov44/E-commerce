import { useState, useRef, useId, useEffect } from 'react';
import { useNavigate } from 'react-router';
import { SearchIcon, CloseIcon } from '@/shared/components/icons';
import { ROUTE_PATHS } from '@/shared/constants/navigation';
import { useProductSearch } from './hooks/useProductSearch';
import { SearchResultsDropdown } from './SearchResultsDropdown';
import type { SearchBarProps, SearchResult } from './SearchBar.types';
import styles from './SearchBar.module.css';

export function SearchBar({
  size = 'md',
  placeholder = 'Search… (Ctrl+K)',
  onQueryChange,
}: SearchBarProps) {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [focused, setFocused] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState(-1);

  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const listboxId = useId();

  const { results, isLoading, isStale } = useProductSearch(query);

  const showDropdown =
    focused && query.trim().length >= 2 && (isLoading || isStale || results.length > 0);

  // Ctrl+K / Cmd+K — focus the search input from anywhere
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        inputRef.current?.focus();
        inputRef.current?.select();
      }
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, []);

  const close = () => {
    setFocused(false);
    setSelectedIndex(-1);
  };

  const navigateToResult = (result: SearchResult) => {
    navigate(ROUTE_PATHS.productDetail.replace(':slug', result.slug));
    setQuery('');
    close();
  };

  // Keyboard navigation delegated to the container so both input and results receive events
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!showDropdown) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex((i) => Math.min(i + 1, results.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex((i) => Math.max(i - 1, -1));
    } else if (e.key === 'Escape') {
      e.preventDefault();
      close();
      inputRef.current?.blur();
    } else if (e.key === 'Enter' && selectedIndex >= 0) {
      e.preventDefault();
      const result = results[selectedIndex];
      if (result) navigateToResult(result);
    }
  };

  // Close when focus leaves the entire widget (clicking outside)
  const handleBlur = (e: React.FocusEvent<HTMLDivElement>) => {
    if (!containerRef.current?.contains(e.relatedTarget as Node)) {
      close();
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = query.trim();
    if (!trimmed) return;
    if (!onQueryChange) {
      navigate(`${ROUTE_PATHS.products}?search=${encodeURIComponent(trimmed)}`);
    }
    close();
    inputRef.current?.blur();
  };

  const handleViewAll = () => {
    if (!onQueryChange) {
      navigate(`${ROUTE_PATHS.products}?search=${encodeURIComponent(query.trim())}`);
    }
    close();
    inputRef.current?.blur();
  };

  return (
    <div
      ref={containerRef}
      className={`${styles.wrapper} ${styles[size]}`}
      onKeyDown={handleKeyDown}
      onBlur={handleBlur}
    >
      <form onSubmit={handleSubmit} role="search">
        <div className={styles.inputWrapper}>
          <span className={styles.searchIcon} aria-hidden="true">
            <SearchIcon size={size === 'sm' ? 14 : 16} />
          </span>

          <input
            ref={inputRef}
            type="search"
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setSelectedIndex(-1);
              onQueryChange?.(e.target.value);
            }}
            onFocus={() => setFocused(true)}
            placeholder={placeholder}
            className={styles.input}
            aria-label="Search products"
            aria-autocomplete="list"
            aria-controls={showDropdown ? listboxId : undefined}
            aria-expanded={showDropdown}
            aria-activedescendant={
              selectedIndex >= 0 ? `${listboxId}-option-${selectedIndex}` : undefined
            }
            role="combobox"
            autoComplete="off"
            spellCheck={false}
          />

          {query && (
            <button
              type="button"
              className={styles.clearButton}
              onClick={() => {
                setQuery('');
                setSelectedIndex(-1);
                inputRef.current?.focus();
              }}
              aria-label="Clear search"
              tabIndex={-1}
            >
              <CloseIcon width={12} height={12} />
            </button>
          )}
        </div>
      </form>

      {showDropdown && (
        <SearchResultsDropdown
          results={results}
          isLoading={isLoading}
          isStale={isStale}
          selectedIndex={selectedIndex}
          query={query.trim()}
          listboxId={listboxId}
          onResultClick={navigateToResult}
          onViewAll={handleViewAll}
        />
      )}
    </div>
  );
}
