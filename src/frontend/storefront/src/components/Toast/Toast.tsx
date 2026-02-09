/**
 * Toast Component - Individual toast notification
 * Displays message with icon, auto-dismisses after duration
 */

import { useEffect } from 'react';
import type { Toast } from '../../store/slices/toastSlice';
import styles from './Toast.module.css';

interface ToastProps {
  toast: Toast;
  onDismiss: (id: string) => void;
}

export default function Toast({ toast, onDismiss }: ToastProps) {
  useEffect(() => {
    if (!toast.duration || toast.duration <= 0) return;

    const timer = setTimeout(() => {
      onDismiss(toast.id);
    }, toast.duration);

    return () => clearTimeout(timer);
  }, [toast.id, toast.duration, onDismiss]);

  const getIcon = () => {
    switch (toast.variant) {
      case 'success':
        return '✓';
      case 'error':
        return '✕';
      case 'warning':
        return '⚠';
      case 'info':
        return 'ℹ';
      default:
        return '●';
    }
  };

  return (
    <div
      className={`${styles.toast} ${styles[toast.variant]}`}
      role="alert"
      aria-live="polite"
    >
      <span className={styles.icon}>{getIcon()}</span>
      <span className={styles.message}>{toast.message}</span>
      <button
        className={styles.closeButton}
        onClick={() => onDismiss(toast.id)}
        aria-label="Dismiss notification"
      >
        ×
      </button>
    </div>
  );
}
