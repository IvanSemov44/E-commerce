import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { config } from '../../config';
import type { ApiResponse } from '../../types';

export interface CheckAvailabilityItem {
  productId: string;
  quantity: number;
}

export interface CheckAvailabilityRequest {
  items: CheckAvailabilityItem[];
}

export interface StockIssue {
  productId: string;
  productName: string;
  message: string;
  requestedQuantity: number;
  availableQuantity: number;
}

export interface CheckAvailabilityResponse {
  isAvailable: boolean;
  issues: StockIssue[];
}

export const inventoryApi = createApi({
  reducerPath: 'inventoryApi',
  baseQuery: fetchBaseQuery({
    baseUrl: config.api.baseUrl,
  }),
  endpoints: (builder) => ({
    checkAvailability: builder.mutation<CheckAvailabilityResponse, CheckAvailabilityRequest>({
      query: (body) => ({
        url: '/inventory/check-availability',
        method: 'POST',
        body,
      }),
      transformResponse: (response: ApiResponse<CheckAvailabilityResponse>) =>
        response.data || { isAvailable: false, issues: [] },
    }),
  }),
});

export const { useCheckAvailabilityMutation } = inventoryApi;
