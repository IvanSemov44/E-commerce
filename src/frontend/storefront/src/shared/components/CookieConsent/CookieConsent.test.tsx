import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { render, screen, act, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import CookieConsent from './CookieConsent';

const CONSENT_KEY = 'cookie-consent';

function renderCookieConsent() {
  return render(
    <MemoryRouter>
      <CookieConsent />
    </MemoryRouter>
  );
}

describe('CookieConsent', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    localStorage.clear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('does not render immediately before delay', () => {
    renderCookieConsent();
    expect(screen.queryByText('🍪 Cookie Preferences')).not.toBeInTheDocument();
  });

  it('renders after delay when no saved consent', () => {
    renderCookieConsent();

    act(() => {
      vi.advanceTimersByTime(1000);
    });

    expect(screen.getByText('🍪 Cookie Preferences')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Accept All' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Decline' })).toBeInTheDocument();
  });

  it('does not render when consent already accepted', () => {
    localStorage.setItem(CONSENT_KEY, 'accepted');
    renderCookieConsent();

    act(() => {
      vi.runAllTimers();
    });

    expect(screen.queryByText('🍪 Cookie Preferences')).not.toBeInTheDocument();
  });

  it('accepts consent and hides banner', () => {
    renderCookieConsent();

    act(() => {
      vi.advanceTimersByTime(1000);
    });

    fireEvent.click(screen.getByRole('button', { name: 'Accept All' }));

    expect(localStorage.getItem(CONSENT_KEY)).toBe('accepted');
    expect(screen.queryByText('🍪 Cookie Preferences')).not.toBeInTheDocument();
  });

  it('declines consent and hides banner', () => {
    renderCookieConsent();

    act(() => {
      vi.advanceTimersByTime(1000);
    });

    fireEvent.click(screen.getByRole('button', { name: 'Decline' }));

    expect(localStorage.getItem(CONSENT_KEY)).toBe('declined');
    expect(screen.queryByText('🍪 Cookie Preferences')).not.toBeInTheDocument();
  });

  it('renders privacy policy link', () => {
    renderCookieConsent();

    act(() => {
      vi.advanceTimersByTime(1000);
    });

    const link = screen.getByRole('link', { name: 'Privacy Policy' });
    expect(link).toHaveAttribute('href', '/privacy');
  });
});
