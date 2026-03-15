import { useEffect } from 'react';
import { useLocation } from 'react-router';
import { telemetry } from '@/shared/lib/utils/telemetry';

export function useRouteTelemetry() {
  const location = useLocation();

  useEffect(() => {
    telemetry.track('route.change', { path: location.pathname });
  }, [location.pathname]);
}
