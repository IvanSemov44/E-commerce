/**
 * Hook for handling featured filter changes
 * @param onIsFeaturedChange - Callback for featured change
 * @returns Handler for featured checkbox
 */
export function useFeaturedFilter(onIsFeaturedChange: (value: boolean | undefined) => void) {
  const handleFeaturedChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onIsFeaturedChange(e.target.checked ? true : undefined);
  };

  return {
    handleFeaturedChange,
  };
}
