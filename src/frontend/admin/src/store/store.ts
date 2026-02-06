import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from './slices/authSlice';
import toastReducer from './slices/toastSlice';
import { authApi } from './api/authApi';
import { productsApi } from './api/productsApi';
import { ordersApi } from './api/ordersApi';
import { dashboardApi } from './api/dashboardApi';
import { customersApi } from './api/customersApi';
import { reviewsApi } from './api/reviewsApi';
import { promoCodesApi } from './api/promoCodesApi';
import { inventoryApi } from './api/inventoryApi';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    toast: toastReducer,
    [authApi.reducerPath]: authApi.reducer,
    [productsApi.reducerPath]: productsApi.reducer,
    [ordersApi.reducerPath]: ordersApi.reducer,
    [dashboardApi.reducerPath]: dashboardApi.reducer,
    [customersApi.reducerPath]: customersApi.reducer,
    [reviewsApi.reducerPath]: reviewsApi.reducer,
    [promoCodesApi.reducerPath]: promoCodesApi.reducer,
    [inventoryApi.reducerPath]: inventoryApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      authApi.middleware,
      productsApi.middleware,
      ordersApi.middleware,
      dashboardApi.middleware,
      customersApi.middleware,
      reviewsApi.middleware,
      promoCodesApi.middleware,
      inventoryApi.middleware
    ),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
