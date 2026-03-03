/**
 * Toast Container - Manages multiple toast notifications
 * Displays stacked toasts from Redux state
 */

import { useAppSelector, useAppDispatch } from '@/shared/lib/store';
import { removeToast } from './toastSlice';
import Toast from './Toast';
import styles from './Toast.module.css';

export default function ToastContainer() {
  const toasts = useAppSelector((state) => state.toast.toasts);
  const dispatch = useAppDispatch();

  const handleDismiss = (id: string) => {
    dispatch(removeToast(id));
  };

  return (
    <div className={styles.container}>
      {toasts.map((toast) => (
        <Toast key={toast.id} toast={toast} onDismiss={handleDismiss} />
      ))}
    </div>
  );
}
