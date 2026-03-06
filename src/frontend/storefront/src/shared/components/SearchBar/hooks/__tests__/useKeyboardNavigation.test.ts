import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useKeyboardNavigation } from '../useKeyboardNavigation'

describe('useKeyboardNavigation', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should initialize with selectedIndex -1', () => {
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 5 }))

    expect(result.current.selectedIndex).toBe(-1)
  })

  it('should move down with ArrowDown', () => {
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 5 }))

    const event = new KeyboardEvent('keydown', { key: 'ArrowDown' })
    const preventDefault = vi.spyOn(event, 'preventDefault')

    act(() => {
      result.current.handleKeyDown(event as any)
    })

    expect(result.current.selectedIndex).toBe(0)
    expect(preventDefault).toHaveBeenCalled()
  })

  it('should move up with ArrowUp', () => {
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 5 }))

    // First, move down
    act(() => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' })
      result.current.handleKeyDown(event as any)
    })

    expect(result.current.selectedIndex).toBe(0)

    // Now move up
    act(() => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowUp' })
      result.current.handleKeyDown(event as any)
    })

    expect(result.current.selectedIndex).toBe(-1)
  })

  it('should not go below -1 with ArrowUp', () => {
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 5 }))

    const event = new KeyboardEvent('keydown', { key: 'ArrowUp' })
    act(() => {
      result.current.handleKeyDown(event as any)
    })

    expect(result.current.selectedIndex).toBe(-1)
  })

  it('should not exceed itemCount-1 with ArrowDown', () => {
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 3 }))

    // Move down 4 times (should stop at 2)
    for (let i = 0; i < 4; i++) {
      act(() => {
        const event = new KeyboardEvent('keydown', { key: 'ArrowDown' })
        result.current.handleKeyDown(event as any)
      })
    }

    expect(result.current.selectedIndex).toBe(2)
  })

  it('should call onEnter callback with selected index', () => {
    const onEnter = vi.fn()
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 5, onEnter }))

    // Select first item
    act(() => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' })
      result.current.handleKeyDown(event as any)
    })

    // Press Enter
    act(() => {
      const event = new KeyboardEvent('keydown', { key: 'Enter' })
      const preventDefault = vi.spyOn(event, 'preventDefault')
      result.current.handleKeyDown(event as any)
      expect(preventDefault).toHaveBeenCalled()
    })

    expect(onEnter).toHaveBeenCalledWith(0)
  })

  it('should not call onEnter if no item selected', () => {
    const onEnter = vi.fn()
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 5, onEnter }))

    // Press Enter without selecting
    act(() => {
      const event = new KeyboardEvent('keydown', { key: 'Enter' })
      result.current.handleKeyDown(event as any)
    })

    expect(onEnter).not.toHaveBeenCalled()
  })

  it('should reset selection to -1', () => {
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 5 }))

    // Move down
    act(() => {
      result.current.setSelectedIndex(2)
    })

    expect(result.current.selectedIndex).toBe(2)

    // Reset
    act(() => {
      result.current.resetSelection()
    })

    expect(result.current.selectedIndex).toBe(-1)
  })

  it('should navigate multiple steps correctly', () => {
    const { result } = renderHook(() => useKeyboardNavigation({ itemCount: 5 }))

    // Move down 3 times
    for (let i = 0; i < 3; i++) {
      act(() => {
        const event = new KeyboardEvent('keydown', { key: 'ArrowDown' })
        result.current.handleKeyDown(event as any)
      })
    }

    expect(result.current.selectedIndex).toBe(2)

    // Move up 1 time
    act(() => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowUp' })
      result.current.handleKeyDown(event as any)
    })

    expect(result.current.selectedIndex).toBe(1)
  })
})
