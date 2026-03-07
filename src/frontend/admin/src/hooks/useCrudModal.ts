import { useState } from 'react';

interface UseCrudModalReturn<T> {
  modalOpen: boolean;
  editingItem: T | undefined;
  handleCreate: () => void;
  handleEdit: (item: T) => void;
  handleClose: () => void;
}

export function useCrudModal<T>(): UseCrudModalReturn<T> {
  const [modalOpen, setModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<T | undefined>();

  const handleCreate = () => {
    setEditingItem(undefined);
    setModalOpen(true);
  };

  const handleEdit = (item: T) => {
    setEditingItem(item);
    setModalOpen(true);
  };

  const handleClose = () => {
    setModalOpen(false);
  };

  return { modalOpen, editingItem, handleCreate, handleEdit, handleClose };
}
