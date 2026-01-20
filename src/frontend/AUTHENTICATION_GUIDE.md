# Frontend Authentication Guide

This document describes the authentication system for both storefront and admin applications.

## Overview

Both applications use:
- **JWT Tokens** for authentication
- **Redux** for state management
- **localStorage** for token persistence
- **Protected Routes** for access control
- **RTK Query** for API calls

## Authentication Flow

### 1. Login Flow

```
User enters email/password
        ↓
RTK Query mutation sends credentials to backend
        ↓
Backend returns JWT token + user data
        ↓
Token stored in localStorage
        ↓
User data stored in Redux
        ↓
User redirected to dashboard
```

### 2. Subsequent Requests

```
All API calls automatically include JWT token
        ↓
Backend validates token
        ↓
Request processed or 401 returned
        ↓
If 401: User logged out, redirected to login
```

### 3. Logout Flow

```
User clicks logout
        ↓
Redux state cleared
        ↓
localStorage token removed
        ↓
User redirected to login
```

## Setup

### Install Dependencies

**Storefront:**
```bash
cd src/frontend/storefront
npm install
```

**Admin:**
```bash
cd src/frontend/admin
npm install
```

### Environment Variables

Create `.env` file in each application:

**`src/frontend/storefront/.env`:**
```env
VITE_API_URL=http://localhost:5000/api/v1
```

**`src/frontend/admin/.env`:**
```env
VITE_API_URL=http://localhost:5000/api/v1
```

## Storefront Authentication

### Auth API Endpoints

Located in: `src/frontend/storefront/src/store/api/authApi.ts`

```typescript
export const authApi = createApi({
  endpoints: (builder) => ({
    login: builder.mutation<AuthResponse, LoginRequest>({
      query: (credentials) => ({
        url: '/auth/login',
        method: 'POST',
        body: credentials,
      }),
    }),
    register: builder.mutation<AuthResponse, RegisterRequest>({
      query: (credentials) => ({
        url: '/auth/register',
        method: 'POST',
        body: credentials,
      }),
    }),
    refreshToken: builder.mutation<AuthResponse, string>({...}),
  }),
});
```

### Auth State Management

Located in: `src/frontend/storefront/src/store/slices/authSlice.ts`

```typescript
export interface AuthState {
  isAuthenticated: boolean;
  user: AuthUser | null;
  token: string | null;
  loading: boolean;
  error: string | null;
}

export const authSlice = createSlice({
  reducers: {
    loginStart,
    loginSuccess,
    loginFailure,
    logout,
    clearError,
  },
});
```

### Login Page

Located in: `src/frontend/storefront/src/pages/Login.tsx`

**Flow:**
1. User enters email and password
2. Click login button
3. `useLoginMutation` calls backend
4. On success:
   - Dispatch `loginSuccess` action
   - Token saved to Redux and localStorage
   - User redirected to home page
5. On error:
   - Display error message
   - Clear form

**Usage Example:**
```typescript
import { useLoginMutation } from '../store/api/authApi';
import { useAppDispatch } from '../store/hooks';
import { loginSuccess } from '../store/slices/authSlice';

function Login() {
  const [login, { isLoading }] = useLoginMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const response = await login({ email, password }).unwrap();
      dispatch(loginSuccess({ user: response.user, token: response.token }));
      navigate('/');
    } catch (err) {
      setError(err?.data?.message);
    }
  };

  return <form onSubmit={handleSubmit}>...</form>;
}
```

### Register Page

Located in: `src/frontend/storefront/src/pages/Register.tsx`

**Features:**
- First name and last name fields
- Email validation
- Password confirmation
- Same flow as login after successful registration

### Protected Routes

Located in: `src/frontend/storefront/src/components/ProtectedRoute.tsx`

```typescript
export default function ProtectedRoute({ children }) {
  const { isAuthenticated } = useAppSelector((state) => state.auth);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
```

**Usage in App:**
```typescript
<Route path="/checkout" element={<ProtectedRoute><Checkout /></ProtectedRoute>} />
```

### Token Persistence

Token is automatically:
- Saved to `localStorage` on login
- Retrieved on app reload
- Included in all API requests via `authApi.baseQuery`

```typescript
const baseQuery = fetchBaseQuery({
  baseUrl: API_URL,
  prepareHeaders: (headers) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  },
});
```

## Admin Authentication

### Auth API Endpoints

Located in: `src/frontend/admin/src/store/api/authApi.ts`

Same endpoints as storefront, but accessible only to admin users.

### Auth State Management

Located in: `src/frontend/admin/src/store/slices/authSlice.ts`

```typescript
export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'admin' | 'superadmin';
  avatarUrl?: string;
}
```

Role-based access control via `role` field.

### Login Page

Located in: `src/frontend/admin/src/pages/Login.tsx`

**Features:**
- Admin-only login interface
- Blue theme matching admin dashboard
- Professional login card design
- Gradient background
- Error handling with warning icon

### Protected Routes with Role Check

Located in: `src/frontend/admin/src/components/ProtectedRoute.tsx`

```typescript
export default function ProtectedRoute({ children, requiredRole }) {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole && user?.role !== requiredRole && user?.role !== 'superadmin') {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
```

**Usage with Role Check:**
```typescript
<Route
  path="/admin/settings"
  element={
    <ProtectedRoute requiredRole="superadmin">
      <Settings />
    </ProtectedRoute>
  }
/>
```

### Header with User Info

Located in: `src/frontend/admin/src/components/Header.tsx`

**Features:**
- Displays logged-in user's name and email
- User avatar with first letter
- Dropdown menu with profile and logout
- Notification bell icon
- Dynamic user display from Redux state

### Logout

Located in: `src/frontend/admin/src/components/Header.tsx`

```typescript
const handleLogout = () => {
  dispatch(logout());
  setUserMenuOpen(false);
  navigate('/login');
};
```

## Common Patterns

### Check Authentication Status

```typescript
const { isAuthenticated, user } = useAppSelector((state) => state.auth);

if (!isAuthenticated) {
  return <Navigate to="/login" />;
}

return <div>Welcome, {user?.firstName}!</div>;
```

### Display Current User

```typescript
const { user } = useAppSelector((state) => state.auth);

return (
  <div>
    <p>User: {user?.firstName} {user?.lastName}</p>
    <p>Email: {user?.email}</p>
    <p>Role: {user?.role}</p>
  </div>
);
```

### Check User Role (Admin)

```typescript
const { user } = useAppSelector((state) => state.auth);

const isSuperAdmin = user?.role === 'superadmin';
const isAdmin = user?.role === 'admin' || user?.role === 'superadmin';

return <>{isAdmin && <AdminPanel />}</>;
```

### Handle Authentication Errors

```typescript
const [login, { isLoading, error }] = useLoginMutation();

try {
  await login({ email, password }).unwrap();
} catch (err) {
  // err.status - HTTP status code
  // err.data.message - Error message from backend
  // err.data.errors - Array of validation errors
  console.error('Login failed:', err.data?.message);
}
```

### Refresh Token Flow

When a request returns 401 (Unauthorized):

1. Frontend detects 401 error
2. Calls `refreshToken` mutation with existing token
3. Backend returns new token
4. Frontend retries original request with new token
5. If refresh fails, user is logged out

Current implementation: TODO in baseQuery

## Backend Integration

### Expected API Responses

**Login Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "user": {
      "id": "uuid",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "customer" | "admin" | "superadmin",
      "avatarUrl": "https://..."
    },
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

**JWT Token Structure:**
```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "admin",
  "iat": 1234567890,
  "exp": 1234571490
}
```

### Backend Endpoints

| Endpoint | Method | Auth | Request | Response |
|----------|--------|------|---------|----------|
| `/auth/login` | POST | No | Email, Password | User, Token |
| `/auth/register` | POST | No | Email, Password, Name | User, Token |
| `/auth/me` | GET | Yes | - | User |
| `/auth/logout` | POST | Yes | - | Success |
| `/auth/refresh-token` | POST | No | Token | New Token |

## Security Considerations

### ✅ What's Implemented

- JWT token-based authentication
- Token stored in localStorage (accessible to token refresh)
- Bearer token included in all protected requests
- Protected routes for authenticated users
- Role-based access control for admin
- Password fields use HTML5 password input

### ⚠️ Production Recommendations

- [ ] Implement HTTPS only
- [ ] Consider storing token in httpOnly cookie instead of localStorage
- [ ] Implement token refresh before expiration
- [ ] Add CSRF protection
- [ ] Implement rate limiting on login attempts
- [ ] Add two-factor authentication
- [ ] Implement automatic logout on inactivity
- [ ] Log security events (login, logout, failed attempts)
- [ ] Validate JWT signature on backend
- [ ] Add Content Security Policy headers

## Troubleshooting

### User not staying logged in after refresh

**Issue**: User gets logged out when refreshing the page

**Solution**:
1. Check that token is in localStorage
2. Verify API URL in `.env`
3. Check that auth slice initialState loads token from localStorage
4. Verify backend endpoint returns token in correct format

### Login button disabled indefinitely

**Issue**: isLoading stays true after login fails

**Solution**: RTK Query should handle this automatically. If not:
1. Check error handling in mutation
2. Verify catch block in handleSubmit
3. Check that error doesn't crash the component

### Protected routes not working

**Issue**: Can access protected pages without login

**Solution**:
1. Verify `ProtectedRoute` component is wrapping the route
2. Check that Redux state is properly initialized
3. Verify `useAppSelector` is reading correct state
4. Check localStorage is available

### API calls not including token

**Issue**: 401 errors on protected endpoints

**Solution**:
1. Verify token is in localStorage
2. Check `prepareHeaders` function in baseQuery
3. Verify Authorization header format: `Bearer {token}`
4. Check backend is validating JWT correctly

## Further Reading

- [Redux Documentation](https://redux.js.org/)
- [RTK Query Documentation](https://redux-toolkit.js.org/rtk-query/overview)
- [JWT Introduction](https://jwt.io/introduction)
- [React Router Documentation](https://reactrouter.com/)
