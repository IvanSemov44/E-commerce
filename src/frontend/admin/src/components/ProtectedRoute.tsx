import { Navigate } from 'react-router-dom';
import { useAppSelector } from '../store/hooks';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: 'admin' | 'superadmin';
}

export default function ProtectedRoute({
  children,
  requiredRole,
}: ProtectedRouteProps) {
  const { isAuthenticated, user, initialized } = useAppSelector((state) => state.auth);

  // Wait for auth state to be initialized (from httpOnly cookie validation)
  if (!initialized) {
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole && user?.role !== requiredRole && user?.role !== 'superadmin') {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
