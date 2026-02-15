/**
 * ArchiveTaskList Component
 * Fetches and displays completed/archived tasks
 * Shows tasks with completion date, strikethrough styling, and original list
 * No quick-add, completion checkbox, or drag-drop (archive is read-only-ish)
 */

'use client';

import { useEffect, useState } from 'react';
import { TodoTask } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import { useToast } from '@/hooks/useToast';
import ArchiveTaskRow from './ArchiveTaskRow';
import TaskListSkeleton from './TaskListSkeleton';
import ToastContainer from '../Toast/ToastContainer';

interface ArchiveTaskListProps {
  onTaskClick?: (task: TodoTask) => void;
  refresh?: number; // Increment to trigger refetch
}

export default function ArchiveTaskList({
  onTaskClick,
  refresh = 0,
}: ArchiveTaskListProps) {
  const [tasks, setTasks] = useState<TodoTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { toasts, show, dismiss } = useToast();

  useEffect(() => {
    const fetchArchivedTasks = async () => {
      setLoading(true);
      setError(null);

      try {
        // Fetch archived tasks with archived=true query param
        const { data } = await apiClient.get<{
          tasks: TodoTask[];
          totalCount: number;
        }>('/tasks?archived=true');

        // Sort by completedAt descending (newest first)
        const sortedTasks = data.tasks.sort((a, b) => {
          const dateA = a.completedAt ? new Date(a.completedAt).getTime() : 0;
          const dateB = b.completedAt ? new Date(b.completedAt).getTime() : 0;
          return dateB - dateA;
        });

        setTasks(sortedTasks);
      } catch (err) {
        console.error('Failed to fetch archived tasks:', err);
        setError('Failed to load completed tasks. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchArchivedTasks();
  }, [refresh]);

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
            No completed tasks yet. Keep working! 💪
          </p>
        </div>
        <ToastContainer toasts={toasts} onDismiss={dismiss} />
      </>
    );
  }

  return (
    <>
      <div className="space-y-2">
        {tasks.map((task) => (
          <ArchiveTaskRow
            key={task.id}
            task={task}
            onClick={onTaskClick}
          />
        ))}
      </div>
      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </>
  );
}
