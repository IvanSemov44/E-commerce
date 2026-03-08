import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useClickOutside } from '../useClickOutside';
import { useRef } from 'react';

describe('useClickOutside', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should call callback when clicking outside', () => {
    const callback = vi.fn();
    const { result } = renderHook(() => {
      const ref = useRef<HTMLDivElement>(null);
      useClickOutside(ref, callback);
      return ref;
    });

    const ref = result.current;
    const div = document.createElement('div');
    ref.current = div;

    act(() => {
      const event = new MouseEvent('mousedown', { bubbles: true });
      document.dispatchEvent(event);
    });

    expect(callback).toHaveBeenCalled();
  });

  it('should not call callback when clicking inside', () => {
    const callback = vi.fn();
    const { result } = renderHook(() => {
      const ref = useRef<HTMLDivElement>(null);
      useClickOutside(ref, callback);
      return ref;
    });

    const ref = result.current;
    const div = document.createElement('div');
    ref.current = div;
    const childElement = document.createElement('div');
    div.appendChild(childElement);

    act(() => {
      const event = new MouseEvent('mousedown', { bubbles: true });
      Object.defineProperty(event, 'target', { value: childElement, enumerable: true });
      document.dispatchEvent(event);
    });

    expect(callback).not.toHaveBeenCalled();
  });

  it('should clean up event listener on unmount', () => {
    const callback = vi.fn();
    const removeEventListenerSpy = vi.spyOn(document, 'removeEventListener');

    const { result, unmount } = renderHook(() => {
      const ref = useRef<HTMLDivElement>(null);
      useClickOutside(ref, callback);
      return ref;
    });

    const ref = result.current;
    const div = document.createElement('div');
    ref.current = div;

    unmount();

    expect(removeEventListenerSpy).toHaveBeenCalledWith('mousedown', expect.any(Function));
    removeEventListenerSpy.mockRestore();
  });

  it('should handle null ref gracefully', () => {
    const callback = vi.fn();
    renderHook(() => {
      const ref = useRef<HTMLDivElement>(null);
      useClickOutside(ref, callback);
      return ref;
    });

    // Don't set ref.current (stays null)
    act(() => {
      const event = new MouseEvent('mousedown', { bubbles: true });
      document.dispatchEvent(event);
    });

    // Callback should NOT be called when ref is null (defensive check)
    expect(callback).not.toHaveBeenCalled();
  });

  it('should work with nested elements', () => {
    const callback = vi.fn();
    const { result } = renderHook(() => {
      const ref = useRef<HTMLDivElement>(null);
      useClickOutside(ref, callback);
      return ref;
    });

    const ref = result.current;
    const outerDiv = document.createElement('div');
    const innerDiv = document.createElement('div');
    outerDiv.appendChild(innerDiv);
    ref.current = outerDiv;

    act(() => {
      const event = new MouseEvent('mousedown', { bubbles: true });
      Object.defineProperty(event, 'target', { value: innerDiv, enumerable: true });
      document.dispatchEvent(event);
    });

    expect(callback).not.toHaveBeenCalled();
  });
});
