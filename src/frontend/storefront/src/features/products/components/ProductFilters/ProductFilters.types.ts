export interface ProductFiltersProps {
  minPrice: number | undefined;
  maxPrice: number | undefined;
  minRating: number | undefined;
  isFeatured: boolean | undefined;
  onMinPriceChange: (value: number | undefined) => void;
  onMaxPriceChange: (value: number | undefined) => void;
  onMinRatingChange: (value: number | undefined) => void;
  onIsFeaturedChange: (value: boolean | undefined) => void;
}
