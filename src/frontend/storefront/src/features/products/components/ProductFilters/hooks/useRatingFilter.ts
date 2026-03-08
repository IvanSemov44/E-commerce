/**
 * Hook for handling rating filter changes
 * @param onMinRatingChange - Callback for rating change
 * @returns Handler for rating select
 */
export function useRatingFilter(onMinRatingChange: (value: number | undefined) => void) {
  const handleRatingChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    onMinRatingChange(e.target.value ? parseFloat(e.target.value) : undefined);
  };

  return {
    handleRatingChange,
  };
}
