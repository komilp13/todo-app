/**
 * UpcomingTaskList Component
 * Specialized list for the Upcoming view with date grouping
 * Groups tasks by: Overdue, Today, Tomorrow, specific dates, No date
 * Sorted by priority within each group
 */

'use client';

import { useEffect, useState } from 'react';
import { TodoTask, Priority } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import { useToast } from '@/hooks/useToast';
import TaskRow from './TaskRow';
import TaskListSkeleton from './TaskListSkeleton';
import ToastContainer from '../Toast/ToastContainer';

interface UpcomingTaskListProps {
  onTaskClick?: (task: TodoTask) => void;
  refresh?: number;
}

interface GroupedTasks {
  overdue: TodoTask[];
  today: TodoTask[];
  tomorrow: TodoTask[];
  upcoming: { date: Date; tasks: TodoTask[] }[];
  noDate: TodoTask[];
}

export default function UpcomingTaskList({
  onTaskClick,
  refresh = 0,
}: UpcomingTaskListProps) {
  const [tasks, setTasks] = useState<TodoTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { toasts, show, dismiss } = useToast();

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
      noDate: [],
    };

    // First, sort all tasks by priority within their respective groups
    const sortByPriority = (a: TodoTask, b: TodoTask) => {
      const priorityOrder = { P1: 1, P2: 2, P3: 3, P4: 4 };
      return priorityOrder[a.priority] - priorityOrder[b.priority];
    };

    // Group tasks
    const upcomingDatesMap = new Map<string, TodoTask[]>();

    tasks.forEach((task) => {
      if (!task.dueDate) {
        grouped.noDate.push(task);
        return;
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
    grouped.noDate.sort(sortByPriority);

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
                  onClick={onTaskClick}
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
                  onClick={onTaskClick}
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
                  onClick={onTaskClick}
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
                  onClick={onTaskClick}
                  showSystemList={true}
                />
              ))}
            </div>
          </div>
        ))}

        {/* No Date Section */}
        {groupedTasks.noDate.length > 0 && (
          <div>
            <h2 className="text-lg font-semibold text-gray-500 mb-2">
              No date ({groupedTasks.noDate.length})
            </h2>
            <div className="space-y-2">
              {groupedTasks.noDate.map((task) => (
                <TaskRow
                  key={task.id}
                  task={task}
                  onClick={onTaskClick}
                  showSystemList={true}
                />
              ))}
            </div>
          </div>
        )}
      </div>
      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </>
  );
}
