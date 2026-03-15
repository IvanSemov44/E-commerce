import { redirect, Outlet } from 'react-router';
import { Navigate } from 'react-router-dom';
import { store } from '@/shared/lib/store';
import {
  selectAuthInitialized,
  selectIsAuthenticated,
  selectAuthStatus,
} from '@/features/auth/slices/authSlice';
import { useAppSelector } from '@/shared/lib/store';
import { ROUTE_PATHS } from '@/shared/constants/navigation';

export async function loader() {
  const state = store.getState();
  const initialized = selectAuthInitialized(state);
  const isAuthenticated = selectIsAuthenticated(state);

  // Only hard-redirect when we KNOW auth state is settled and user is not authenticated.
  // If not initialized yet (initial page load), fall through to the component
  // which handles the loading spinner until RTK Query resolves /auth/me.
  if (initialized && !isAuthenticated) {
    throw redirect(ROUTE_PATHS.login);
  }

  return null;
}

export default function ProtectedLayout() {
  const { isAuthenticated, loading } = useAppSelector(selectAuthStatus);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to={ROUTE_PATHS.login} replace />;
  }

  return <Outlet />;
}
