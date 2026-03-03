import type { ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

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

const inventoryApiSlice = baseApi.injectEndpoints({
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

export const { useCheckAvailabilityMutation } = inventoryApiSlice;
