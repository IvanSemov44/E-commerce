import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { UserProfile, UpdateProfileRequest, ApiResponse } from '../../types';
import { config } from '../../config';

export const profileApi = createApi({
  reducerPath: 'profileApi',
  baseQuery: fetchBaseQuery({
    baseUrl: config.api.baseUrl,
    prepareHeaders: (headers) => {
      if (typeof window !== 'undefined') {
        const token = localStorage.getItem(config.storage.authToken);
        if (token) {
          headers.set('Authorization', `Bearer ${token}`);
        }
      }
      return headers;
    },
  }),
  tagTypes: ['Profile'],
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

export const {
  useGetProfileQuery,
  useUpdateProfileMutation,
} = profileApi;
