/**
 * useToast Hook
 * Manages toast notifications with automatic cleanup
 */

import { useState, useCallback } from 'react';

interface ToastOptions {
  type?: 'success' | 'error' | 'info';
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

interface Toast {
  id: string;
  message: string;
  type?: 'success' | 'error' | 'info';
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export function useToast() {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const show = useCallback(
    (message: string, options: ToastOptions = {}) => {
      const id = `toast-${Date.now()}-${Math.random()}`;
      const toast: Toast = {
        id,
        message,
        type: options.type,
        duration: options.duration,
        action: options.action,
      };

      setToasts((prev) => [...prev, toast]);
      return id;
    },
    []
  );

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  }, []);

  const success = useCallback(
    (message: string, options?: Omit<ToastOptions, 'type'>) => {
      return show(message, { ...options, type: 'success' });
    },
    [show]
  );

  const error = useCallback(
    (message: string, options?: Omit<ToastOptions, 'type'>) => {
      return show(message, { ...options, type: 'error' });
    },
    [show]
  );

  const info = useCallback(
    (message: string, options?: Omit<ToastOptions, 'type'>) => {
      return show(message, { ...options, type: 'info' });
    },
    [show]
  );

  return {
    toasts,
    show,
    dismiss,
    success,
    error,
    info,
  };
}
