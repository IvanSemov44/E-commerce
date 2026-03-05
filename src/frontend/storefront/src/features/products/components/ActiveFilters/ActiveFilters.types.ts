export interface ActiveFiltersProps {
  search: string;
  categorySelected: boolean;
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  isFeatured: boolean | undefined;
  onClearAll: () => void;
}
