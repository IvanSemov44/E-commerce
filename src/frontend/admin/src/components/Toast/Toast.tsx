/**
 * Toast Component - Admin Panel
 * Renders a single toast notification
 */

import { useEffect } from 'react';
import type { Toast } from '../store/slices/toastSlice';
import styles from './Toast.module.css';

interface ToastProps {
  toast: Toast;
  onDismiss: (id: string) => void;
}

const getIcon = (variant: string): string => {
  switch (variant) {
    case 'success':
      return '✓';
    case 'error':
      return '✕';
    case 'warning':
      return '⚠';
    case 'info':
      return 'ℹ';
    default:
      return '•';
  }
};

export default function Toast({ toast, onDismiss }: ToastProps) {
  useEffect(() => {
    if (toast.duration) {
      const timer = setTimeout(() => {
        onDismiss(toast.id);
      }, toast.duration);

      return () => clearTimeout(timer);
    }
  }, [toast, onDismiss]);

  return (
    <div className={`${styles.toast} ${styles[toast.variant]}`} role="alert" aria-live="polite">
      <span className={styles.icon}>{getIcon(toast.variant)}</span>
      <span className={styles.message}>{toast.message}</span>
      <button
        className={styles.close}
        onClick={() => onDismiss(toast.id)}
        aria-label="Close notification"
      >
        ✕
      </button>
    </div>
  );
}
