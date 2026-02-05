import type { ReactNode } from 'react';
import Button from './ui/Button';
import styles from './PaginatedView.module.css';

interface PaginatedViewProps<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  renderItem: (item: T) => ReactNode;
  gridClassName?: string;
}

export default function PaginatedView<T extends { id: string }>({
  items,
  totalCount,
  currentPage,
  pageSize,
  onPageChange,
  renderItem,
  gridClassName,
}: PaginatedViewProps<T>) {
  const totalPages = Math.ceil(totalCount / pageSize);
  const hasNextPage = currentPage < totalPages;
  const hasPrevPage = currentPage > 1;

  return (
    <>
      <div className={gridClassName}>
        {items.map(renderItem)}
      </div>

      <div className={styles.pagination}>
        <Button
          variant="secondary"
          onClick={() => onPageChange(currentPage - 1)}
          disabled={!hasPrevPage}
        >
          Previous
        </Button>

        <span className={styles.pageInfo}>
          Page {currentPage} of {totalPages}
        </span>

        <Button
          variant="secondary"
          onClick={() => onPageChange(currentPage + 1)}
          disabled={!hasNextPage}
        >
          Next
        </Button>
      </div>
    </>
  );
}
