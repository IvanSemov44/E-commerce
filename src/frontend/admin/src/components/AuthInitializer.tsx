import { useEffect } from 'react';
import { useAppDispatch } from '../store/hooks';
import { setInitialized, setUser } from '../store/slices/authSlice';
import { authApi } from '../store/api/authApi';

interface AuthInitializerProps {
  children: React.ReactNode;
}

export default function AuthInitializer({ children }: AuthInitializerProps) {
  const dispatch = useAppDispatch();

  useEffect(() => {
    // Try to get current user on app startup
    const initAuth = async () => {
      try {
        const response = await fetch(`${import.meta.env.VITE_API_URL || 'http://localhost:5000/api'}/auth/me`, {
          credentials: 'include',
        });
        
        if (response.ok) {
          const data = await response.json();
          if (data.success && data.data) {
            dispatch(setUser(data.data));
          } else {
            dispatch(setInitialized());
          }
        } else {
          dispatch(setInitialized());
        }
      } catch (error) {
        console.error('Auth initialization failed:', error);
        dispatch(setInitialized());
      }
    };

    initAuth();
  }, [dispatch]);

  return <>{children}</>;
}
