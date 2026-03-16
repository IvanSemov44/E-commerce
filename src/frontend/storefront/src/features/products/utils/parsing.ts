import { VALID_SORT_BY, type SortBy } from '@/features/products/constants';

export function parseOptionalFloat(value: string | null): number | undefined {
  return value ? parseFloat(value) : undefined;
}

export function parseSortBy(value: string | null): SortBy {
  return value && (VALID_SORT_BY as readonly string[]).includes(value)
    ? (value as SortBy)
    : 'newest';
}
