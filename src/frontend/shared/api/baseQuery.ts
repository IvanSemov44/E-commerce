import { fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { FetchBaseQueryError, FetchArgs } from '@reduxjs/toolkit/query/react';
import type { BaseQueryApi } from '@reduxjs/toolkit/query';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

export const baseQuery = fetchBaseQuery({
  baseUrl: API_URL,
  prepareHeaders: (headers) => {
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('authToken');
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
    }
    headers.set('Content-Type', 'application/json');
    return headers;
  },
});

export const baseQueryWithReauth = async (args: string | FetchArgs, api: BaseQueryApi, extraOptions: {}) => {
  let result = await baseQuery(args, api, extraOptions);

  if (result.error && (result.error as FetchBaseQueryError).status === 401) {
    // Handle token refresh or logout
    if (typeof window !== 'undefined') {
      localStorage.removeItem('authToken');
      // TODO: Dispatch logout action or redirect to login
    }
  }

  return result;
};
