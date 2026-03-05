/**
 * Hook for handling price filter changes
 * @param onMinPriceChange - Callback for min price change
 * @param onMaxPriceChange - Callback for max price change
 * @returns Handlers for price inputs
 */
export function usePriceFilters(
  onMinPriceChange: (value: number | undefined) => void,
  onMaxPriceChange: (value: number | undefined) => void,
) {
  const handleMinPriceChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onMinPriceChange(e.target.value ? parseFloat(e.target.value) : undefined);
  };

  const handleMaxPriceChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onMaxPriceChange(e.target.value ? parseFloat(e.target.value) : undefined);
  };

  return {
    handleMinPriceChange,
    handleMaxPriceChange,
  };
}
