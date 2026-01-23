import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from './slices/authSlice';
import { authApi } from './api/authApi';
import { productsApi } from './api/productsApi';
import { ordersApi } from './api/ordersApi';
import { dashboardApi } from './api/dashboardApi';
import { customersApi } from './api/customersApi';
import { reviewsApi } from './api/reviewsApi';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    [authApi.reducerPath]: authApi.reducer,
    [productsApi.reducerPath]: productsApi.reducer,
    [ordersApi.reducerPath]: ordersApi.reducer,
    [dashboardApi.reducerPath]: dashboardApi.reducer,
    [customersApi.reducerPath]: customersApi.reducer,
    [reviewsApi.reducerPath]: reviewsApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      authApi.middleware,
      productsApi.middleware,
      ordersApi.middleware,
      dashboardApi.middleware,
      customersApi.middleware,
      reviewsApi.middleware
    ),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
