/**
 * TaskDetailPanel Component
 * Right-sliding side panel displaying complete task details
 * Shows all task attributes in organized sections
 * Read-only display for now (editing in Story 4.4.2)
 */

'use client';

import { useEffect, useState, useRef } from 'react';
import { TodoTask, Priority, TaskStatus } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';

interface TaskDetailPanelProps {
  isOpen: boolean;
  taskId: string | null;
  onClose: () => void;
}

export default function TaskDetailPanel({
  isOpen,
  taskId,
  onClose,
}: TaskDetailPanelProps) {
  const [task, setTask] = useState<TodoTask | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const panelRef = useRef<HTMLDivElement>(null);

  // Load task data when panel opens or taskId changes
  useEffect(() => {
    if (!isOpen || !taskId) {
      return;
    }

    const fetchTask = async () => {
      setLoading(true);
      setError(null);

      try {
        const { data } = await apiClient.get<TodoTask>(`/tasks/${taskId}`);
        setTask(data);
      } catch (err) {
        const errorMessage =
          err instanceof ApiError ? err.message : 'Failed to load task details';
        setError(errorMessage);
        console.error('Failed to fetch task:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchTask();
  }, [isOpen, taskId]);

  // Handle Escape key to close
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };

    if (isOpen) {
      window.addEventListener('keydown', handleEscape);
      return () => window.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, onClose]);

  // Close on backdrop click
  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  if (!isOpen) {
    return null;
  }

  return (
    <>
      {/* Backdrop - only covers left side (list area) */}
      <div
        className="fixed left-0 top-0 bottom-0 z-30 bg-black bg-opacity-5 transition-opacity"
        style={{ width: 'calc(100% - 24rem)' }}
        onClick={handleBackdropClick}
      />

      {/* Side Panel */}
      <div
        ref={panelRef}
        className="fixed right-0 top-0 bottom-0 z-40 w-96 bg-white shadow-lg transition-transform duration-300 overflow-hidden flex flex-col"
      >
        {/* Header */}
        <div className="flex-shrink-0 border-b border-gray-200 px-6 py-4">
          <div className="flex items-start justify-between gap-4">
            <div className="flex-1 min-w-0">
              <h2 className="text-xl font-bold text-gray-900 break-words">
                {task?.name || 'Loading...'}
              </h2>
              <p className="mt-1 text-xs text-gray-400">{taskId}</p>
            </div>
            <button
              onClick={onClose}
              className="flex-shrink-0 text-gray-400 hover:text-gray-600 transition-colors"
              aria-label="Close panel"
            >
              <svg
                className="w-6 h-6"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto px-6 py-4">
          {loading ? (
            <div className="space-y-4">
              {/* Loading skeleton */}
              <div className="h-20 bg-gray-100 rounded animate-pulse" />
              <div className="space-y-2">
                <div className="h-4 bg-gray-100 rounded w-3/4 animate-pulse" />
                <div className="h-4 bg-gray-100 rounded w-1/2 animate-pulse" />
              </div>
              <div className="h-32 bg-gray-100 rounded animate-pulse" />
            </div>
          ) : error ? (
            <div className="rounded-lg bg-red-50 border border-red-200 p-4">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          ) : task ? (
            <div className="space-y-6">
              {/* Description Section */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-2">
                  Description
                </h3>
                <div className="text-sm text-gray-600 whitespace-pre-wrap break-words">
                  {task.description || (
                    <span className="text-gray-400 italic">No description</span>
                  )}
                </div>
              </div>

              {/* Properties Section */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-3">
                  Properties
                </h3>
                <div className="space-y-3">
                  {/* Due Date */}
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Due Date:</span>
                    <span className="text-sm font-medium text-gray-900">
                      {task.dueDate ? (
                        formatDate(new Date(task.dueDate))
                      ) : (
                        <span className="text-gray-400">No due date</span>
                      )}
                    </span>
                  </div>

                  {/* Priority */}
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Priority:</span>
                    <span
                      className={`px-2 py-1 rounded text-xs font-semibold text-white ${getPriorityColor(
                        task.priority
                      )}`}
                    >
                      {task.priority}
                    </span>
                  </div>

                  {/* System List */}
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">System List:</span>
                    <span className="text-sm font-medium text-gray-900">
                      {task.systemList}
                    </span>
                  </div>

                  {/* Project */}
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Project:</span>
                    <span className="text-sm font-medium text-gray-900">
                      {task.projectId ? (
                        <span className="px-2 py-1 bg-blue-50 text-blue-700 rounded text-xs">
                          Project
                        </span>
                      ) : (
                        <span className="text-gray-400">None</span>
                      )}
                    </span>
                  </div>

                  {/* Labels */}
                  <div>
                    <span className="text-sm text-gray-600 block mb-2">
                      Labels:
                    </span>
                    <div className="flex flex-wrap gap-1">
                      {/* Labels would be populated by Story 7.1 */}
                      <span className="text-xs text-gray-400">
                        No labels assigned
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Metadata Section */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-3">
                  Metadata
                </h3>
                <div className="space-y-2 text-xs text-gray-600">
                  <div className="flex items-center justify-between">
                    <span>Created:</span>
                    <span className="font-medium">
                      {formatDateTime(new Date(task.createdAt))}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Updated:</span>
                    <span className="font-medium">
                      {formatDateTime(new Date(task.updatedAt))}
                    </span>
                  </div>
                  {task.completedAt && (
                    <div className="flex items-center justify-between">
                      <span>Completed:</span>
                      <span className="font-medium">
                        {formatDateTime(new Date(task.completedAt))}
                      </span>
                    </div>
                  )}
                  <div className="flex items-center justify-between">
                    <span>Status:</span>
                    <span className="font-medium">
                      {task.status === TaskStatus.Done ? (
                        <span className="text-green-600">Done</span>
                      ) : (
                        <span className="text-blue-600">Open</span>
                      )}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          ) : null}
        </div>
      </div>
    </>
  );
}

/**
 * Helper: Get color class for priority badge
 */
function getPriorityColor(priority: Priority): string {
  switch (priority) {
    case Priority.P1:
      return 'bg-red-500';
    case Priority.P2:
      return 'bg-orange-500';
    case Priority.P3:
      return 'bg-blue-500';
    case Priority.P4:
    default:
      return 'bg-gray-400';
  }
}

/**
 * Helper: Format date as "Jan 15, 2026"
 */
function formatDate(date: Date): string {
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

/**
 * Helper: Format datetime as "Jan 15, 2026 at 2:30 PM"
 */
function formatDateTime(date: Date): string {
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}
