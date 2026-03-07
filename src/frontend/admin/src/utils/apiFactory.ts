import { fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { getCsrfToken } from './csrf';

export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export const csrfBaseQuery = fetchBaseQuery({
  baseUrl: API_URL,
  credentials: 'include',
  prepareHeaders: (headers) => {
    const csrfToken = getCsrfToken();
    if (csrfToken) {
      headers.set('X-XSRF-TOKEN', csrfToken);
    }
    return headers;
  },
});
