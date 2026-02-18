import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type {
  PromoCode,
  PromoCodeDetail,
  CreatePromoCodeRequest,
  UpdatePromoCodeRequest,
  PaginatedResult,
  ApiResponse,
} from '@shared/types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

/**
 * Helper function to get CSRF token from cookie
 */
const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
};

export const promoCodesApi = createApi({
  reducerPath: 'promoCodesApi',
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
  tagTypes: ['PromoCode'],
  endpoints: (builder) => ({
    getPromoCodes: builder.query<
      PaginatedResult<PromoCode>,
      { page?: number; pageSize?: number; search?: string; isActive?: boolean }
    >({
      query: ({ page = 1, pageSize = 20, search, isActive }) => {
        const params = new URLSearchParams();
        params.set('page', page.toString());
        params.set('pageSize', pageSize.toString());
        if (search) params.set('search', search);
        if (isActive !== undefined) params.set('isActive', isActive.toString());
        return `/promo-codes?${params}`;
      },
      transformResponse: (response: ApiResponse<PaginatedResult<PromoCode>>) =>
        response.data || { items: [], totalCount: 0, page: 1, pageSize: 20 },
      providesTags: ['PromoCode'],
    }),
    getPromoCodeById: builder.query<PromoCodeDetail, string>({
      query: (id) => `/promo-codes/${id}`,
      transformResponse: (response: ApiResponse<PromoCodeDetail>) => response.data!,
      providesTags: ['PromoCode'],
    }),
    createPromoCode: builder.mutation<PromoCodeDetail, CreatePromoCodeRequest>({
      query: (promoCode) => ({
        url: '/promo-codes',
        method: 'POST',
        body: promoCode,
      }),
      transformResponse: (response: ApiResponse<PromoCodeDetail>) => response.data!,
      invalidatesTags: ['PromoCode'],
    }),
    updatePromoCode: builder.mutation<
      PromoCodeDetail,
      { id: string } & UpdatePromoCodeRequest
    >({
      query: ({ id, ...promoCode }) => ({
        url: `/promo-codes/${id}`,
        method: 'PUT',
        body: promoCode,
      }),
      transformResponse: (response: ApiResponse<PromoCodeDetail>) => response.data!,
      invalidatesTags: ['PromoCode'],
    }),
    deactivatePromoCode: builder.mutation<{ success: boolean }, string>({
      query: (id) => ({
        url: `/promo-codes/${id}/deactivate`,
        method: 'PUT',
      }),
      transformResponse: (response: ApiResponse<any>) => ({ success: response.success }),
      invalidatesTags: ['PromoCode'],
    }),
  }),
});

export const {
  useGetPromoCodesQuery,
  useGetPromoCodeByIdQuery,
  useCreatePromoCodeMutation,
  useUpdatePromoCodeMutation,
  useDeactivatePromoCodeMutation,
} = promoCodesApi;
