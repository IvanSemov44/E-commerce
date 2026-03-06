import type { Product } from '@/shared/types'

export interface SearchBarProps {
  /** Placeholder text */
  placeholder?: string
  /** Size variant */
  size?: 'sm' | 'md' | 'lg'
  /** Additional CSS class */
  className?: string
  /** Whether to show on mobile */
  showOnMobile?: boolean
  /** Callback when search query changes */
  onSearch?: (query: string) => void
  /** Callback when result item is selected */
  onSelectResult?: (product: Product) => void
  /** Callback when search error occurs */
  onError?: (error: unknown) => void
}

export interface SearchResult extends Product {
  /** Formatted display price */
  displayPrice: string
  /** Whether product has image */
  hasImage: boolean
}

export interface SearchState {
  query: string
  isExpanded: boolean
  isFocused: boolean
  selectedIndex: number
}
