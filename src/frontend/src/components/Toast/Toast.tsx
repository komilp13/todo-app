/**
 * Toast Component
 * Displays temporary notification messages with optional action button
 */

import { useEffect } from 'react';

export interface ToastProps {
  id: string;
  message: string;
  type?: 'success' | 'error' | 'info';
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
  onDismiss: (id: string) => void;
}

export default function Toast({
  id,
  message,
  type = 'info',
  duration = 5000,
  action,
  onDismiss,
}: ToastProps) {
  useEffect(() => {
    if (!action) {
      const timer = setTimeout(() => onDismiss(id), duration);
      return () => clearTimeout(timer);
    }
  }, [id, duration, action, onDismiss]);

  const bgColor =
    type === 'success'
      ? 'bg-green-50 border-green-200'
      : type === 'error'
        ? 'bg-red-50 border-red-200'
        : 'bg-gray-50 border-gray-200';

  const textColor =
    type === 'success'
      ? 'text-green-800'
      : type === 'error'
        ? 'text-red-800'
        : 'text-gray-800';

  const actionColor =
    type === 'success'
      ? 'text-green-600 hover:text-green-700'
      : type === 'error'
        ? 'text-red-600 hover:text-red-700'
        : 'text-blue-600 hover:text-blue-700';

  return (
    <div
      className={`animate-slide-up border rounded-lg p-4 shadow-lg ${bgColor} flex items-center justify-between gap-4`}
      role="alert"
    >
      <p className={`text-sm font-medium ${textColor}`}>{message}</p>
      <div className="flex items-center gap-2">
        {action && (
          <button
            onClick={() => {
              action.onClick();
              onDismiss(id);
            }}
            className={`text-sm font-semibold ${actionColor} transition-colors`}
          >
            {action.label}
          </button>
        )}
        <button
          onClick={() => onDismiss(id)}
          className={`text-gray-400 hover:text-gray-600 transition-colors`}
          aria-label="Close notification"
        >
          ✕
        </button>
      </div>
    </div>
  );
}
