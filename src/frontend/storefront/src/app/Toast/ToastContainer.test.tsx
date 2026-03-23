import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ToastContainer } from './ToastContainer';

const dispatchMock = vi.fn();
const useAppSelectorMock = vi.fn();

vi.mock('@/shared/lib/store', () => ({
  useAppSelector: (selector: (state: unknown) => unknown) => useAppSelectorMock(selector),
  useAppDispatch: () => dispatchMock,
}));

vi.mock('./Toast', () => ({
  Toast: ({
    toast,
    onDismiss,
  }: {
    toast: { id: string; message: string };
    onDismiss: (id: string) => void;
  }) => (
    <div>
      <span>{toast.message}</span>
      <button onClick={() => onDismiss(toast.id)}>dismiss-{toast.id}</button>
    </div>
  ),
}));

describe('ToastContainer', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders toasts from state', () => {
    useAppSelectorMock.mockImplementation((selector) =>
      selector({
        toast: {
          toasts: [
            { id: '1', message: 'Saved', variant: 'success' },
            { id: '2', message: 'Failed', variant: 'error' },
          ],
        },
      })
    );

    render(<ToastContainer />);

    expect(screen.getByText('Saved')).toBeInTheDocument();
    expect(screen.getByText('Failed')).toBeInTheDocument();
  });

  it('dispatches removeToast when toast dismisses', () => {
    useAppSelectorMock.mockImplementation((selector) =>
      selector({
        toast: {
          toasts: [{ id: '1', message: 'Saved', variant: 'success' }],
        },
      })
    );

    render(<ToastContainer />);

    fireEvent.click(screen.getByRole('button', { name: 'dismiss-1' }));

    expect(dispatchMock).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'toast/removeToast', payload: '1' })
    );
  });

  it('renders empty container when no toasts', () => {
    useAppSelectorMock.mockImplementation((selector) =>
      selector({
        toast: {
          toasts: [],
        },
      })
    );

    const { container } = render(<ToastContainer />);
    expect(container.firstChild).toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
