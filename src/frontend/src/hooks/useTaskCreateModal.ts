/**
 * useTaskCreateModal Hook
 * Attaches keyboard shortcuts (Q key) to the task creation modal
 * Requires TaskCreateModalProvider to be present in the component tree
 * Only triggers on "Q" key when not typing in an input field
 */

import { useEffect } from 'react';
import { useTaskCreateModalContext } from '@/contexts/TaskCreateModalContext';

interface UseTaskCreateModalOptions {
  enabled?: boolean;
}

export function useTaskCreateModal(options: UseTaskCreateModalOptions = {}) {
  const { enabled = true } = options;
  const { openModal } = useTaskCreateModalContext();

  // Handle keyboard shortcut (Q key)
  useEffect(() => {
    if (!enabled) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      // Don't trigger if user is typing in input/textarea
      const target = event.target as HTMLElement;
      const isTyping =
        target instanceof HTMLInputElement ||
        target instanceof HTMLTextAreaElement;

      // Trigger on "Q" key (case-insensitive) when not typing
      if ((event.key === 'q' || event.key === 'Q') && !isTyping) {
        event.preventDefault();
        openModal();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [enabled, openModal]);
}
