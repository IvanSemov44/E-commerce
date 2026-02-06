import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { config } from '../../config';
import type { ApiResponse } from '../../types';

export interface ValidatePromoCodeRequest {
  code: string;
  orderAmount: number;
}

export interface ValidatePromoCodeResponse {
  isValid: boolean;
  discountAmount: number;
  message: string;
}

export const promoCodeApi = createApi({
  reducerPath: 'promoCodeApi',
  baseQuery: fetchBaseQuery({
    baseUrl: config.api.baseUrl,
  }),
  endpoints: (builder) => ({
    validatePromoCode: builder.mutation<ValidatePromoCodeResponse, ValidatePromoCodeRequest>({
      query: (body) => ({
        url: '/promo-codes/validate',
        method: 'POST',
        body,
      }),
      transformResponse: (response: ApiResponse<ValidatePromoCodeResponse>) =>
        response.data || { isValid: false, discountAmount: 0, message: 'Invalid response' },
    }),
  }),
});

export const { useValidatePromoCodeMutation } = promoCodeApi;
