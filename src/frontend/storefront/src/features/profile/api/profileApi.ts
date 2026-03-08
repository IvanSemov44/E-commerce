import type { UserProfile, UpdateProfileRequest, ApiResponse } from '@/shared/types';
import { baseApi } from '@/shared/lib/api/baseApi';

const profileApiSlice = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getProfile: builder.query<UserProfile, void>({
      query: () => '/profile',
      transformResponse: (response: ApiResponse<UserProfile>) =>
        response.data || ({} as UserProfile),
      providesTags: ['Profile'],
    }),

    updateProfile: builder.mutation<UserProfile, UpdateProfileRequest>({
      query: (data) => ({
        url: '/profile',
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: ApiResponse<UserProfile>) =>
        response.data || ({} as UserProfile),
      invalidatesTags: ['Profile'],
    }),
  }),
});

export const { useGetProfileQuery, useUpdateProfileMutation } = profileApiSlice;
