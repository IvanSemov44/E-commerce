/**
 * Logger Utility
 * Provides structured logging that can be disabled in production builds.
 * Replaces console.log/error/warn with a controlled logging mechanism.
 */

type LogLevel = 'debug' | 'info' | 'warn' | 'error';

interface LoggerConfig {
  enabled: boolean;
  level: LogLevel;
  prefix: string;
}

// Check if we're in development mode
const isDevelopment = import.meta.env.DEV;

const config: LoggerConfig = {
  enabled: isDevelopment,
  level: isDevelopment ? 'debug' : 'error',
  prefix: '[ECommerce]',
};

const levelPriority: Record<LogLevel, number> = {
  debug: 0,
  info: 1,
  warn: 2,
  error: 3,
};

function shouldLog(level: LogLevel): boolean {
  // Errors always log regardless of environment (build config strips them in prod)
  if (level === 'error') return true;
  return config.enabled && levelPriority[level] >= levelPriority[config.level];
}

function formatMessage(level: LogLevel, context: string, message: string): string {
  const timestamp = new Date().toISOString();
  return `${config.prefix} [${timestamp}] [${level.toUpperCase()}] [${context}] ${message}`;
}

export const logger = {
  debug(context: string, message: string, ...args: unknown[]): void {
    if (shouldLog('debug')) {
      console.debug(formatMessage('debug', context, message), ...args);
    }
  },

  info(context: string, message: string, ...args: unknown[]): void {
    if (shouldLog('info')) {
      console.info(formatMessage('info', context, message), ...args);
    }
  },

  warn(context: string, message: string, ...args: unknown[]): void {
    if (shouldLog('warn')) {
      console.warn(formatMessage('warn', context, message), ...args);
    }
  },

  error(context: string, message: string, ...args: unknown[]): void {
    if (shouldLog('error')) {
      console.error(formatMessage('error', context, message), ...args);
    }
  },

  group(title: string, fn: () => void): void {
    if (config.enabled) {
      console.group(title);
      fn();
      console.groupEnd();
    }
  },

  // For performance metrics that should always be available
  metric(name: string, value: number, rating: string): void {
    if (config.enabled) {
      const color = rating === 'good' ? 'green' : rating === 'needs-improvement' ? 'orange' : 'red';
      console.log(
        `%c${name}%c Value: ${value.toFixed(2)}ms | Rating: ${rating}`,
        `color: ${color}; font-weight: bold;`,
        ''
      );
    }
  },
};

// Export a no-op logger for production use
export const noopLogger = {
  debug: () => {},
  info: () => {},
  warn: () => {},
  error: () => {},
  group: () => {},
  metric: () => {},
};

export default logger;
