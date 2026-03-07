/**
 * Client Telemetry Service
 *
 * Lightweight event-tracking layer for client-side observability.
 * Logs to console (dev) and can be wired to any analytics backend
 * (GA4, Segment, Amplitude, etc.) by replacing the `dispatch` sink.
 *
 * Minimum required events (per FRONTEND_CODING_GUIDE.md):
 *  - route.change
 *  - api.request
 *  - checkout.step
 *  - error.boundary
 */

import { logger } from './logger';

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface TelemetryEvent {
  /** Dot-separated event name, e.g. "api.request", "checkout.step" */
  event: string;
  /** Arbitrary key-value metadata */
  payload?: Record<string, unknown>;
  /** Populated automatically by track() */
  timestamp: number;
}

/** Sink function type: receives the event and forwards it to an analytics provider. */
type TelemetrySink = (event: TelemetryEvent) => void;

// ---------------------------------------------------------------------------
// Default sink: developer console
// ---------------------------------------------------------------------------

const consoleSink: TelemetrySink = (te) => {
  logger.info(
    'Telemetry',
    `[${te.event}]`,
    te.payload ?? ''
  );
};

// ---------------------------------------------------------------------------
// Service
// ---------------------------------------------------------------------------

class TelemetryService {
  private sinks: TelemetrySink[] = [consoleSink];

  /**
   * Register an additional sink (call before app starts).
   * Example: `telemetry.addSink((e) => analytics.track(e.event, e.payload))`
   */
  addSink(sink: TelemetrySink): void {
    this.sinks.push(sink);
  }

  /**
   * Emit a telemetry event to all registered sinks.
   */
  track(event: string, payload?: Record<string, unknown>): void {
    const te: TelemetryEvent = { event, payload, timestamp: Date.now() };
    for (const sink of this.sinks) {
      try {
        sink(te);
      } catch {
        // Sinks must not throw — swallow silently
      }
    }
  }
}

export const telemetry = new TelemetryService();
