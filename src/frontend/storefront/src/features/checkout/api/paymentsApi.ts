import type { ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

export interface PaymentMethodsResponse {
  methods: string[];
}

const paymentsApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getPaymentMethods: builder.query<PaymentMethodsResponse, void>({
      query: () => '/payments/methods',
      transformResponse: (response: ApiResponse<PaymentMethodsResponse>) =>
        response.data || { methods: [] },
    }),
  }),
});

export const { useGetPaymentMethodsQuery } = paymentsApiSlice;
