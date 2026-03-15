import type { Product } from '@/shared/types';

export interface SearchBarProps {
  size?: 'sm' | 'md';
  placeholder?: string;
  /** When provided: called on every keystroke and navigation on submit/view-all is suppressed. */
  onQueryChange?: (query: string) => void;
}

export type SearchResult = Pick<Product, 'id' | 'name' | 'slug' | 'price' | 'images' | 'category'>;
