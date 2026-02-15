'use client';

/**
 * Archive Page
 * Shows completed/archived tasks sorted by completion date (newest first)
 * Tasks display with strikethrough, completion date, and original system list
 * Click opens detail panel with "Reopen" button
 */

import { useState } from 'react';
import { TodoTask } from '@/types';
import ArchiveTaskList from '@/components/Tasks/ArchiveTaskList';
import TaskDetailPanel from '@/components/Tasks/TaskDetailPanel';

export default function ArchivePage() {
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [refreshCounter, setRefreshCounter] = useState(0);

  const handleTaskClick = (task: TodoTask) => {
    setSelectedTaskId(task.id);
  };

  const handleClosePanel = () => {
    setSelectedTaskId(null);
  };

  const handleTaskReopened = () => {
    // Refresh the list when a task is reopened
    setRefreshCounter(prev => prev + 1);
    setSelectedTaskId(null);
  };

  return (
    <>
      <div className="space-y-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Completed</h1>
          <p className="mt-1 text-sm text-gray-600">
            Review your completed tasks and reopen if needed
          </p>
        </div>

        <ArchiveTaskList
          onTaskClick={handleTaskClick}
          refresh={refreshCounter}
        />
      </div>

      {/* Task Detail Panel - will show "Reopen" button for archived tasks */}
      <TaskDetailPanel
        isOpen={!!selectedTaskId}
        taskId={selectedTaskId}
        onClose={handleClosePanel}
        isArchiveView={true}
        onTaskReopened={handleTaskReopened}
      />
    </>
  );
}
