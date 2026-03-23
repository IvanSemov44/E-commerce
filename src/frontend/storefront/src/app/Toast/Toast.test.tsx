import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { Toast } from './Toast';

afterEach(() => {
  vi.useRealTimers();
});

describe('Toast', () => {
  it('renders message and variant icon', () => {
    render(<Toast toast={{ id: '1', message: 'Saved', variant: 'success' }} onDismiss={vi.fn()} />);

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('Saved')).toBeInTheDocument();
    expect(screen.getByText('✓')).toBeInTheDocument();
  });

  it('dismisses on close button click', () => {
    const onDismiss = vi.fn();
    render(<Toast toast={{ id: '1', message: 'Error', variant: 'error' }} onDismiss={onDismiss} />);

    fireEvent.click(screen.getByRole('button', { name: 'Dismiss notification' }));
    expect(onDismiss).toHaveBeenCalledWith('1');
  });

  it('auto dismisses when duration is set', () => {
    vi.useFakeTimers();
    const onDismiss = vi.fn();

    render(
      <Toast
        toast={{ id: '1', message: 'Info', variant: 'info', duration: 1000 }}
        onDismiss={onDismiss}
      />
    );

    act(() => {
      vi.advanceTimersByTime(1000);
    });

    expect(onDismiss).toHaveBeenCalledWith('1');
  });

  it('does not auto dismiss when duration is zero', () => {
    vi.useFakeTimers();
    const onDismiss = vi.fn();

    render(
      <Toast
        toast={{ id: '1', message: 'Warn', variant: 'warning', duration: 0 }}
        onDismiss={onDismiss}
      />
    );

    act(() => {
      vi.advanceTimersByTime(5000);
    });

    expect(onDismiss).not.toHaveBeenCalled();
  });
});
