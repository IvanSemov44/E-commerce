/**
 * Toast Container - Admin Panel
 * Manages and displays all toast notifications
 */

import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { removeToast } from '../../store/slices/toastSlice';
import Toast from './Toast';
import styles from './Toast.module.css';

export default function ToastContainer() {
  const dispatch = useAppDispatch();
  const toasts = useAppSelector((state) => state.toast.toasts);

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
