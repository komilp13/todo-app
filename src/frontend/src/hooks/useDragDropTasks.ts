/**
 * Custom hook for managing drag-and-drop task reordering
 * Handles optimistic updates, API calls, and rollback on failure
 */

import { useState, useCallback } from 'react';
import { TodoTask, SystemList } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';

interface DragDropState {
  tasks: TodoTask[];
  isReordering: boolean;
  error: string | null;
}

export function useDragDropTasks(
  tasks: TodoTask[],
  systemList: SystemList,
  onReorderError: (message: string) => void
) {
  const [state, setState] = useState<DragDropState>({
    tasks,
    isReordering: false,
    error: null,
  });

  const handleReorder = useCallback(
    async (newOrder: TodoTask[]) => {
      // Save original for rollback
      const originalTasks = state.tasks;

      // Optimistically update local state
      setState((prev) => ({
        ...prev,
        tasks: newOrder,
        isReordering: true,
        error: null,
      }));

      try {
        // Call API to persist new order
        const taskIds = newOrder.map((t) => t.id);
        await apiClient.patch('/tasks/reorder', {
          taskIds,
          systemList,
        });

        // Success - state already updated optimistically
        setState((prev) => ({
          ...prev,
          isReordering: false,
        }));
      } catch (err) {
        // Rollback on failure
        console.error('Failed to reorder tasks:', err);

        setState((prev) => ({
          ...prev,
          tasks: originalTasks,
          isReordering: false,
          error: err instanceof ApiError ? err.message : 'Failed to reorder tasks',
        }));

        onReorderError(
          err instanceof ApiError ? err.message : 'Failed to reorder tasks'
        );
      }
    },
    [state.tasks, systemList, onReorderError]
  );

  return {
    tasks: state.tasks,
    isReordering: state.isReordering,
    error: state.error,
    handleReorder,
  };
}
