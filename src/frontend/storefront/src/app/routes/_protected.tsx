import { Outlet } from 'react-router';
import { Navigate } from 'react-router-dom';
import { selectAuthStatus } from '@/features/auth/slices/authSlice';
import { useAppSelector } from '@/shared/lib/store';
import { ROUTE_PATHS } from '@/shared/constants/navigation';

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
