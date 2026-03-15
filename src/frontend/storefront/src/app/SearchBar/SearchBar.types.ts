import type { Product } from '@/shared/types';

export interface SearchBarProps {
  size?: 'sm' | 'md';
  placeholder?: string;
}

export type SearchResult = Pick<Product, 'id' | 'name' | 'slug' | 'price' | 'images' | 'category'>;
