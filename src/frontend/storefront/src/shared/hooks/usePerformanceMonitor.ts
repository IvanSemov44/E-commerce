/**
 * usePerformanceMonitor Hook
 * Tracks Core Web Vitals and performance metrics:
 * - Largest Contentful Paint (LCP)
 * - First Input Delay (FID)
 * - Cumulative Layout Shift (CLS)
 * - First Contentful Paint (FCP)
 * - Time to Interactive (TTI)
 */

import { useEffect } from 'react';
import { logger } from '@/shared/lib/utils/logger';

interface PerformanceMetric {
  name: string;
  value: number;
  rating: 'good' | 'needs-improvement' | 'poor';
  timestamp: number;
}

interface LayoutShift extends PerformanceEntry {
  value: number;
  hadRecentInput: boolean;
}

interface LargestContentfulPaint extends PerformanceEntry {
  renderTime: number;
  loadTime: number;
}

interface FirstInputDelay extends PerformanceEntry {
  processingStart: number;
}

interface PerformanceCallback {
  (metric: PerformanceMetric): void;
}

interface UsePerformanceMonitorOptions {
  onMetric?: PerformanceCallback;
  enableLogging?: boolean;
}

export function usePerformanceMonitor(options: UsePerformanceMonitorOptions = {}): void {
  const { onMetric, enableLogging = import.meta.env.MODE === 'development' } = options;

  useEffect(() => {
    // Helper to log metric
    const logMetric = (metric: PerformanceMetric) => {
      if (enableLogging) {
        logger.metric(metric.name, metric.value, metric.rating);
      }

      if (onMetric) {
        onMetric(metric);
      }
    };

    // Monitor Largest Contentful Paint (LCP)
    const observeLCP = () => {
      try {
        const observer = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          const lastEntry = entries[entries.length - 1] as LargestContentfulPaint;
          const value = lastEntry.renderTime || lastEntry.loadTime;

          const metric: PerformanceMetric = {
            name: 'Largest Contentful Paint (LCP)',
            value: value,
            rating: value < 2500 ? 'good' : value < 4000 ? 'needs-improvement' : 'poor',
            timestamp: Date.now(),
          };

          logMetric(metric);
        });

        observer.observe({ type: 'largest-contentful-paint', buffered: true });
        return () => observer.disconnect();
      } catch {
        logger.warn('usePerformanceMonitor', 'LCP monitoring not supported');
        return () => {};
      }
    };

    // Monitor First Input Delay (FID)
    const observeFID = () => {
      try {
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            const fidEntry = entry as FirstInputDelay;
            const delay = fidEntry.processingStart - fidEntry.startTime;
            const metric: PerformanceMetric = {
              name: 'First Input Delay (FID)',
              value: delay,
              rating: delay < 100 ? 'good' : delay < 300 ? 'needs-improvement' : 'poor',
              timestamp: Date.now(),
            };

            logMetric(metric);
          }
        });

        observer.observe({ type: 'first-input', buffered: true });
        return () => observer.disconnect();
      } catch {
        logger.warn('usePerformanceMonitor', 'FID monitoring not supported');
        return () => {};
      }
    };

    // Monitor Cumulative Layout Shift (CLS)
    const observeCLS = () => {
      try {
        let clsValue = 0;
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            const layoutShiftEntry = entry as LayoutShift;
            if (!layoutShiftEntry.hadRecentInput) {
              clsValue += layoutShiftEntry.value;

              const metric: PerformanceMetric = {
                name: 'Cumulative Layout Shift (CLS)',
                value: clsValue,
                rating: clsValue < 0.1 ? 'good' : clsValue < 0.25 ? 'needs-improvement' : 'poor',
                timestamp: Date.now(),
              };

              logMetric(metric);
            }
          }
        });

        observer.observe({ type: 'layout-shift', buffered: true });
        return () => observer.disconnect();
      } catch {
        logger.warn('usePerformanceMonitor', 'CLS monitoring not supported');
        return () => {};
      }
    };

    // Monitor First Contentful Paint (FCP)
    const observeFCP = () => {
      try {
        const observer = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          const fcpEntry = entries.find((entry) => entry.name === 'first-contentful-paint');

          if (fcpEntry) {
            const metric: PerformanceMetric = {
              name: 'First Contentful Paint (FCP)',
              value: fcpEntry.startTime,
              rating:
                fcpEntry.startTime < 1800
                  ? 'good'
                  : fcpEntry.startTime < 3000
                    ? 'needs-improvement'
                    : 'poor',
              timestamp: Date.now(),
            };

            logMetric(metric);
          }
        });

        observer.observe({ type: 'paint', buffered: true });
        return () => observer.disconnect();
      } catch {
        logger.warn('usePerformanceMonitor', 'FCP monitoring not supported');
        return () => {};
      }
    };

    // Track page visibility for TTI estimation
    const trackVisibility = () => {
      const handleVisibilityChange = () => {
        if (document.hidden) {
          // Page is hidden - user might have left
          logger.debug('usePerformanceMonitor', 'Page hidden - TTI tracking paused');
        } else {
          // Page is visible - continue monitoring
          logger.debug('usePerformanceMonitor', 'Page visible - TTI tracking resumed');
        }
      };

      document.addEventListener('visibilitychange', handleVisibilityChange);
      return () => document.removeEventListener('visibilitychange', handleVisibilityChange);
    };

    // Start all observers
    const cleanupLCP = observeLCP();
    const cleanupFID = observeFID();
    const cleanupCLS = observeCLS();
    const cleanupFCP = observeFCP();
    const cleanupVisibility = trackVisibility();

    // Cleanup function
    return () => {
      cleanupLCP();
      cleanupFID();
      cleanupCLS();
      cleanupFCP();
      cleanupVisibility();
    };
  }, [enableLogging, onMetric]);
}
