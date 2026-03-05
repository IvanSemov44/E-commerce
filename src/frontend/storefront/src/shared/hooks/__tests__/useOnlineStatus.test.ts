import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useOnlineStatus } from '../useOnlineStatus';

describe('useOnlineStatus', () => {
  beforeEach(() => {
    // Reset navigator.onLine to true by default
    Object.defineProperty(window.navigator, 'onLine', {
      writable: true,
      value: true,
    });
  });

  it('should return isOnline as true when online', () => {
    const { result } = renderHook(() => useOnlineStatus());
    
    expect(result.current.isOnline).toBe(true);
  });

  it('should return isOnline as false when offline', () => {
    Object.defineProperty(window.navigator, 'onLine', {
      writable: true,
      value: false,
    });
    
    const { result } = renderHook(() => useOnlineStatus());
    
    expect(result.current.isOnline).toBe(false);
  });

  it('should update isOnline when online event is fired', () => {
    Object.defineProperty(window.navigator, 'onLine', {
      writable: true,
      value: false,
    });
    
    const { result } = renderHook(() => useOnlineStatus());
    
    expect(result.current.isOnline).toBe(false);
    
    act(() => {
      window.dispatchEvent(new Event('online'));
    });
    
    expect(result.current.isOnline).toBe(true);
  });

  it('should update isOnline when offline event is fired', () => {
    const { result } = renderHook(() => useOnlineStatus());
    
    expect(result.current.isOnline).toBe(true);
    
    act(() => {
      window.dispatchEvent(new Event('offline'));
    });
    
    expect(result.current.isOnline).toBe(false);
  });

  it('should set wasOffline to true when coming back online', () => {
    Object.defineProperty(window.navigator, 'onLine', {
      writable: true,
      value: false,
    });
    
    const { result } = renderHook(() => useOnlineStatus());
    
    expect(result.current.wasOffline).toBe(false);
    
    act(() => {
      window.dispatchEvent(new Event('online'));
    });
    
    expect(result.current.wasOffline).toBe(true);
  });

  it('should have initial wasOffline as false', () => {
    const { result } = renderHook(() => useOnlineStatus());
    
    expect(result.current.wasOffline).toBe(false);
  });
});