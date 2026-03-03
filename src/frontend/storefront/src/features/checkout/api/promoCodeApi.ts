import type { ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

export interface ValidatePromoCodeRequest {
  code: string;
  orderAmount: number;
}

export interface ValidatePromoCodeResponse {
  isValid: boolean;
  discountAmount: number;
  message: string;
}

const promoCodeApiSlice = baseApi.injectEndpoints({
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

export const { useValidatePromoCodeMutation } = promoCodeApiSlice;
