import { useState, useCallback } from 'react'

interface UseKeyboardNavigationOptions {
  itemCount: number
  onEnter?: (index: number) => void
}

/**
 * useKeyboardNavigation Hook
 *
 * Handles keyboard navigation for list-like components
 * Supports ArrowUp, ArrowDown, and Enter keys
 *
 * @param options - Configuration with item count and Enter callback
 * @returns Selected index, setter, and keyboard handler
 */
export function useKeyboardNavigation({ itemCount, onEnter }: UseKeyboardNavigationOptions) {
  const [selectedIndex, setSelectedIndex] = useState(-1)

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault()
          setSelectedIndex((prev) => (prev < itemCount - 1 ? prev + 1 : prev))
          break
        case 'ArrowUp':
          e.preventDefault()
          setSelectedIndex((prev) => (prev > 0 ? prev - 1 : -1))
          break
        case 'Enter':
          if (selectedIndex >= 0) {
            e.preventDefault()
            onEnter?.(selectedIndex)
          }
          break
      }
    },
    [itemCount, selectedIndex, onEnter]
  )

  const resetSelection = useCallback(() => {
    setSelectedIndex(-1)
  }, [])

  return { selectedIndex, setSelectedIndex, handleKeyDown, resetSelection }
}
