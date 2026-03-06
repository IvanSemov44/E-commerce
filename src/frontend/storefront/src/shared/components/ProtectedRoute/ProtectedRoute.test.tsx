import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import ProtectedRoute from './ProtectedRoute';
import { authSlice } from '@/features/auth/slices/authSlice';

vi.mock('@/shared/lib/store', () => ({
  useAppSelector: vi.fn(),
  useAppDispatch: vi.fn(),
}));

const { useAppSelector } = await import('@/shared/lib/store');

interface AuthState {
  isAuthenticated: boolean;
  loading: boolean;
  user: any;
  error: null;
  initialized: boolean;
}

const createMockStore = (authState: AuthState) => {
  return configureStore({
    reducer: {
      auth: () => authState,
    },
  });
};

const renderWithRouter = (component: React.ReactNode, store: any) => {
  return render(
    <Provider store={store}>
      <BrowserRouter>
        {component}
      </BrowserRouter>
    </Provider>
  );
};

describe('ProtectedRoute', () => {
  it('renders children when user is authenticated', () => {
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: true,
      loading: false,
      user: { id: '1', email: 'test@test.com' },
      error: null,
      initialized: true,
    });

    const store = createMockStore({
      isAuthenticated: true,
      loading: false,
      user: { id: '1', email: 'test@test.com' },
      error: null,
      initialized: true,
    });

    renderWithRouter(
      <Routes>
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <div>Protected Content</div>
            </ProtectedRoute>
          }
        />
        <Route path="/login" element={<div>Login Page</div>} />
      </Routes>,
      store
    );

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('shows loading spinner when auth is loading', () => {
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: false,
      loading: true,
      user: null,
      error: null,
      initialized: false,
    });

    const store = createMockStore({
      isAuthenticated: false,
      loading: true,
      user: null,
      error: null,
      initialized: false,
    });

    renderWithRouter(
      <ProtectedRoute>
        <div>Protected Content</div>
      </ProtectedRoute>,
      store
    );

    const spinner = document.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('redirects to login when user is not authenticated', () => {
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: false,
      loading: false,
      user: null,
      error: null,
      initialized: true,
    });

    const store = createMockStore({
      isAuthenticated: false,
      loading: false,
      user: null,
      error: null,
      initialized: true,
    });

    renderWithRouter(
      <Routes>
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <div>Protected Content</div>
            </ProtectedRoute>
          }
        />
        <Route path="/login" element={<div>Login Page</div>} />
      </Routes>,
      store
    );

    // Should not see protected content
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    // Should navigate to login
    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });

  it('does not show protected content when not authenticated', () => {
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: false,
      loading: false,
      user: null,
      error: null,
      initialized: true,
    });

    const store = createMockStore({
      isAuthenticated: false,
      loading: false,
      user: null,
      error: null,
      initialized: true,
    });

    renderWithRouter(
      <ProtectedRoute>
        <div>Secret Data</div>
      </ProtectedRoute>,
      store
    );

    expect(screen.queryByText('Secret Data')).not.toBeInTheDocument();
  });

  it('handles multiple children elements', () => {
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: true,
      loading: false,
      user: { id: '1', email: 'test@test.com' },
      error: null,
      initialized: true,
    });

    const store = createMockStore({
      isAuthenticated: true,
      loading: false,
      user: { id: '1', email: 'test@test.com' },
      error: null,
      initialized: true,
    });

    renderWithRouter(
      <ProtectedRoute>
        <div>Content 1</div>
        <div>Content 2</div>
        <div>Content 3</div>
      </ProtectedRoute>,
      store
    );

    expect(screen.getByText('Content 1')).toBeInTheDocument();
    expect(screen.getByText('Content 2')).toBeInTheDocument();
    expect(screen.getByText('Content 3')).toBeInTheDocument();
  });

  it('shows spinner while initializing auth', () => {
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: false,
      loading: true,
      user: null,
      error: null,
      initialized: false,
    });

    const store = createMockStore({
      isAuthenticated: false,
      loading: true,
      user: null,
      error: null,
      initialized: false,
    });

    const { container } = renderWithRouter(
      <ProtectedRoute>
        <div>Protected</div>
      </ProtectedRoute>,
      store
    );

    // Should show loading spinner
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('transitions from loading to authenticated', () => {
    const { rerender } = render(
      <Provider store={createMockStore({
        isAuthenticated: false,
        loading: true,
        user: null,
        error: null,
        initialized: false,
      })}>
        <BrowserRouter>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      </Provider>
    );

    // Initially loading
    const spinner = document.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();

    // Update to authenticated
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: true,
      loading: false,
      user: { id: '1', email: 'test@test.com' },
      error: null,
      initialized: true,
    });

    rerender(
      <Provider store={createMockStore({
        isAuthenticated: true,
        loading: false,
        user: { id: '1', email: 'test@test.com' },
        error: null,
        initialized: true,
      })}>
        <BrowserRouter>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('transitions from loading to unauthenticated', () => {
    vi.mocked(useAppSelector)
      .mockReturnValueOnce({
        isAuthenticated: false,
        loading: true,
        user: null,
        error: null,
        initialized: false,
      })
      .mockReturnValue({
        isAuthenticated: false,
        loading: false,
        user: null,
        error: null,
        initialized: true,
      });

    const { rerender } = render(
      <Provider store={createMockStore({
        isAuthenticated: false,
        loading: true,
        user: null,
        error: null,
        initialized: false,
      })}>
        <BrowserRouter>
          <Routes>
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <div>Protected Content</div>
                </ProtectedRoute>
              }
            />
            <Route path="/login" element={<div>Login Page</div>} />
          </Routes>
        </BrowserRouter>
      </Provider>
    );

    // Re-render unauthenticated state
    rerender(
      <Provider store={createMockStore({
        isAuthenticated: false,
        loading: false,
        user: null,
        error: null,
        initialized: true,
      })}>
        <BrowserRouter>
          <Routes>
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <div>Protected Content</div>
                </ProtectedRoute>
              }
            />
            <Route path="/login" element={<div>Login Page</div>} />
          </Routes>
        </BrowserRouter>
      </Provider>
    );

    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });

  it('has correct spinner styling', () => {
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: false,
      loading: true,
      user: null,
      error: null,
      initialized: false,
    });

    const store = createMockStore({
      isAuthenticated: false,
      loading: true,
      user: null,
      error: null,
      initialized: true,
    });

    const { container } = renderWithRouter(
      <ProtectedRoute>
        <div>Protected</div>
      </ProtectedRoute>,
      store
    );

    const spinnerContainer = container.querySelector('.flex.items-center.justify-center');
    expect(spinnerContainer).toHaveClass('min-h-screen');
  });

  it('renders null children (edge case)', () => {
    vi.mocked(useAppSelector).mockReturnValue({
      isAuthenticated: true,
      loading: false,
      user: { id: '1', email: 'test@test.com' },
      error: null,
      initialized: true,
    });

    const store = createMockStore({
      isAuthenticated: true,
      loading: false,
      user: { id: '1', email: 'test@test.com' },
      error: null,
      initialized: true,
    });

    const { container } = renderWithRouter(
      <ProtectedRoute>
      </ProtectedRoute>,
      store
    );

    expect(container).toBeInTheDocument();
  });
});
