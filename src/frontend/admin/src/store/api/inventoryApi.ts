import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

/**
 * Helper function to get CSRF token from cookie
 */
const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

export interface InventoryItem {
  productId: string;
  productName: string;
  sku?: string;
  stockQuantity: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  isOutOfStock: boolean;
  imageUrl?: string;
  price: number;
}

export interface InventoryLog {
  id: string;
  productId: string;
  productName: string;
  quantityChange: number;
  stockAfterChange: number;
  reason: string;
  referenceId?: string;
  notes?: string;
  createdAt: string;
  createdByUserName?: string;
}

export interface AdjustStockRequest {
  quantity: number;
  reason: string;
  notes?: string;
}

export interface LowStockAlert {
  productId: string;
  productName: string;
  sku?: string;
  currentStock: number;
  lowStockThreshold: number;
}

export const inventoryApi = createApi({
  reducerPath: 'inventoryApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_URL,
    credentials: 'include', // Required for httpOnly cookies to be sent
    prepareHeaders: (headers) => {
      // Add CSRF token header for state-changing requests
      const csrfToken = getCsrfToken();
      if (csrfToken) {
        headers.set('X-XSRF-TOKEN', csrfToken);
      }
      return headers;
    },
  }),
  tagTypes: ['Inventory', 'InventoryHistory', 'LowStock'],
  endpoints: (builder) => ({
    getInventory: builder.query<
      { success: boolean; data: InventoryItem[]; message: string },
      { page?: number; pageSize?: number; search?: string; lowStockOnly?: boolean }
    >({
      query: ({ page = 1, pageSize = 50, search, lowStockOnly }) => {
        const params = new URLSearchParams({
          page: page.toString(),
          pageSize: pageSize.toString(),
        });
        if (search) params.append('search', search);
        if (lowStockOnly !== undefined) params.append('lowStockOnly', lowStockOnly.toString());
        return `/inventory?${params.toString()}`;
      },
      providesTags: ['Inventory'],
    }),

    getLowStockProducts: builder.query<
      { success: boolean; data: LowStockAlert[]; count: number; message: string },
      void
    >({
      query: () => '/inventory/low-stock',
      providesTags: ['LowStock'],
    }),

    getInventoryHistory: builder.query<
      { success: boolean; data: InventoryLog[]; message: string },
      { productId: string; page?: number; pageSize?: number }
    >({
      query: ({ productId, page = 1, pageSize = 50 }) => {
        const params = new URLSearchParams({
          page: page.toString(),
          pageSize: pageSize.toString(),
        });
        return `/inventory/${productId}/history?${params.toString()}`;
      },
      providesTags: (_result, _error, { productId }) => [
        { type: 'InventoryHistory', id: productId },
      ],
    }),

    adjustStock: builder.mutation<
      { success: boolean; message: string; data: Record<string, unknown> },
      { productId: string; request: AdjustStockRequest }
    >({
      query: ({ productId, request }) => ({
        url: `/inventory/${productId}/adjust`,
        method: 'POST',
        body: request,
      }),
      invalidatesTags: ['Inventory', 'LowStock'],
    }),

    restockProduct: builder.mutation<
      { success: boolean; message: string; data: Record<string, unknown> },
      { productId: string; request: AdjustStockRequest }
    >({
      query: ({ productId, request }) => ({
        url: `/inventory/${productId}/restock`,
        method: 'POST',
        body: request,
      }),
      invalidatesTags: ['Inventory', 'LowStock'],
    }),
  }),
});

export const {
  useGetInventoryQuery,
  useGetLowStockProductsQuery,
  useGetInventoryHistoryQuery,
  useAdjustStockMutation,
  useRestockProductMutation,
} = inventoryApi;
