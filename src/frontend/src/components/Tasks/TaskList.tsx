/**
 * TaskList Component
 * Fetches and displays tasks for a given system list
 * Shows loading skeleton, empty state, and handles task interactions
 * Supports task completion with animation and undo toast
 */

'use client';

import { useEffect, useState, useRef } from 'react';
import { TodoTask, SystemList, TaskStatus } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import { useToast } from '@/hooks/useToast';
import TaskRow from './TaskRow';
import TaskListSkeleton from './TaskListSkeleton';
import ToastContainer from '../Toast/ToastContainer';

interface TaskListProps {
  systemList: SystemList;
  onTaskClick?: (task: TodoTask) => void;
  onTaskComplete?: (taskId: string) => void;
  refresh?: number; // Increment to trigger refetch
}

interface CompletingTask {
  taskId: string;
  originalTask: TodoTask;
  toastId?: string;
  timeoutId?: NodeJS.Timeout;
}

export default function TaskList({
  systemList,
  onTaskClick,
  onTaskComplete,
  refresh = 0,
}: TaskListProps) {
  const [tasks, setTasks] = useState<TodoTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [animatingOutTaskIds, setAnimatingOutTaskIds] = useState<Set<string>>(
    new Set()
  );
  const completingTasksRef = useRef<Map<string, CompletingTask>>(new Map());
  const { toasts, show, dismiss } = useToast();

  useEffect(() => {
    const fetchTasks = async () => {
      setLoading(true);
      setError(null);

      try {
        const { data } = await apiClient.get<{
          tasks: TodoTask[];
          totalCount: number;
        }>(`/tasks?systemList=${systemList}`);
        setTasks(data.tasks);
      } catch (err) {
        console.error('Failed to fetch tasks:', err);
        setError('Failed to load tasks. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchTasks();
  }, [systemList, refresh]);

  const handleTaskComplete = async (taskId: string) => {
    const taskIndex = tasks.findIndex((t) => t.id === taskId);
    if (taskIndex === -1) return;

    const originalTask = tasks[taskIndex];
    const optimisticTask: TodoTask = {
      ...originalTask,
      status: TaskStatus.Done,
      isArchived: true,
    };

    // Optimistically update UI
    const newTasks = [...tasks];
    newTasks[taskIndex] = optimisticTask;
    setTasks(newTasks);

    try {
      // Call API to complete task
      const { data } = await apiClient.patch<TodoTask>(
        `/tasks/${taskId}/complete`
      );

      // Update with API response
      const updatedTasks = [...tasks];
      const updatedIndex = updatedTasks.findIndex((t) => t.id === taskId);
      if (updatedIndex !== -1) {
        updatedTasks[updatedIndex] = data;
        setTasks(updatedTasks);
      }

      // Track this task for completion
      const completingTask: CompletingTask = {
        taskId,
        originalTask,
      };
      completingTasksRef.current.set(taskId, completingTask);

      // Show undo toast
      const toastId = show('Task completed', {
        type: 'success',
        duration: 5000,
        action: {
          label: 'Undo',
          onClick: () => handleUndo(taskId),
        },
      });
      completingTask.toastId = toastId;

      // Start animation after 1.5s if not undone
      const timeoutId = setTimeout(() => {
        if (completingTasksRef.current.has(taskId)) {
          setAnimatingOutTaskIds((prev) => new Set(prev).add(taskId));

          // Remove from DOM after animation completes
          setTimeout(() => {
            setTasks((currentTasks) =>
              currentTasks.filter((t) => t.id !== taskId)
            );
            setAnimatingOutTaskIds((prev) => {
              const next = new Set(prev);
              next.delete(taskId);
              return next;
            });
            completingTasksRef.current.delete(taskId);
          }, 300); // Animation duration
        }
      }, 1500);

      completingTask.timeoutId = timeoutId;
    } catch (err) {
      // Revert optimistic update - use current state
      setTasks((currentTasks) => {
        const revertedTasks = [...currentTasks];
        const revertedIndex = revertedTasks.findIndex((t) => t.id === taskId);
        if (revertedIndex !== -1) {
          revertedTasks[revertedIndex] = originalTask;
        }
        return revertedTasks;
      });

      console.error('Failed to complete task:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to complete task', { type: 'error' });
      } else {
        show('Failed to complete task', { type: 'error' });
      }
    }
  };

  const handleUndo = async (taskId: string) => {
    const completingTask = completingTasksRef.current.get(taskId);
    if (!completingTask) return;

    // Clear animation timeout
    if (completingTask.timeoutId) {
      clearTimeout(completingTask.timeoutId);
    }

    // Clear animation state
    setAnimatingOutTaskIds((prev) => {
      const next = new Set(prev);
      next.delete(taskId);
      return next;
    });

    try {
      // Call API to reopen task
      const { data } = await apiClient.patch<TodoTask>(
        `/tasks/${taskId}/reopen`
      );

      // Update task back to original state
      const updatedTasks = [...tasks];
      const updatedIndex = updatedTasks.findIndex((t) => t.id === taskId);
      if (updatedIndex !== -1) {
        updatedTasks[updatedIndex] = data;
      } else {
        // Task might have been removed, add it back
        updatedTasks.push(data);
      }
      setTasks(updatedTasks);

      show('Task restored', { type: 'success' });
    } catch (err) {
      console.error('Failed to undo task completion:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to restore task', { type: 'error' });
      } else {
        show('Failed to restore task', { type: 'error' });
      }
    } finally {
      completingTasksRef.current.delete(taskId);
    }
  };

  if (loading) {
    return <TaskListSkeleton />;
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-center">
        <p className="text-sm text-red-700">{error}</p>
      </div>
    );
  }

  if (tasks.length === 0) {
    return (
      <div className="rounded-lg border border-gray-200 bg-white p-8 text-center">
        <p className="text-gray-500">
          No tasks here. Enjoy your free time! 🎉
        </p>
        <ToastContainer toasts={toasts} onDismiss={dismiss} />
      </div>
    );
  }

  return (
    <>
      <div className="space-y-2">
        {tasks.map((task) => (
          <TaskRow
            key={task.id}
            task={task}
            onComplete={handleTaskComplete}
            onClick={onTaskClick}
            isAnimatingOut={animatingOutTaskIds.has(task.id)}
          />
        ))}
      </div>
      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </>
  );
}
