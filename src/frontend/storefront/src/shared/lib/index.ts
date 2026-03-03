/**
 * Shared Library Barrel Export
 * 
 * Central export for all shared infrastructure and utilities:
 * - API utilities (baseApi re-export)
 * - Store utilities (Redux hooks)
 * - Common utilities (constants, validation, logger)
 * 
 * Usage:
 *   import { baseApi } from '@/shared/lib/api';
 *   import { useAppDispatch } from '@/shared/lib/store';
 *   import { logger, validators } from '@/shared/lib/utils';
 */

// API utilities
export * from './api';

// Store utilities
export * from './store';

// Common utilities
export * from './utils';
