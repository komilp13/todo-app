'use client';

/**
 * Upcoming Page
 * Date-driven view showing tasks with due dates in the next 14 days,
 * plus tasks explicitly assigned to the Upcoming system list.
 * Groups tasks by: Overdue, Today, Tomorrow, specific dates, No date.
 */

import { useCallback, useState } from 'react';
import { TodoTask, SystemList, Priority, TaskStatus } from '@/types';
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import { useToast } from '@/hooks/useToast';
import { apiClient, ApiError } from '@/services/apiClient';
import TaskDetailPanel from '@/components/Tasks/TaskDetailPanel';
import UpcomingTaskList from '@/components/Tasks/UpcomingTaskList';
import QuickAddTaskInput from '@/components/Tasks/QuickAddTaskInput';

export default function UpcomingPage() {
  const [refreshCounter, setRefreshCounter] = useState(0);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const { show } = useToast();

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

  const handleQuickAddTask = async (taskName: string) => {
    try {
      // Get today's date at midnight (local time)
      const today = new Date();
      today.setHours(0, 0, 0, 0);

      // Create task with today's due date in Upcoming system list
      await apiClient.post<TodoTask>('/tasks', {
        name: taskName,
        systemList: SystemList.Upcoming,
        dueDate: today.toISOString(),
      });

      show('Task created with today\'s due date', { type: 'success' });

      // Refresh the list to show the new task
      setRefreshCounter(prev => prev + 1);
    } catch (err) {
      console.error('Failed to create task:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to create task', { type: 'error' });
      } else {
        show('Failed to create task', { type: 'error' });
      }
    }
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

        <QuickAddTaskInput
          systemList={SystemList.Upcoming}
          onTaskCreated={handleQuickAddTask}
          onError={(error) => show(error, { type: 'error' })}
        />

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
        onTaskMoved={handleTaskMoved}
      />
    </>
  );
}
