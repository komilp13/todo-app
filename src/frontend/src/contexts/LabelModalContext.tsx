'use client';

import { createContext, useContext, ReactNode, useState, useCallback } from 'react';

interface LabelToEdit {
  id: string;
  name: string;
  color?: string;
}

interface LabelModalContextType {
  isOpen: boolean;
  editingLabel: LabelToEdit | null;
  openCreateModal: () => void;
  openEditModal: (label: LabelToEdit) => void;
  closeModal: () => void;
}

const LabelModalContext = createContext<LabelModalContextType | undefined>(
  undefined
);

export function LabelModalProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);
  const [editingLabel, setEditingLabel] = useState<LabelToEdit | null>(null);

  const openCreateModal = useCallback(() => {
    setEditingLabel(null);
    setIsOpen(true);
  }, []);

  const openEditModal = useCallback((label: LabelToEdit) => {
    setEditingLabel(label);
    setIsOpen(true);
  }, []);

  const closeModal = useCallback(() => {
    setIsOpen(false);
    setEditingLabel(null);
  }, []);

  return (
    <LabelModalContext.Provider
      value={{ isOpen, editingLabel, openCreateModal, openEditModal, closeModal }}
    >
      {children}
    </LabelModalContext.Provider>
  );
}

export function useLabelModalContext() {
  const context = useContext(LabelModalContext);
  if (!context) {
    throw new Error(
      'useLabelModalContext must be used within LabelModalProvider'
    );
  }
  return context;
}
