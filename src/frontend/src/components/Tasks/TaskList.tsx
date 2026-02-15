/**
 * TaskList Component
 * Fetches and displays tasks for a given system list
 * Shows loading skeleton, empty state, and handles task interactions
 */

'use client';

import { useEffect, useState } from 'react';
import { TodoTask, SystemList } from '@/types';
import { apiClient } from '@/services/apiClient';
import TaskRow from './TaskRow';
import TaskListSkeleton from './TaskListSkeleton';

interface TaskListProps {
  systemList: SystemList;
  onTaskClick?: (task: TodoTask) => void;
  onTaskComplete?: (taskId: string) => void;
  refresh?: number; // Increment to trigger refetch
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

  useEffect(() => {
    const fetchTasks = async () => {
      setLoading(true);
      setError(null);

      try {
        const { data } = await apiClient.get<{ tasks: TodoTask[], totalCount: number }>(
          `/tasks?systemList=${systemList}`
        );
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
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {tasks.map((task) => (
        <TaskRow
          key={task.id}
          task={task}
          onComplete={onTaskComplete}
          onClick={onTaskClick}
        />
      ))}
    </div>
  );
}
