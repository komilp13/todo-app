'use client';

/**
 * Someday Page
 * Deferred tasks using GTD's Someday/Maybe list concept.
 */

import { useState, useCallback } from 'react';
import { TodoTask, SystemList } from '@/types';
import TaskList from '@/components/Tasks/TaskList';
import TaskDetailPanel from '@/components/Tasks/TaskDetailPanel';
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import { useSystemListCounts } from '@/hooks/useSystemListCounts';

export default function SomedayPage() {
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [refreshCounter, setRefreshCounter] = useState(0);
  const { counts, isLoading: isLoadingCounts } = useSystemListCounts(refreshCounter);

  // Register refresh callback for this page
  useTaskRefresh('someday', useCallback(() => {
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

  const handleTaskComplete = (taskId: string) => {
    // TODO: Call complete endpoint (Story 4.2.2)
  };

  return (
    <>
      <div className="space-y-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">
            Someday
            {!isLoadingCounts && (
              <span className="ml-3 text-xl font-normal text-gray-500">
                {counts[SystemList.Someday] || 0}
              </span>
            )}
          </h1>
          <p className="mt-1 text-sm text-gray-600">
            Ideas and wishes for the future. Revisit when ready.
          </p>
        </div>

        <TaskList
          systemList={SystemList.Someday}
          onTaskClick={handleTaskClick}
          onTaskComplete={handleTaskComplete}
          onTaskMoved={handleTaskMoved}
          onTaskDeleted={handleTaskDeleted}
          refresh={refreshCounter}
          emptyMessage="Nothing on the back burner. Add tasks you might want to do someday."
        />
      </div>

      {/* Task Detail Panel */}
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
