import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useNewsletterSubscription } from './useNewsletterSubscription';

// Mock useToast
vi.mock('@/app/Toast', () => ({
  useToast: () => ({
    success: vi.fn(),
    error: vi.fn(),
  }),
}));

describe('useNewsletterSubscription', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('returns initial state', () => {
    const { result } = renderHook(() =>
      useNewsletterSubscription({
        invalidEmailMessage: 'Invalid email',
        subscribeSuccessMessage: 'Subscribed!',
        alreadySubscribedMessage: 'Already subscribed',
        subscribeFailedMessage: 'Failed',
      })
    );

    expect(result.current.email).toBe('');
    expect(result.current.isSubmitting).toBe(false);
  });

  it('updates email state', () => {
    const { result } = renderHook(() =>
      useNewsletterSubscription({
        invalidEmailMessage: 'Invalid email',
        subscribeSuccessMessage: 'Subscribed!',
        alreadySubscribedMessage: 'Already subscribed',
        subscribeFailedMessage: 'Failed',
      })
    );

    act(() => {
      result.current.setEmail('test@example.com');
    });

    expect(result.current.email).toBe('test@example.com');
  });

  it('sets isSubmitting during submission', async () => {
    const { result } = renderHook(() =>
      useNewsletterSubscription({
        invalidEmailMessage: 'Invalid email',
        subscribeSuccessMessage: 'Subscribed!',
        alreadySubscribedMessage: 'Already subscribed',
        subscribeFailedMessage: 'Failed',
      })
    );

    act(() => {
      result.current.setEmail('test@example.com');
    });

    const formEvent = { preventDefault: vi.fn() } as unknown as React.FormEvent;

    // Start the submission
    act(() => {
      result.current.handleNewsletterSubmit(formEvent);
    });

    // isSubmitting should be true during submission
    expect(result.current.isSubmitting).toBe(true);
  });
});
