/**
 * Logger Utility - Admin Panel
 * Provides structured logging that can be disabled in production builds.
 * esbuild strips all console.* calls in production (drop: ['console'] in vite.config.ts).
 */

type LogLevel = 'debug' | 'info' | 'warn' | 'error';

const isDevelopment = import.meta.env.DEV;
const PREFIX = '[Admin]';

function formatMessage(level: LogLevel, context: string, message: string): string {
  const timestamp = new Date().toISOString();
  return `${PREFIX} [${timestamp}] [${level.toUpperCase()}] [${context}] ${message}`;
}

export const logger = {
  debug(context: string, message: string, ...args: unknown[]): void {
    if (isDevelopment) {
      console.debug(formatMessage('debug', context, message), ...args);
    }
  },

  info(context: string, message: string, ...args: unknown[]): void {
    if (isDevelopment) {
      console.info(formatMessage('info', context, message), ...args);
    }
  },

  warn(context: string, message: string, ...args: unknown[]): void {
    if (isDevelopment) {
      console.warn(formatMessage('warn', context, message), ...args);
    }
  },

  // Errors always log (esbuild strips them in production builds)
  error(context: string, message: string, ...args: unknown[]): void {
    console.error(formatMessage('error', context, message), ...args);
  },
};
