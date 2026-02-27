import { useCallback, useEffect, useMemo } from 'react';
import Button from './Button';
import Select from './Select';
import styles from './Pagination.module.css';

export interface PaginationProps {
  /** Current page number (1-based) */
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
  /** Whether the component is in loading state */
  loading?: boolean;
  /** Show page size selector */
  showPageSizeSelector?: boolean;
  /** Number of visible page buttons (odd number recommended) */
  maxVisiblePages?: number;
  /** Text for "Previous" button */
  previousLabel?: string;
  /** Text for "Next" button */
  nextLabel?: string;
  /** Custom className */
  className?: string;
}

/**
 * Professional Pagination Component
 * 
 *{