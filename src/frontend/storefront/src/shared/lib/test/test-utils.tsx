import type { ReactElement } from 'react';
import {
  render,
  renderHook,
  type RenderOptions,
  type RenderHookOptions,
} from '@testing-library/react';
import { BrowserRouter } from 'react-router';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from '@/features/auth/slices/authSlice';
import { cartReducer } from '@/features/cart/slices/cartSlice';
import toastReducer from '@/app/Toast/toastSlice';
import { baseApi } from '@/shared/lib/api/baseApi';

interface ExtendedRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  preloadedState?: any;
  withRouter?: boolean;
  withRedux?: boolean;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function setupStore(preloadedState?: any) {
  return configureStore({
    reducer: {
      auth: authReducer,
      cart: cartReducer,
      toast: toastReducer,
      [baseApi.reducerPath]: baseApi.reducer,
    },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    middleware: (getDefaultMiddleware: any) => getDefaultMiddleware().concat(baseApi.middleware),
    preloadedState,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
  } as any);
}

export function renderWithProviders(
  ui: ReactElement,
  {
    preloadedState,
    withRouter = true,
    withRedux = true,
    ...renderOptions
  }: ExtendedRenderOptions = {}
) {
  const store = setupStore(preloadedState);

  let Wrapper = ({ children }: { children: React.ReactNode }) => <>{children}</>;

  if (withRedux && withRouter) {
    Wrapper = ({ children }: { children: React.ReactNode }) => (
      <Provider store={store}>
        <BrowserRouter>{children}</BrowserRouter>
      </Provider>
    );
  } else if (withRedux) {
    Wrapper = ({ children }: { children: React.ReactNode }) => (
      <Provider store={store}>{children}</Provider>
    );
  } else if (withRouter) {
    Wrapper = ({ children }: { children: React.ReactNode }) => (
      <BrowserRouter>{children}</BrowserRouter>
    );
  }

  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) };
}

export function renderHookWithProviders<TProps, TResult>(
  hook: (props: TProps) => TResult,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  options: RenderHookOptions<TProps> & { preloadedState?: any; withRouter?: boolean } = {}
) {
  const { preloadedState, withRouter = false, ...renderOptions } = options;
  const store = setupStore(preloadedState);

  let Wrapper = ({ children }: { children: React.ReactNode }) => (
    <Provider store={store}>{children}</Provider>
  );

  if (withRouter) {
    Wrapper = ({ children }: { children: React.ReactNode }) => (
      <Provider store={store}>
        <BrowserRouter>{children}</BrowserRouter>
      </Provider>
    );
  }

  return { store, ...renderHook(hook, { wrapper: Wrapper, ...renderOptions }) };
}

// Re-export everything from @testing-library/react
// eslint-disable-next-line react-refresh/only-export-components
export * from '@testing-library/react';

// Override render with our custom version
export { renderWithProviders as render };
