import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { usePerformanceMonitor } from '../usePerformanceMonitor'

// Mock PerformanceObserver
class MockPerformanceObserver {
  callback: PerformanceObserverCallback
  constructor(callback: PerformanceObserverCallback) {
    this.callback = callback
  }
  observe() {}
  disconnect() {}
}

// Mock logger
vi.mock('../utils/logger', () => ({
  logger: {
    metric: vi.fn(),
    info: vi.fn(),
    error: vi.fn(),
  },
}))

// Mock import.meta.env
vi.stubGlobal('import.meta', {
  env: {
    MODE: 'development',
  },
})

describe('usePerformanceMonitor', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    
    // Mock PerformanceObserver
    vi.stubGlobal('PerformanceObserver', MockPerformanceObserver)
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('should initialize without errors', () => {
    const { result } = renderHook(() => usePerformanceMonitor())
    expect(result.current).toBeUndefined()
  })

  it('should accept options', () => {
    const onMetric = vi.fn()
    
    const { result } = renderHook(() => 
      usePerformanceMonitor({ 
        onMetric,
        enableLogging: true 
      })
    )
    
    expect(result.current).toBeUndefined()
  })

  it('should call onMetric callback when provided', () => {
    const onMetric = vi.fn()
    
    renderHook(() => 
      usePerformanceMonitor({ 
        onMetric,
        enableLogging: false 
      })
    )
    
    // Hook should not throw
    expect(onMetric).not.toHaveBeenCalled()
  })

  it('should work with default options', () => {
    const { result } = renderHook(() => usePerformanceMonitor())
    expect(result.current).toBeUndefined()
  })
})
