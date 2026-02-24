/**
 * useOnlineStatus Hook
 * Detects if the user is online or offline
 * Provides graceful degradation for offline scenarios
 */

import { useEffect, useState } from 'react';
import { logger } from '../utils/logger';

interface UseOnlineStatusReturn {
  isOnline: boolean;
  wasOffline: boolean;
}

export function useOnlineStatus(): UseOnlineStatusReturn {
  const [isOnline, setIsOnline] = useState(() => {
    // Check initial online status
    if (typeof window !== 'undefined') {
      return navigator.onLine;
    }
    return true;
  });

  const [wasOffline, setWasOffline] = useState(false);

  useEffect(() => {
    // Handle going online
    const handleOnline = () => {
      setIsOnline(true);
      // Mark that user was offline (can be used to refetch data)
      setWasOffline(true);
      logger.info('useOnlineStatus', 'Back online');
    };

    // Handle going offline
    const handleOffline = () => {
      setIsOnline(false);
      logger.info('useOnlineStatus', 'Went offline');
    };

    // Add event listeners
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    // Cleanup
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  return {
    isOnline,
    wasOffline,
  };
}
