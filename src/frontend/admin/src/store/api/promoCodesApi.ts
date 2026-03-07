import { createApi } from '@reduxjs/toolkit/query/react';
import type {
  PromoCode,
  PromoCodeDetail,
  CreatePromoCodeRequest,
  UpdatePromoCodeRequest,
  PaginatedResult,
  ApiResponse,
} from '@shared/types';
import { csrfBaseQuery } from '../../utils/apiFactory';

export const promoCodesApi = createApi({
  reducerPath: 'promoCodesApi',
  baseQuery: csrfBaseQuery,
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
    updatePromoCode: builder.mutation<PromoCodeDetail, { id: string } & UpdatePromoCodeRequest>({
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
      transformResponse: (response: ApiResponse<unknown>) => ({ success: response.success }),
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
