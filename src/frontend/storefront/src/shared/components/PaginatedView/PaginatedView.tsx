import type { ReactNode } from 'react';
import Pagination from '../ui/Pagination';

interface PaginatedViewProps<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  renderItem: (item: T) => ReactNode;
  gridClassName?: string;
  showPageSizeSelector?: boolean;
}

export default function PaginatedView<T extends { id: string }>({
  items,
  totalCount,
  currentPage,
  pageSize,
  onPageChange,
  onPageSizeChange,
  renderItem,
  gridClassName,
  showPageSizeSelector = false,
}: PaginatedViewProps<T>) {
  return (
    <>
      <div className={gridClassName}>
        {items.map(renderItem)}
      </div>

      <Pagination
        currentPage={currentPage}
        totalCount={totalCount}
        pageSize={pageSize}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        showPageSizeSelector={showPageSizeSelector}
        showFirstLast={true}
        showTotal={true}
      />
    </>
  );
}
