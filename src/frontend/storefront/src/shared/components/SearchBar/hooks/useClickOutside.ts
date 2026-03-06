import { useEffect, useCallback } from 'react'
import type { RefObject } from 'react'

/**
 * useClickOutside Hook
 *
 * Detects clicks outside a referenced element and triggers callback
 * Automatically cleans up event listeners
 *
 * @param ref - Reference to the element to track
 * @param callback - Function to call when clicking outside
 */
export function useClickOutside<T extends HTMLElement>(
  ref: RefObject<T | null>,
  callback: () => void
) {
  const handleClickOutside = useCallback(
    (event: MouseEvent) => {
      if (ref.current && !ref.current.contains(event.target as Node)) {
        callback()
      }
    },
    [ref, callback]
  )

  useEffect(() => {
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [handleClickOutside])
}
