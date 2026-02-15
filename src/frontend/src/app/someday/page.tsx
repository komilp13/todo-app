'use client';

/**
 * Someday Page
 * Deferred tasks using GTD's Someday/Maybe list concept.
 */

import { useState } from 'react';
import { TodoTask, SystemList } from '@/types';
import TaskList from '@/components/Tasks/TaskList';

export default function SomedayPage() {
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
        <h1 className="text-3xl font-bold text-gray-900">Someday</h1>
        <p className="mt-1 text-sm text-gray-600">
          Ideas and wishes for the future. Revisit when ready.
        </p>
      </div>

      <TaskList
        systemList={SystemList.Someday}
        onTaskClick={handleTaskClick}
        onTaskComplete={handleTaskComplete}
      />
    </div>
  );
}
