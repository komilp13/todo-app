/**
 * TaskCreateModalContext
 * Provides global access to the task creation modal state and actions
 * Allows any component to open the modal from anywhere in the app
 */

'use client';

import { createContext, useContext, ReactNode, useState, useCallback } from 'react';

interface TaskCreateModalContextType {
  isOpen: boolean;
  openModal: () => void;
  closeModal: () => void;
}

const TaskCreateModalContext = createContext<TaskCreateModalContextType | undefined>(
  undefined
);

export function TaskCreateModalProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);

  const openModal = useCallback(() => {
    setIsOpen(true);
  }, []);

  const closeModal = useCallback(() => {
    setIsOpen(false);
  }, []);

  return (
    <TaskCreateModalContext.Provider value={{ isOpen, openModal, closeModal }}>
      {children}
    </TaskCreateModalContext.Provider>
  );
}

export function useTaskCreateModalContext() {
  const context = useContext(TaskCreateModalContext);
  if (!context) {
    throw new Error(
      'useTaskCreateModalContext must be used within TaskCreateModalProvider'
    );
  }
  return context;
}
