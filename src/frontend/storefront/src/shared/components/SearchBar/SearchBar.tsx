import { useState, useRef, useEffect, useCallback } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { SearchIcon, SpinnerIcon, CloseIcon, PackageIcon } from '@/shared/components/icons'
import { useSearch, useKeyboardNavigation, useClickOutside } from './hooks'
import type { SearchBarProps } from './SearchBar.types'
import styles from './SearchBar.module.css'

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
  const { t } = useTranslation()
  const defaultPlaceholder = t('products.searchProducts')
  const inputRef = useRef<HTMLInputElement>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()

  // State
  const [isExpanded, setIsExpanded] = useState(false)
  const [isFocused, setIsFocused] = useState(false)

  // Custom hooks
  const { query, setQuery, results, isFetching, error, handleClear } = useSearch()
  const { selectedIndex, handleKeyDown: handleKeyboardNav, resetSelection } = useKeyboardNavigation({
    itemCount: results.length,
    onEnter: (index) => {
      const product = results[index]
      onSelectResult?.(product)
      navigate(`/products/${product.slug}`)
      setIsFocused(false)
      setIsExpanded(false)
      resetSelection()
    },
  })

  // Click outside handler
  useClickOutside(containerRef, () => {
    setIsExpanded(false)
    setIsFocused(false)
  })

  // Global keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Cmd/Ctrl + K to focus search
      if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
        event.preventDefault()
        inputRef.current?.focus()
        setIsExpanded(true)
        setIsFocused(true)
      }

      // Escape to blur
      if (event.key === 'Escape' && isFocused) {
        inputRef.current?.blur()
        setIsExpanded(false)
        setIsFocused(false)
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isFocused])

  // Notify on search query change
  useEffect(() => {
    onSearch?.(query)
  }, [query, onSearch])

  // Handle errors
  useEffect(() => {
    if (error) {
      onError?.(error)
    }
  }, [error, onError])

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault()
      if (query.trim()) {
        navigate(`/products?search=${encodeURIComponent(query.trim())}`)
        inputRef.current?.blur()
        setIsExpanded(false)
        setIsFocused(false)
      }
    },
    [query, navigate]
  )

  const handleFocus = useCallback(() => {
    setIsFocused(true)
    setIsExpanded(true)
  }, [])

  const handleKeyDownWrapper = useCallback(
    (e: React.KeyboardEvent) => {
      handleKeyboardNav(e)
    },
    [handleKeyboardNav]
  )

  const showDropdown = isFocused && query.trim().length >= 2 && (results.length > 0 || isFetching)

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(price)
  }

  return (
    <div
      ref={containerRef}
      className={`${styles.container} ${styles[size]} ${isExpanded ? styles.expanded : ''} ${className} ${!showOnMobile ? styles.hideOnMobile : ''}`}
    >
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
        <div className={styles.dropdown} role="listbox">
          {isFetching ? (
            <div className={styles.loadingState}>
              <span>{t('common.searching') || 'Searching...'}</span>
            </div>
          ) : (
            <>
              {results.length > 0 && results.map((product, index) => (
                <Link
                  key={product.id}
                  to={`/products/${product.slug}`}
                  className={`${styles.resultItem} ${index === selectedIndex ? styles.selected : ''}`}
                  role="option"
                aria-selected={index === selectedIndex}
                  onClick={() => {
                    onSelectResult?.(product)
                    setIsFocused(false)
                    setIsExpanded(false)
                    resetSelection()
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
                        <span className={styles.originalPrice}>{formatPrice(product.compareAtPrice)}</span>
                      )}
                      <span className={styles.currentPrice}>{formatPrice(product.price)}</span>
                    </span>
                  </div>
                </Link>
              ))}
              <Link
                to={`/products?search=${encodeURIComponent(query.trim())}`}
                className={styles.viewAll}
                onClick={() => {
                  setIsFocused(false)
                  setIsExpanded(false)
                }}
              >
                {t('common.viewAllResults', { query: query.trim() }) || `View all results for "${query.trim()}"`}
              </Link>
            </>
          )}
        </div>
      )}
    </div>
  )
}

export default SearchBar
