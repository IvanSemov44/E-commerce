import type { Product } from '@/shared/types';

export interface ProductGridProps {
  products: Product[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}
