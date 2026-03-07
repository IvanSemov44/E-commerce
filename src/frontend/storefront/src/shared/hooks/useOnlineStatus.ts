/**
 * useOnlineStatus Hook
 * Tracks the browser's online/offline connectivity state.
 * Returns:
 *  - isOnline:   current connectivity status
 *  - wasOffline: true once the connection is restored after being offline
 */

import { useState, useEffect } from 'react';

interface OnlineStatus {
  isOnline: boolean;
  /** Becomes true the first time the browser comes back online after being offline. */
  wasOffline: boolean;
}

export function useOnlineStatus(): OnlineStatus {
  const [isOnline, setIsOnline] = useState<boolean>(navigator.onLine);
  const [wasOffline, setWasOffline] = useState<boolean>(false);

  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true);
      setWasOffline(true);
    };

    const handleOffline = () => {
      setIsOnline(false);
    };

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  return { isOnline, wasOffline };
}
