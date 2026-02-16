/**
 * UpcomingTaskList Component
 * Specialized list for the Upcoming view with date grouping
 * Groups tasks by: Overdue, Today, Tomorrow, specific dates, No date
 * Sorted by priority within each group
 */

'use client';

import { useEffect, useState, useRef } from 'react';
import { TodoTask, TaskStatus } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import { useToast } from '@/hooks/useToast';
import { useTaskRefreshContext } from '@/contexts/TaskRefreshContext';
import TaskRow from './TaskRow';
import TaskListSkeleton from './TaskListSkeleton';
import ToastContainer from '../Toast/ToastContainer';

interface CompletingTask {
  taskId: string;
  originalTask: TodoTask;
  toastId?: string;
  timeoutId?: NodeJS.Timeout;
}

interface UpcomingTaskListProps {
  onTaskClick?: (task: TodoTask) => void;
  refresh?: number;
}

interface GroupedTasks {
  overdue: TodoTask[];
  today: TodoTask[];
  tomorrow: TodoTask[];
  upcoming: { date: Date; tasks: TodoTask[] }[];
}

export default function UpcomingTaskList({
  onTaskClick,
  refresh = 0,
}: UpcomingTaskListProps) {
  const [tasks, setTasks] = useState<TodoTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [animatingOutTaskIds, setAnimatingOutTaskIds] = useState<Set<string>>(new Set());
  const completingTasksRef = useRef<Map<string, CompletingTask>>(new Map());
  const { toasts, show, dismiss } = useToast();
  const { triggerRefresh } = useTaskRefreshContext();

  useEffect(() => {
    const fetchUpcomingTasks = async () => {
      setLoading(true);
      setError(null);

      try {
        const { data } = await apiClient.get<{
          tasks: TodoTask[];
          totalCount: number;
        }>('/tasks?view=upcoming');
        setTasks(data.tasks);
      } catch (err) {
        console.error('Failed to fetch upcoming tasks:', err);
        setError('Failed to load upcoming tasks. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchUpcomingTasks();
  }, [refresh]);

  const handleTaskComplete = async (taskId: string) => {
    const taskIndex = tasks.findIndex((t) => t.id === taskId);
    if (taskIndex === -1) return;

    const originalTask = tasks[taskIndex];

    // Optimistically mark as done in UI
    const optimisticTask: TodoTask = {
      ...originalTask,
      status: TaskStatus.Done,
      isArchived: true,
    };
    const newTasks = [...tasks];
    newTasks[taskIndex] = optimisticTask;
    setTasks(newTasks);

    try {
      const { data } = await apiClient.patch<TodoTask>(
        `/tasks/${taskId}/complete`
      );

      // Update with API response
      setTasks((current) => {
        const updated = [...current];
        const idx = updated.findIndex((t) => t.id === taskId);
        if (idx !== -1) updated[idx] = data;
        return updated;
      });

      // Refresh sidebar counts
      triggerRefresh();

      // Track for undo
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

      // Start fade-out animation after 1.5s, then remove from DOM
      const timeoutId = setTimeout(() => {
        if (completingTasksRef.current.has(taskId)) {
          setAnimatingOutTaskIds((prev) => new Set(prev).add(taskId));

          setTimeout(() => {
            setTasks((current) => current.filter((t) => t.id !== taskId));
            setAnimatingOutTaskIds((prev) => {
              const next = new Set(prev);
              next.delete(taskId);
              return next;
            });
            completingTasksRef.current.delete(taskId);
          }, 300);
        }
      }, 1500);

      completingTask.timeoutId = timeoutId;
    } catch (err) {
      // Revert optimistic update
      setTasks((current) => {
        const reverted = [...current];
        const idx = reverted.findIndex((t) => t.id === taskId);
        if (idx !== -1) {
          reverted[idx] = originalTask;
        }
        return reverted;
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

    if (completingTask.timeoutId) {
      clearTimeout(completingTask.timeoutId);
    }

    setAnimatingOutTaskIds((prev) => {
      const next = new Set(prev);
      next.delete(taskId);
      return next;
    });

    try {
      const { data } = await apiClient.patch<TodoTask>(
        `/tasks/${taskId}/reopen`
      );

      setTasks((current) => {
        const idx = current.findIndex((t) => t.id === taskId);
        if (idx !== -1) {
          const updated = [...current];
          updated[idx] = data;
          return updated;
        }
        // Task was already removed, add it back
        return [...current, data];
      });

      show('Task restored', { type: 'success' });
      triggerRefresh();
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

  // Group tasks by date
  const groupTasksByDate = (tasks: TodoTask[]): GroupedTasks => {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    const grouped: GroupedTasks = {
      overdue: [],
      today: [],
      tomorrow: [],
      upcoming: [],
    };

    // First, sort all tasks by priority within their respective groups
    const sortByPriority = (a: TodoTask, b: TodoTask) => {
      const priorityOrder: Record<string, number> = { p1: 1, p2: 2, p3: 3, p4: 4 };
      const aOrder = a.priority ? (priorityOrder[a.priority] ?? 5) : 5;
      const bOrder = b.priority ? (priorityOrder[b.priority] ?? 5) : 5;
      return aOrder - bOrder;
    };

    // Group tasks
    const upcomingDatesMap = new Map<string, TodoTask[]>();

    tasks.forEach((task) => {
      if (!task.dueDate) {
        return; // Skip tasks without due dates (shouldn't happen in Upcoming view)
      }

      const dueDate = new Date(task.dueDate);
      const dueDateOnly = new Date(dueDate.getFullYear(), dueDate.getMonth(), dueDate.getDate());

      if (dueDateOnly < today) {
        grouped.overdue.push(task);
      } else if (dueDateOnly.getTime() === today.getTime()) {
        grouped.today.push(task);
      } else if (dueDateOnly.getTime() === tomorrow.getTime()) {
        grouped.tomorrow.push(task);
      } else {
        const dateKey = dueDateOnly.toISOString();
        if (!upcomingDatesMap.has(dateKey)) {
          upcomingDatesMap.set(dateKey, []);
        }
        upcomingDatesMap.get(dateKey)!.push(task);
      }
    });

    // Sort tasks within each group by priority
    grouped.overdue.sort(sortByPriority);
    grouped.today.sort(sortByPriority);
    grouped.tomorrow.sort(sortByPriority);

    // Convert upcoming dates map to sorted array
    grouped.upcoming = Array.from(upcomingDatesMap.entries())
      .map(([dateKey, tasks]) => ({
        date: new Date(dateKey),
        tasks: tasks.sort(sortByPriority),
      }))
      .sort((a, b) => a.date.getTime() - b.date.getTime());

    return grouped;
  };

  const formatDate = (date: Date): string => {
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
    });
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
      <>
        <div className="rounded-lg border border-gray-200 bg-white p-8 text-center">
          <p className="text-gray-500">
            No upcoming tasks. You&apos;re all caught up! 🎉
          </p>
        </div>
        <ToastContainer toasts={toasts} onDismiss={dismiss} />
      </>
    );
  }

  const groupedTasks = groupTasksByDate(tasks);

  return (
    <>
      <div className="space-y-6">
        {/* Overdue Section */}
        {groupedTasks.overdue.length > 0 && (
          <div>
            <h2 className="text-lg font-semibold text-red-600 mb-2">
              Overdue ({groupedTasks.overdue.length})
            </h2>
            <div className="space-y-2">
              {groupedTasks.overdue.map((task) => (
                <TaskRow
                  key={task.id}
                  task={task}
                  onComplete={handleTaskComplete}
                  onClick={onTaskClick}
                  isAnimatingOut={animatingOutTaskIds.has(task.id)}
                  showSystemList={true}
                />
              ))}
            </div>
          </div>
        )}

        {/* Today Section */}
        {groupedTasks.today.length > 0 && (
          <div>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              Today ({groupedTasks.today.length})
            </h2>
            <div className="space-y-2">
              {groupedTasks.today.map((task) => (
                <TaskRow
                  key={task.id}
                  task={task}
                  onComplete={handleTaskComplete}
                  onClick={onTaskClick}
                  isAnimatingOut={animatingOutTaskIds.has(task.id)}
                  showSystemList={true}
                />
              ))}
            </div>
          </div>
        )}

        {/* Tomorrow Section */}
        {groupedTasks.tomorrow.length > 0 && (
          <div>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              Tomorrow ({groupedTasks.tomorrow.length})
            </h2>
            <div className="space-y-2">
              {groupedTasks.tomorrow.map((task) => (
                <TaskRow
                  key={task.id}
                  task={task}
                  onComplete={handleTaskComplete}
                  onClick={onTaskClick}
                  isAnimatingOut={animatingOutTaskIds.has(task.id)}
                  showSystemList={true}
                />
              ))}
            </div>
          </div>
        )}

        {/* Upcoming Dates */}
        {groupedTasks.upcoming.map((group) => (
          <div key={group.date.toISOString()}>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              {formatDate(group.date)} ({group.tasks.length})
            </h2>
            <div className="space-y-2">
              {group.tasks.map((task) => (
                <TaskRow
                  key={task.id}
                  task={task}
                  onComplete={handleTaskComplete}
                  onClick={onTaskClick}
                  isAnimatingOut={animatingOutTaskIds.has(task.id)}
                  showSystemList={true}
                />
              ))}
            </div>
          </div>
        ))}


      </div>
      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </>
  );
}
