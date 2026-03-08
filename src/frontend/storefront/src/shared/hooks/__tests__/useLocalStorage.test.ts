import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useLocalStorage } from '../useLocalStorage';

describe('useLocalStorage', () => {
  const testKey = 'test-key';

  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should return initial value when localStorage is empty', () => {
    const { result } = renderHook(() => useLocalStorage(testKey, 'initial'));

    expect(result.current[0]).toBe('initial');
  });

  it('should return stored value when localStorage has value', () => {
    localStorage.setItem(testKey, JSON.stringify('stored'));

    const { result } = renderHook(() => useLocalStorage(testKey, 'initial'));

    expect(result.current[0]).toBe('stored');
  });

  it('should update value and persist to localStorage', () => {
    const { result } = renderHook(() => useLocalStorage(testKey, 'initial'));

    act(() => {
      result.current[1]('updated');
    });

    expect(result.current[0]).toBe('updated');
    expect(localStorage.getItem(testKey)).toBe(JSON.stringify('updated'));
  });

  it('should handle function updates', () => {
    const { result } = renderHook(() => useLocalStorage(testKey, 5));

    act(() => {
      result.current[1]((prev) => prev + 1);
    });

    expect(result.current[0]).toBe(6);
    expect(localStorage.getItem(testKey)).toBe(JSON.stringify(6));
  });

  it('should handle object values', () => {
    const initialValue = { name: 'test', count: 0 };
    const { result } = renderHook(() => useLocalStorage(testKey, initialValue));

    expect(result.current[0]).toEqual(initialValue);

    act(() => {
      result.current[1]({ name: 'updated', count: 1 });
    });

    expect(result.current[0]).toEqual({ name: 'updated', count: 1 });
    expect(JSON.parse(localStorage.getItem(testKey)!)).toEqual({ name: 'updated', count: 1 });
  });

  it('should handle array values', () => {
    const { result } = renderHook(() => useLocalStorage<string[]>(testKey, []));

    act(() => {
      result.current[1](['item1', 'item2']);
    });

    expect(result.current[0]).toEqual(['item1', 'item2']);
    expect(JSON.parse(localStorage.getItem(testKey)!)).toEqual(['item1', 'item2']);
  });

  it('should handle null values', () => {
    const { result } = renderHook(() => useLocalStorage<string | null>(testKey, null));

    expect(result.current[0]).toBeNull();

    act(() => {
      result.current[1]('not null');
    });

    expect(result.current[0]).toBe('not null');
  });

  it('should return initial value on JSON parse error', () => {
    localStorage.setItem(testKey, 'invalid-json{');

    // Mock console.warn to suppress error output
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

    const { result } = renderHook(() => useLocalStorage(testKey, 'initial'));

    expect(result.current[0]).toBe('initial');

    warnSpy.mockRestore();
  });

  it('should handle boolean values', () => {
    const { result } = renderHook(() => useLocalStorage(testKey, false));

    expect(result.current[0]).toBe(false);

    act(() => {
      result.current[1](true);
    });

    expect(result.current[0]).toBe(true);
    expect(JSON.parse(localStorage.getItem(testKey)!)).toBe(true);
  });
});
