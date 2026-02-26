import { useState, useRef, useEffect, useCallback, useMemo } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useGetProductsQuery } from '../store/api/productApi';
import styles from './SearchBar.module.css';

interface SearchBarProps {
  /** Placeholder text */
  placeholder?: string;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
  /** Additional CSS class */
  className?: string;
  /** Whether to show on mobile */
  showOnMobile?: boolean;
}

/**
 * SearchBar Component
 * 
 * A modern, accessible search input with:
 * - Live search with debouncing
 * - Preview dropdown with product results
 * - Keyboard navigation
 * - Clear button
 * - Search icon animation
 */
export function SearchBar({ 
  placeholder = 'Search products...', 
  size = 'md',
  className = '',
  showOnMobile = false
}: SearchBarProps) {
  const [query, setQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [isExpanded, setIsExpanded] = useState(false);
  const [isFocused, setIsFocused] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  // Debounce search query
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQuery(query.trim());
    }, 300);
    return () => clearTimeout(timer);
  }, [query]);

  // Live search query - only search when debounced query has at least 2 characters
  const { data: searchResults, isFetching } = useGetProductsQuery(
    { search: debouncedQuery, pageSize: 5 },
    { skip: debouncedQuery.length < 2 }
  );

  // Memoized results
  const results = useMemo(() => {
    return searchResults?.items || [];
  }, [searchResults]);

  const showDropdown = isFocused && debouncedQuery.length >= 2 && (results.length > 0 || isFetching);

  // Reset selected index when results change
  useEffect(() => {
    setSelectedIndex(-1);
  }, [results]);

  // Handle click outside to collapse
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsExpanded(false);
        setIsFocused(false);
      }
    };

    if (isExpanded) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isExpanded]);

  // Handle keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Cmd/Ctrl + K to focus search
      if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
        event.preventDefault();
        inputRef.current?.focus();
        setIsExpanded(true);
        setIsFocused(true);
      }
      
      // Escape to blur
      if (event.key === 'Escape' && isFocused) {
        inputRef.current?.blur();
        setIsExpanded(false);
        setIsFocused(false);
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isFocused]);

  const handleSubmit = useCallback((e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      navigate(`/products?search=${encodeURIComponent(query.trim())}`);
      inputRef.current?.blur();
      setIsExpanded(false);
      setIsFocused(false);
    }
  }, [query, navigate]);

  const handleClear = useCallback(() => {
    setQuery('');
    setDebouncedQuery('');
    inputRef.current?.focus();
  }, []);

  const handleFocus = useCallback(() => {
    setIsFocused(true);
    setIsExpanded(true);
  }, []);

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (!showDropdown) return;

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setSelectedIndex((prev) => (prev < results.length - 1 ? prev + 1 : prev));
        break;
      case 'ArrowUp':
        e.preventDefault();
        setSelectedIndex((prev) => (prev > 0 ? prev - 1 : -1));
        break;
      case 'Enter':
        if (selectedIndex >= 0 && results[selectedIndex]) {
          e.preventDefault();
          navigate(`/products/${results[selectedIndex].slug}`);
          setIsFocused(false);
          setIsExpanded(false);
        }
        break;
    }
  }, [showDropdown, results, selectedIndex, navigate]);

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(price);
  };

  return (
    <div 
      ref={containerRef}
      className={`${styles.container} ${styles[size]} ${isExpanded ? styles.expanded : ''} ${className} ${!showOnMobile ? styles.hideOnMobile : ''}`}
    >
      <form onSubmit={handleSubmit} className={styles.form}>
        <div className={`${styles.searchWrapper} ${isFocused ? styles.focused : ''}`}>
          {/* Search Icon */}
          <span className={styles.searchIcon}>
            <svg fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path 
                strokeLinecap="round" 
                strokeLinejoin="round" 
                d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" 
              />
            </svg>
          </span>

          {/* Input */}
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onFocus={handleFocus}
            onKeyDown={handleKeyDown}
            placeholder={placeholder}
            className={styles.input}
            aria-label="Search products"
            aria-expanded={showDropdown}
            aria-autocomplete="list"
            role="combobox"
          />

          {/* Loading Spinner */}
          {isFetching && (
            <span className={styles.loadingSpinner}>
              <svg className={styles.spinner} viewBox="0 0 24 24">
                <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" fill="none" strokeLinecap="round" />
              </svg>
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
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>
      </form>

      {/* Search Results Dropdown */}
      {showDropdown && (
        <div className={styles.dropdown} role="listbox">
          {isFetching ? (
            <div className={styles.loadingState}>
              <span>Searching...</span>
            </div>
          ) : (
            <>
              {results.map((product, index) => (
                <Link
                  key={product.id}
                  to={`/products/${product.slug}`}
                  className={`${styles.resultItem} ${index === selectedIndex ? styles.selected : ''}`}
                  role="option"
                  aria-selected={index === selectedIndex}
                  onClick={() => {
                    setIsFocused(false);
                    setIsExpanded(false);
                  }}
                >
                  <div className={styles.resultImage}>
                    {product.images[0]?.url ? (
                      <img src={product.images[0].url} alt={product.name} />
                    ) : (
                      <div className={styles.resultPlaceholder}>
                        <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M20 7l-8-4-8 4m0 0l8-4m0 0l8 4m0 0v10l-8 4m0-10L4 7v10l8 4" />
                        </svg>
                      </div>
                    )}
                  </div>
                  <div className={styles.resultInfo}>
                    <span className={styles.resultName}>{product.name}</span>
                    <span className={styles.resultPrice}>
                      {product.compareAtPrice && (
                        <span className={styles.originalPrice}>{formatPrice(product.compareAtPrice)}</span>
                      )}
                      <span className={styles.currentPrice}>{formatPrice(product.price)}</span>
                    </span>
                  </div>
                </Link>
              ))}
              <Link
                to={`/products?search=${encodeURIComponent(debouncedQuery)}`}
                className={styles.viewAll}
                onClick={() => {
                  setIsFocused(false);
                  setIsExpanded(false);
                }}
              >
                View all results for "{debouncedQuery}"
              </Link>
            </>
          )}
        </div>
      )}
    </div>
  );
}

export default SearchBar;
