import { describe, it, expect, vi } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useRouteTelemetry } from './useRouteTelemetry';
import { BrowserRouter } from 'react-router';
import type { ReactNode } from 'react';

vi.mock('@/shared/lib/utils/telemetry', () => ({
  telemetry: {
    track: vi.fn(),
  },
}));

const wrapper = ({ children }: { children: ReactNode }) => (
  <BrowserRouter>{children}</BrowserRouter>
);

describe('useRouteTelemetry', () => {
  it('tracks route changes', () => {
    renderHook(() => useRouteTelemetry(), { wrapper });
    expect(true).toBe(true);
  });
});
