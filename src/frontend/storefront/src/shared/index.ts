/**
 * Shared Module Barrel Export
 * 
 * Central export for all shared/cross-feature code:
 * - UI components (Button, Input, Card, etc)
 * - Layout components (Header, Footer)
 * - Shared hooks
 * - Shared types
 * - Library utilities and infrastructure
 * - Internationalization
 * 
 * Usage:
 *   import { Button, Header } from '@/shared';
 *   import { useForm, useToast } from '@/shared/hooks';
 *   import { logger, validators } from '@/shared/lib';
 *   import type { Product, ApiResponse } from '@/shared/types';
 */

// Components
export * from './components';

// Hooks
export * from './hooks';

// Library utilities and infrastructure
export * from './lib';

// Types
export * from './types';

// i18n
export * from './i18n';
