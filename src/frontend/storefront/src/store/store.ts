import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from './slices/authSlice';
import { cartReducer } from './slices/cartSlice';
import { productApi } from './api/productApi';
import { authApi } from './api/authApi';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    cart: cartReducer,
    [productApi.reducerPath]: productApi.reducer,
    [authApi.reducerPath]: authApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      productApi.middleware,
      authApi.middleware
    ),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
