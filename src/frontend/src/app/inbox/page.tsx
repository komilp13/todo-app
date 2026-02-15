'use client';

/**
 * Inbox Page
 * Default entry point for tasks. Shows all tasks in the Inbox system list.
 */

import { useState, useCallback } from 'react';
import { TodoTask, SystemList } from '@/types';
import TaskList from '@/components/Tasks/TaskList';
import TaskDetailPanel from '@/components/Tasks/TaskDetailPanel';
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import { useSystemListCounts } from '@/hooks/useSystemListCounts';

export default function InboxPage() {
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [refreshCounter, setRefreshCounter] = useState(0);
  const { counts, isLoading: isLoadingCounts } = useSystemListCounts();

  // Register refresh callback for this page
  useTaskRefresh('inbox', useCallback(() => {
    setRefreshCounter(prev => prev + 1);
  }, []));

  const handleTaskClick = (task: TodoTask) => {
    setSelectedTaskId(task.id);
  };

  const handleClosePanel = () => {
    setSelectedTaskId(null);
  };

  const handleTaskComplete = (taskId: string) => {
    // TODO: Call complete endpoint (Story 4.2.2)
  };

  return (
    <>
      <div className="space-y-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">
            Inbox
            {!isLoadingCounts && (
              <span className="ml-3 text-xl font-normal text-gray-500">
                {counts[SystemList.Inbox] || 0}
              </span>
            )}
          </h1>
          <p className="mt-1 text-sm text-gray-600">
            Capture everything, then organize and prioritize
          </p>
        </div>

        <TaskList
          systemList={SystemList.Inbox}
          onTaskClick={handleTaskClick}
          onTaskComplete={handleTaskComplete}
          refresh={refreshCounter}
          emptyMessage="Your Inbox is clear! Nice work."
        />
      </div>

      {/* Task Detail Panel */}
      <TaskDetailPanel
        isOpen={!!selectedTaskId}
        taskId={selectedTaskId}
        onClose={handleClosePanel}
      />
    </>
  );
}
