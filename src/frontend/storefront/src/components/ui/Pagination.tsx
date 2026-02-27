import { useMemo, useCallback, type KeyboardEvent } from 'react';
import styles from './Pagination.module.css';

interface PaginationProps {
  /** Current active page (1-indexed) */
  currentPage: number;
  /** Total number of items */
  totalCount: number;
  /** Number of items per page */
  pageSize: number;
  /** Callback when page changes */
  onPageChange: (page: number) => void;
  /** Callback when page size changes */
  onPageSizeChange?: (pageSize: number) => void;
  /** Available page size options */
  pageSizeOptions?: number[];
  /** Whether to show first/last page buttons */
  showFirstLast?: boolean;
  /** Whether to show page size selector */
  showPageSizeSelector?: boolean;
  /** Whether to show total items count */
  showTotal?: boolean;
  /** Additional CSS classes */
  className?: string;
  /** Label for page (accessible) */
  pageLabel?: (page: number) => string;
}

/**
 * Professional Pagination Component
 * 
 * Features:
 * - Page numbers with ellipsis for many pages
 * - First/last page navigation
 * - Items per page selector
 * - Keyboard navigation support
 * - Full accessibility (ARIA labels, focus management)
 * - Responsive design
 */
export default function Pagination({
  currentPage,
  totalCount,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [12, 24, 48, 96],
  showFirstLast = true,
  showPageSizeSelector = false,
  showTotal = true,
  className = '',
  pageLabel = (page) => `Go to page ${page}`,
}: PaginationProps) {
  const totalPages = Math.ceil(totalCount / pageSize);
  
  // Calculate visible page numbers with ellipsis
  const visiblePages = useMemo(() => {
    const delta = 2; // Number of pages to show on each side of current
    const range: (number | 'ellipsis')[] = [];
    
    for (let i = 1; i <= totalPages; i++) {
      if (
        i === 1 ||
        i === totalPages ||
        (i >= currentPage - delta && i <= currentPage + delta)
      ) {
        range.push(i);
      } else if (range[range.length - 1] !== 'ellipsis') {
        range.push('ellipsis');
      }
    }
    
    return range;
  }, [currentPage, totalPages]);

  // Handle keyboard navigation
  const handleKeyDown = useCallback((e: KeyboardEvent<HTMLButtonElement>, page: number) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onPageChange(page);
    }
  }, [onPageChange]);

  // Handle page size change
  const handlePageSizeChange = useCallback((e: React.ChangeEvent<HTMLSelectElement>) => {
    const newSize = Number(e.target.value);
    onPageSizeChange?.(newSize);
  }, [onPageSizeChange]);

  // Calculate showing range
  const showingStart = totalCount === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const showingEnd = Math.min(currentPage * pageSize, totalCount);

  // Don't render if there's only one page or no items
  if (totalPages <= 1 && totalCount === 0) {
    return null;
  }

  const paginationClassName = [
    styles.pagination,
    showPageSizeSelector && styles.paginationWide,
    className,
  ].filter(Boolean).join(' ');

  return (
    <nav
      className={paginationClassName}
      role="navigation"
      aria-label="Pagination"
    >
      {/* First Page Button */}
      {showFirstLast && (
        <button
          className={styles.navButton}
          onClick={() => onPageChange(1)}
          disabled={currentPage === 1}
          aria-label="Go to first page"
        >
          ««
        </button>
      )}

      {/* Previous Page Button */}
      <button
        className={styles.navButton}
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage === 1}
        aria-label="Go to previous page"
      >
        «
      </button>

      {/* Page Numbers */}
      <div className={styles.pages}>
        {visiblePages.map((page, index) => {
          if (page === 'ellipsis') {
            return (
              <span
                key={`ellipsis-${index}`}
                className={styles.ellipsis}
                aria-hidden="true"
              >
                …
              </span>
            );
          }

          const isActive = page === currentPage;

          return (
            <button
              key={page}
              className={`${styles.pageButton} ${isActive ? styles.active : ''}`}
              onClick={() => onPageChange(page)}
              onKeyDown={(e) => handleKeyDown(e, page)}
              aria-label={pageLabel(page)}
              aria-current={isActive ? 'page' : undefined}
              disabled={isActive}
            >
              {page}
            </button>
          );
        })}
      </div>

      {/* Next Page Button */}
      <button
        className={styles.navButton}
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage === totalPages}
        aria-label="Go to next page"
      >
        »
      </button>

      {/* Last Page Button */}
      {showFirstLast && (
        <button
          className={styles.navButton}
          onClick={() => onPageChange(totalPages)}
          disabled={currentPage === totalPages}
          aria-label="Go to last page"
        >
          »»
        </button>
      )}

      {/* Page Info */}
      <span className={styles.pageInfo}>
        {showingStart}-{showingEnd} of {totalCount}
      </span>

      {/* Page Size Selector */}
      {showPageSizeSelector && onPageSizeChange && (
        <div className={styles.pageSize}>
          <label className={styles.pageSizeLabel} htmlFor="page-size-select">
            per page:
          </label>
          <select
            id="page-size-select"
            className={styles.pageSizeSelect}
            value={pageSize}
            onChange={handlePageSizeChange}
            aria-label="Items per page"
          >
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </div>
      )}
    </nav>
  );
}
