import { useState, useCallback } from 'react';

interface ConfirmationState {
  isOpen: boolean;
  title: string;
  message: string;
  onConfirm: () => void;
}

interface UseConfirmationReturn {
  confirmation: ConfirmationState;
  confirm: (title: string, message: string, onConfirm: () => void) => void;
  handleConfirm: () => void;
  handleCancel: () => void;
}

const CLOSED: ConfirmationState = { isOpen: false, title: '', message: '', onConfirm: () => {} };

export function useConfirmation(): UseConfirmationReturn {
  const [confirmation, setConfirmation] = useState<ConfirmationState>(CLOSED);

  const confirm = useCallback((title: string, message: string, onConfirm: () => void) => {
    setConfirmation({ isOpen: true, title, message, onConfirm });
  }, []);

  const handleConfirm = useCallback(() => {
    confirmation.onConfirm();
    setConfirmation(CLOSED);
  }, [confirmation]);

  const handleCancel = useCallback(() => {
    setConfirmation(CLOSED);
  }, []);

  return { confirmation, confirm, handleConfirm, handleCancel };
}
