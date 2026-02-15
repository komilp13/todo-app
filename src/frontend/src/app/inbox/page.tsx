'use client';

/**
 * Inbox Page
 * Default entry point for tasks. Shows all tasks in the Inbox system list.
 */

import { useState } from 'react';
import { TodoTask, SystemList } from '@/types';
import TaskList from '@/components/Tasks/TaskList';

export default function InboxPage() {
  const [, setSelectedTask] = useState<TodoTask | null>(null);

  const handleTaskClick = (task: TodoTask) => {
    setSelectedTask(task);
    // TODO: Open task detail panel (Story 4.4.1)
  };

  const handleTaskComplete = (taskId: string) => {
    // TODO: Call complete endpoint (Story 4.2.2)
  };

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Inbox</h1>
        <p className="mt-1 text-sm text-gray-600">
          Capture everything, then organize and prioritize
        </p>
      </div>

      <TaskList
        systemList={SystemList.Inbox}
        onTaskClick={handleTaskClick}
        onTaskComplete={handleTaskComplete}
      />
    </div>
  );
}
