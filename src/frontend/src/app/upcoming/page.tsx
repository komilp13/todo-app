'use client';

/**
 * Upcoming Page
 * Date-driven view showing tasks with due dates in the next 14 days,
 * plus tasks explicitly assigned to the Upcoming system list.
 * Groups tasks by: Overdue, Today, Tomorrow, specific dates, No date.
 */

import { useCallback, useState } from 'react';
import { TodoTask } from '@/types';
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import TaskDetailPanel from '@/components/Tasks/TaskDetailPanel';
import UpcomingTaskList from '@/components/Tasks/UpcomingTaskList';

export default function UpcomingPage() {
  const [refreshCounter, setRefreshCounter] = useState(0);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);

  // Register refresh callback for this page
  useTaskRefresh('upcoming', useCallback(() => {
    setRefreshCounter(prev => prev + 1);
  }, []));

  const handleTaskClick = (task: TodoTask) => {
    setSelectedTaskId(task.id);
  };

  const handleClosePanel = () => {
    setSelectedTaskId(null);
  };

  const handleTaskDeleted = () => {
    setSelectedTaskId(null);
    setRefreshCounter(prev => prev + 1);
  };

  const handleTaskMoved = () => {
    setSelectedTaskId(null);
    setRefreshCounter(prev => prev + 1);
  };

  const handleTaskUpdated = () => {
    setRefreshCounter(prev => prev + 1);
  };

  return (
    <>
      <div className="space-y-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Upcoming</h1>
          <p className="mt-1 text-sm text-gray-600">
            Tasks coming up. Plan ahead and stay on schedule.
          </p>
        </div>

        <UpcomingTaskList
          onTaskClick={handleTaskClick}
          refresh={refreshCounter}
        />
      </div>

      <TaskDetailPanel
        isOpen={!!selectedTaskId}
        taskId={selectedTaskId}
        onClose={handleClosePanel}
        onTaskDeleted={handleTaskDeleted}
        onTaskUpdated={handleTaskUpdated}
        onTaskMoved={handleTaskMoved}
      />
    </>
  );
}
